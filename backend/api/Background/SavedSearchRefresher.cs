using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Jobs;

namespace api.Background
{
    // Refreshes due saved searches while the app is running. The schedule logic lives in JobSearchService.
    public class SavedSearchRefresher : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<SavedSearchRefresher> _logger;

        public SavedSearchRefresher(IServiceScopeFactory scopes, ILogger<SavedSearchRefresher> logger)
        {
            _scopes = scopes;
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
            using var timer = new PeriodicTimer(CheckInterval);
            do
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var found = await scope.ServiceProvider.GetRequiredService<JobSearchService>().RefreshDueAsync(stoppingToken);
                    if (found > 0)
                        _logger.LogInformation("Saved searches found {Count} new jobs", found);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Refreshing saved searches failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
