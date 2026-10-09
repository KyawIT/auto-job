using Core.Entities;

namespace Core.Contracts.Repositories
{
    public interface IGenerationTaskRepository : IRepository<GenerationTask>
    {
        // Oldest queued tasks first, at most `max` (the worker's parallelism).
        Task<List<GenerationTask>> GetQueuedAsync(int max, CancellationToken cancellationToken = default);

        // Startup recovery: tasks left "Running" by a crash/close go back to "Queued".
        // Changes are applied on the next SaveChangesAsync. Returns how many were requeued.
        Task<int> RequeueInterruptedAsync(CancellationToken cancellationToken = default);
    }
}
