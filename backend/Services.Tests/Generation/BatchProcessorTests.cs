using Core.Contracts;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Persistence.Tests;
using Services.Applications;
using Services.Generation;
using Services.Ports;
using Services.Tests.Support;

namespace Services.Tests.Generation
{
    // The background worker's logic: one RunOnceAsync call = one pass over the queue.
    public class BatchProcessorTests : IDisposable
    {
        private readonly TestDatabase _db = TestDatabase.OnDisk();
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero));
        private readonly FakeAiClient _ai = new();
        private readonly ServiceProvider _services;

        public BatchProcessorTests()
        {
            _services = new ServiceCollection()
                .AddScoped(_ => _db.CreateUnitOfWork())
                .AddSingleton<IAiClient>(_ai)
                .AddSingleton<TimeProvider>(_time)
                .AddScoped<ApplicationService>()
                .AddScoped<GenerationService>()
                .AddScoped<BatchService>()
                .AddSingleton<BatchProcessor>()
                .BuildServiceProvider();
            Seed.ProfileAsync(_db.CreateUnitOfWork()).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _services.Dispose();
            _db.Dispose();
        }

        private BatchProcessor Processor => _services.GetRequiredService<BatchProcessor>();

        private async Task<BatchDto> StartBatchAsync(int jobs)
        {
            var ids = new List<int>();
            for (var i = 0; i < jobs; i++)
                ids.Add((await Seed.JobAsync(_db.CreateUnitOfWork(), $"Job {i}")).Id);
            using var scope = _services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<BatchService>().StartAsync(new StartBatchRequest(ids));
        }

        private async Task<BatchDto> BatchAsync(int id)
        {
            using var scope = _services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<BatchService>().GetAsync(id);
        }

        [Fact]
        public async Task Pass_generates_every_queued_application_and_completes_the_batch()
        {
            _ai.AlwaysReturnsLetter("Sehr geehrte Damen und Herren");
            var batch = await StartBatchAsync(jobs: 3);

            await Processor.RunOnceAsync();
            await Processor.RunOnceAsync();   // settings allow 2 in parallel → second pass takes the third

            var result = await BatchAsync(batch.Id);
            Assert.Equal(BatchStatus.Completed, result.Status);
            Assert.Equal(new DateTime(2026, 10, 9, 10, 0, 0), result.FinishedAt);
            Assert.Equal(new BatchProgress(3, 0, 0, 3, 0, 0), result.Progress);
            using var scope = _services.CreateScope();
            var applications = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            foreach (var task in result.Tasks)
                Assert.Equal(3, (await applications.GetAsync(task.ApplicationId)).Texts.Count);
        }

        [Fact]
        public async Task Rate_limited_task_is_retried_after_the_reported_wait()
        {
            _ai.Throws(new AiRateLimitedException(TimeSpan.FromSeconds(20))).ReturnsLetter("Sehr geehrte Damen und Herren");
            var batch = await StartBatchAsync(jobs: 1);

            var first = await Processor.RunOnceAsync();
            Assert.Equal(TimeSpan.FromSeconds(20), first.RetryAfter);
            Assert.Equal(GenerationTaskStatus.Queued, (await BatchAsync(batch.Id)).Tasks.Single().Status);

            await Processor.RunOnceAsync();
            var task = (await BatchAsync(batch.Id)).Tasks.Single();
            Assert.Equal((GenerationTaskStatus.Done, 2), (task.Status, task.Attempts));
        }

        [Fact]
        public async Task Task_fails_after_three_attempts_and_the_batch_still_completes()
        {
            _ai.AlwaysThrows(new AiRequestException("The AI answer was not valid JSON."));
            var batch = await StartBatchAsync(jobs: 1);

            for (var pass = 0; pass < BatchProcessor.MaxAttempts; pass++)
                await Processor.RunOnceAsync();

            var result = await BatchAsync(batch.Id);
            var task = result.Tasks.Single();
            Assert.Equal((GenerationTaskStatus.Failed, 3, "The AI answer was not valid JSON."), (task.Status, task.Attempts, task.Error));
            Assert.Equal(BatchStatus.Completed, result.Status);
            Assert.Equal(0, (await Processor.RunOnceAsync()).Processed);
        }

        private async Task<BatchDto> WithBatchService(Func<BatchService, Task<BatchDto>> action)
        {
            using var scope = _services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<BatchService>());
        }

        [Fact]
        public async Task Cancelled_batch_stops_and_failed_tasks_can_be_retried()
        {
            _ai.Throws(new AiNotConfiguredException("No key")).Throws(new AiNotConfiguredException("No key")).AlwaysReturnsLetter("Hallo");
            var batch = await StartBatchAsync(jobs: 3);
            await Processor.RunOnceAsync();                                  // 2 tasks fail (no key), 1 still queued

            var cancelled = await WithBatchService(s => s.CancelAsync(batch.Id));
            Assert.Equal(BatchStatus.Cancelled, cancelled.Status);
            Assert.Equal(new BatchProgress(3, 0, 0, 0, 2, 1), cancelled.Progress);
            Assert.Equal(0, (await Processor.RunOnceAsync()).Processed);

            var retried = await WithBatchService(s => s.RetryFailedAsync(batch.Id));   // user added the key
            Assert.Equal(BatchStatus.Running, retried.Status);
            await Processor.RunOnceAsync();
            Assert.Equal(new BatchProgress(3, 0, 0, 2, 0, 1), (await BatchAsync(batch.Id)).Progress);
        }

        [Fact]
        public async Task Tasks_interrupted_by_closing_the_app_continue_after_restart()
        {
            var batch = await StartBatchAsync(jobs: 1);
            var uow = _db.CreateUnitOfWork();
            var task = await uow.GenerationTasks.GetByIdAsync(batch.Tasks.Single().Id);
            task!.Status = GenerationTaskStatus.Running;                      // app closed mid-generation
            await uow.SaveChangesAsync();
            _ai.AlwaysReturnsLetter("Hallo");

            Assert.Equal(1, await Processor.RecoverAsync());
            await Processor.RunOnceAsync();

            Assert.Equal(GenerationTaskStatus.Done, (await BatchAsync(batch.Id)).Tasks.Single().Status);
        }

        [Fact]
        public async Task Missing_api_key_fails_at_once_without_retrying()
        {
            _ai.AlwaysThrows(new AiNotConfiguredException("The NVIDIA NIM API key is not set."));
            var batch = await StartBatchAsync(jobs: 1);

            await Processor.RunOnceAsync();

            var task = (await BatchAsync(batch.Id)).Tasks.Single();
            Assert.Equal((GenerationTaskStatus.Failed, 1), (task.Status, task.Attempts));
        }
    }
}
