using Core.Entities;
using Core.Models;

namespace Core.Contracts.Repositories
{
    public interface IGenerationBatchRepository : IRepository<GenerationBatch>
    {
        Task<BatchProgress> GetProgressAsync(int batchId, CancellationToken cancellationToken = default);

        // With tasks and each task's application and job.
        Task<GenerationBatch?> GetWithTasksAsync(int batchId, CancellationToken cancellationToken = default);
    }
}
