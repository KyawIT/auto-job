using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Time.Testing;
using Persistence.Tests;
using Services.Applications;
using Services.Common;
using Services.Generation;
using Services.Tests.Support;

namespace Services.Tests.Generation
{
    public class BatchServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero));

        public BatchServiceTests()
        {
            Seed.ProfileAsync(_db.CreateUnitOfWork()).GetAwaiter().GetResult();
        }

        public void Dispose() => _db.Dispose();

        private BatchService Service()
        {
            var uow = _db.CreateUnitOfWork();
            return new BatchService(uow, new ApplicationService(uow, _time), _time);
        }

        [Fact]
        public async Task Starting_a_batch_queues_one_task_per_job_and_reuses_existing_applications()
        {
            var uow = _db.CreateUnitOfWork();
            var withApplication = await Seed.JobAsync(uow, "Backend Developer");
            var withoutApplication = await Seed.JobAsync(uow, "API Developer");
            var existing = await new ApplicationService(_db.CreateUnitOfWork(), _time).StartAsync(withApplication.Id);

            var batch = await Service().StartAsync(new StartBatchRequest([withApplication.Id, withoutApplication.Id], Language: "en"));

            Assert.Equal(new BatchProgress(Total: 2, Queued: 2, Running: 0, Done: 0, Failed: 0, Cancelled: 0), batch.Progress);
            Assert.Equal(BatchStatus.Running, batch.Status);
            Assert.Contains(existing.Id, batch.Tasks.Select(t => t.ApplicationId));
            Assert.Equal(["API Developer", "Backend Developer"], batch.Tasks.Select(t => t.JobTitle).Order());
            Assert.Equal("en", (await new ApplicationService(_db.CreateUnitOfWork(), _time).ListAsync(null)).Single(a => a.JobTitle == "API Developer").Language);
        }

        [Fact]
        public async Task Empty_selection_is_rejected_and_unknown_job_is_not_found()
        {
            await Assert.ThrowsAsync<ValidationException>(() => Service().StartAsync(new StartBatchRequest([])));
            await Assert.ThrowsAsync<NotFoundException>(() => Service().StartAsync(new StartBatchRequest([999])));
        }
    }
}
