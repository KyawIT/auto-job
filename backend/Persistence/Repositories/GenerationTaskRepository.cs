using Core.Contracts.Repositories;
using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class GenerationTaskRepository : Repository<GenerationTask>, IGenerationTaskRepository
    {
        public GenerationTaskRepository(ApplicationDBContext context) : base(context) { }

        public Task<List<GenerationTask>> GetQueuedAsync(int max, CancellationToken cancellationToken = default) =>
            _set
                .Where(t => t.Status == GenerationTaskStatus.Queued)
                .OrderBy(t => t.Id)
                .Take(max)
                .ToListAsync(cancellationToken);

        // Load-and-modify instead of ExecuteUpdate so timestamps and RowVersion are stamped on save.
        public async Task<int> RequeueInterruptedAsync(CancellationToken cancellationToken = default)
        {
            var interrupted = await _set.Where(t => t.Status == GenerationTaskStatus.Running).ToListAsync(cancellationToken);
            foreach (var task in interrupted)
            {
                task.Status = GenerationTaskStatus.Queued;
                task.StartedAt = null;
            }
            return interrupted.Count;
        }
    }
}
