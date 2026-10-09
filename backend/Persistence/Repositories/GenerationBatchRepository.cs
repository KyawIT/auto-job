using Core.Contracts.Repositories;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class GenerationBatchRepository : Repository<GenerationBatch>, IGenerationBatchRepository
    {
        public GenerationBatchRepository(ApplicationDBContext context) : base(context) { }

        public Task<GenerationBatch?> GetWithTasksAsync(int batchId, CancellationToken cancellationToken = default) =>
            _set.Include(b => b.Tasks).ThenInclude(t => t.Application).ThenInclude(a => a.JobPosting)
                .AsSplitQuery()
                .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

        public async Task<BatchProgress> GetProgressAsync(int batchId, CancellationToken cancellationToken = default)
        {
            var counts = await _context.Set<GenerationTask>()
                .Where(t => t.GenerationBatchId == batchId)
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

            int Count(GenerationTaskStatus status) => counts.GetValueOrDefault(status);

            return new BatchProgress(
                Total: counts.Values.Sum(),
                Queued: Count(GenerationTaskStatus.Queued),
                Running: Count(GenerationTaskStatus.Running),
                Done: Count(GenerationTaskStatus.Done),
                Failed: Count(GenerationTaskStatus.Failed),
                Cancelled: Count(GenerationTaskStatus.Cancelled));
        }
    }
}
