using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Generation;

namespace api.Background
{
    // Runs BatchProcessor passes while the app is open: right away when there is more work,
    // after the provider's Retry-After on rate limits, and every couple of seconds when idle.
    public class GenerationWorker : BackgroundService
    {
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);

        private readonly BatchProcessor _processor;
        private readonly ILogger<GenerationWorker> _logger;

        public GenerationWorker(BatchProcessor processor, ILogger<GenerationWorker> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The app is stopping; leave quietly.
            }
        }

        private async Task RunAsync(CancellationToken stoppingToken)
        {
            var recovered = await _processor.RecoverAsync(stoppingToken);
            if (recovered > 0)
                _logger.LogInformation("Requeued {Count} interrupted generation tasks", recovered);

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan delay;
                try
                {
                    var pass = await _processor.RunOnceAsync(stoppingToken);
                    delay = pass.RetryAfter ?? (pass.Processed == 0 ? IdleDelay : TimeSpan.Zero);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Generation pass failed");
                    delay = IdleDelay;
                }

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
