using Core.Contracts;
using Core.Entities;
using Core.Enums;
using Services.Applications;
using Services.Common;

namespace Services.Generation
{
    // Generate letters for many jobs at once: creates the batch and its queued tasks.
    // The work itself is done in the background by BatchProcessor.
    public class BatchService
    {
        private readonly IUnitOfWork _uow;
        private readonly ApplicationService _applications;
        private readonly TimeProvider _time;

        public BatchService(IUnitOfWork uow, ApplicationService applications, TimeProvider time)
        {
            _uow = uow;
            _applications = applications;
            _time = time;
        }

        public async Task<BatchDto> StartAsync(StartBatchRequest request, CancellationToken ct = default)
        {
            new Validator().Check(request.JobPostingIds.Count > 0, nameof(request.JobPostingIds), "Select at least one job.").ThrowIfInvalid();

            var settings = await _uow.AppSettings.GetAsync(ct);
            var batch = new GenerationBatch { Language = request.Language ?? settings.DefaultLetterLanguage };
            foreach (var jobId in request.JobPostingIds.Distinct())
            {
                var application = await _applications.StartAsync(jobId, new StartApplicationRequest(request.Language), ct);
                batch.Tasks.Add(new GenerationTask { ApplicationId = application.Id });
            }

            await _uow.GenerationBatches.AddAsync(batch, ct);
            await _uow.SaveChangesAsync(ct);
            return await GetAsync(batch.Id, ct);
        }

        // Queued tasks are dropped; tasks already running finish normally.
        public async Task<BatchDto> CancelAsync(int id, CancellationToken ct = default)
        {
            var batch = await RequireAsync(id, ct);
            if (batch.Status == BatchStatus.Running)
            {
                foreach (var task in batch.Tasks.Where(t => t.Status == GenerationTaskStatus.Queued))
                {
                    task.Status = GenerationTaskStatus.Cancelled;
                    task.FinishedAt = Now;
                }
                batch.Status = BatchStatus.Cancelled;
                batch.FinishedAt = Now;
                await _uow.SaveChangesAsync(ct);
            }
            return await GetAsync(id, ct);
        }

        // E.g. after the user added a missing API key. Failed tasks get a fresh set of attempts.
        public async Task<BatchDto> RetryFailedAsync(int id, CancellationToken ct = default)
        {
            var batch = await RequireAsync(id, ct);
            var failed = batch.Tasks.Where(t => t.Status == GenerationTaskStatus.Failed).ToList();
            if (failed.Count > 0)
            {
                foreach (var task in failed)
                {
                    task.Status = GenerationTaskStatus.Queued;
                    task.Attempts = 0;
                    task.Error = null;
                    task.StartedAt = null;
                    task.FinishedAt = null;
                }
                batch.Status = BatchStatus.Running;
                batch.FinishedAt = null;
                await _uow.SaveChangesAsync(ct);
            }
            return await GetAsync(id, ct);
        }

        public async Task<BatchDto> GetAsync(int id, CancellationToken ct = default)
        {
            var batch = await RequireAsync(id, ct);
            var progress = await _uow.GenerationBatches.GetProgressAsync(id, ct);
            return new BatchDto(batch.Id, batch.Status, batch.CreatedAt, batch.FinishedAt, progress,
                batch.Tasks.OrderBy(t => t.Id).Select(t => new BatchTaskDto(
                    t.Id, t.ApplicationId, t.Application.JobPosting.Title, t.Application.JobPosting.CompanyName, t.Status, t.Attempts, t.Error)).ToList());
        }

        private async Task<GenerationBatch> RequireAsync(int id, CancellationToken ct) =>
            await _uow.GenerationBatches.GetWithTasksAsync(id, ct) ?? throw new NotFoundException("Batch", id);

        private DateTime Now => _time.GetUtcNow().UtcDateTime;
    }
}
