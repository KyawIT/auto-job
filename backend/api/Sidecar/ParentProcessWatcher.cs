using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace api.Sidecar
{
    // If Electron crashes or is force-killed it can't stop us, so we stop ourselves.
    public class ParentProcessWatcher : BackgroundService
    {
        private readonly SidecarOptions _options;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<ParentProcessWatcher> _logger;

        public ParentProcessWatcher(SidecarOptions options, IHostApplicationLifetime lifetime, ILogger<ParentProcessWatcher> logger)
        {
            _options = options;
            _lifetime = lifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_options.ParentPid is not int pid)
                return;

            try
            {
                using var parent = Process.GetProcessById(pid);
                await parent.WaitForExitAsync(stoppingToken);
            }
            catch (ArgumentException)
            {
                // Parent was already gone before we started watching.
            }
            catch (OperationCanceledException)
            {
                return; // We're shutting down normally.
            }

            _logger.LogWarning("Parent process {Pid} exited, shutting down", pid);
            _lifetime.StopApplication();
        }
    }
}
