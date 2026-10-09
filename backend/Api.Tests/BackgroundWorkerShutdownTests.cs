using api.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Tests
{
    public class BackgroundWorkerShutdownTests
    {
        // Stopping the app must end the workers normally, not with an exception
        // (the debugger otherwise breaks on every stop with "TaskCanceledException user-unhandled").
        [Fact]
        public async Task Workers_finish_without_exception_when_the_app_stops()
        {
            var factory = new ApiFactory();
            factory.CreateClient();
            var workers = factory.Services.GetServices<IHostedService>()
                .Where(s => s is GenerationWorker or SavedSearchRefresher)
                .Cast<BackgroundService>()
                .ToList();
            await Task.Delay(500);   // let them reach their waits

            factory.Dispose();

            Assert.Equal(2, workers.Count);
            Assert.All(workers, w => Assert.Equal(TaskStatus.RanToCompletion, w.ExecuteTask!.Status));
        }
    }
}
