using Core.Contracts;
using Core.Entities;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Services.Ports;

namespace Services.Generation
{
    public record BatchPassResult(int Processed, TimeSpan? RetryAfter);

    // Works through queued generation tasks. Each RunOnceAsync call is one pass: take up to
    // MaxParallelGenerations tasks, generate them in parallel (each with its own DbContext), record the outcome.
    // Waiting between passes is the caller's job (see the background worker), which keeps this testable.
    public class BatchProcessor
    {
        public const int MaxAttempts = 3;
        private static readonly TimeSpan DefaultBackoff = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopes;
        private readonly TimeProvider _time;

        public BatchProcessor(IServiceScopeFactory scopes, TimeProvider time)
        {
            _scopes = scopes;
            _time = time;
        }

        // On startup: tasks that were running when the app closed go back to the queue.
        public async Task<int> RecoverAsync(CancellationToken ct = default)
        {
            using var scope = _scopes.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var requeued = await uow.GenerationTasks.RequeueInterruptedAsync(ct);
            await uow.SaveChangesAsync(ct);
            return requeued;
        }

        public async Task<BatchPassResult> RunOnceAsync(CancellationToken ct = default)
        {
            var claimed = await ClaimQueuedTasksAsync(ct);
            if (claimed.Count == 0)
                return new BatchPassResult(0, null);

            var outcomes = await Task.WhenAll(claimed.Select(t => GenerateAsync(t, ct)));
            await RecordAsync(outcomes, ct);

            var retryAfter = outcomes.Where(o => o.Kind == OutcomeKind.Retry).Select(o => o.RetryAfter).Max();
            return new BatchPassResult(claimed.Count, retryAfter);
        }

        private async Task<List<ClaimedTask>> ClaimQueuedTasksAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var settings = await uow.AppSettings.GetAsync(ct);
            var tasks = await uow.GenerationTasks.GetQueuedAsync(settings.MaxParallelGenerations, ct);

            foreach (var task in tasks)
            {
                task.Status = GenerationTaskStatus.Running;
                task.StartedAt = Now;
                task.Attempts++;
            }
            await uow.SaveChangesAsync(ct);
            return tasks.Select(t => new ClaimedTask(t.Id, t.ApplicationId, t.GenerationBatchId, t.Attempts)).ToList();
        }

        private async Task<Outcome> GenerateAsync(ClaimedTask task, CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<GenerationService>().GenerateAsync(task.ApplicationId, ct);
                return new Outcome(task, OutcomeKind.Done, null, null);
            }
            catch (AiRateLimitedException ex)
            {
                return new Outcome(task, OutcomeKind.Retry, ex.Message, ex.RetryAfter ?? DefaultBackoff);
            }
            catch (AiNotConfiguredException ex)
            {
                return new Outcome(task, OutcomeKind.Failed, ex.Message, null);   // retrying can't help
            }
            catch (AiRequestException ex)
            {
                return new Outcome(task, OutcomeKind.Retry, ex.Message, DefaultBackoff);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new Outcome(task, OutcomeKind.Failed, ex.Message, null);
            }
        }

        private async Task RecordAsync(IReadOnlyList<Outcome> outcomes, CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            foreach (var outcome in outcomes)
            {
                var task = await uow.GenerationTasks.GetByIdAsync(outcome.Task.Id, ct);
                var batch = await uow.GenerationBatches.GetByIdAsync(outcome.Task.BatchId, ct);
                if (task == null || batch == null)
                    continue;   // deleted meanwhile

                var giveUp = outcome.Kind == OutcomeKind.Failed || (outcome.Kind == OutcomeKind.Retry && outcome.Task.Attempts >= MaxAttempts);
                (task.Status, task.Error) = outcome.Kind switch
                {
                    OutcomeKind.Done => (GenerationTaskStatus.Done, null),
                    _ when giveUp => (GenerationTaskStatus.Failed, outcome.Error),
                    _ when batch.Status == BatchStatus.Cancelled => (GenerationTaskStatus.Cancelled, outcome.Error),
                    _ => (GenerationTaskStatus.Queued, outcome.Error),
                };
                task.FinishedAt = task.Status == GenerationTaskStatus.Queued ? null : Now;
                if (task.Status == GenerationTaskStatus.Queued)
                    task.StartedAt = null;
            }
            await uow.SaveChangesAsync(ct);

            foreach (var batchId in outcomes.Select(o => o.Task.BatchId).Distinct())
                await CompleteIfFinishedAsync(uow, batchId, ct);
            await uow.SaveChangesAsync(ct);
        }

        private async Task CompleteIfFinishedAsync(IUnitOfWork uow, int batchId, CancellationToken ct)
        {
            var batch = await uow.GenerationBatches.GetByIdAsync(batchId, ct);
            var progress = await uow.GenerationBatches.GetProgressAsync(batchId, ct);
            if (batch is { Status: BatchStatus.Running } && progress.Queued == 0 && progress.Running == 0)
            {
                batch.Status = BatchStatus.Completed;
                batch.FinishedAt = Now;
            }
        }

        private DateTime Now => _time.GetUtcNow().UtcDateTime;

        private record ClaimedTask(int Id, int ApplicationId, int BatchId, int Attempts);

        private enum OutcomeKind { Done, Retry, Failed }

        private record Outcome(ClaimedTask Task, OutcomeKind Kind, string? Error, TimeSpan? RetryAfter);
    }
}
