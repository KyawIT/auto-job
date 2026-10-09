using Core.Contracts.Repositories;
using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class ApplicationRepository : Repository<Application>, IApplicationRepository
    {
        public ApplicationRepository(ApplicationDBContext context) : base(context) { }

        public Task<Application?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
            _set
                .Include(a => a.JobPosting)
                .Include(a => a.Attachments).ThenInclude(ad => ad.Document)
                .Include(a => a.GeneratedTexts)
                .Include(a => a.Events)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        public Task<List<Application>> ListAsync(ApplicationStatus? status, CancellationToken cancellationToken = default) =>
            _set.Include(a => a.JobPosting)
                .Where(a => status == null || a.Status == status)
                .OrderByDescending(a => a.UpdatedAt).ThenByDescending(a => a.Id)
                .ToListAsync(cancellationToken);
    }
}
