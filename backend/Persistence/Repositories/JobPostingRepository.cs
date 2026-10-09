using Core.Contracts.Repositories;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class JobPostingRepository : Repository<JobPosting>, IJobPostingRepository
    {
        public JobPostingRepository(ApplicationDBContext context) : base(context) { }

        public Task<JobPosting?> FindByExternalIdAsync(JobSource source, string externalId, CancellationToken cancellationToken = default) =>
            _set.Include(j => j.Application).FirstOrDefaultAsync(j => j.Source == source && j.ExternalId == externalId, cancellationToken);

        public Task<JobPosting?> GetWithApplicationAsync(int id, CancellationToken cancellationToken = default) =>
            _set.Include(j => j.Application).FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        public Task<List<JobPosting>> ListAsync(JobPostingQuery query, CancellationToken cancellationToken = default)
        {
            var jobs = _set.Include(j => j.Application).AsQueryable();

            if (query.State != null)
                jobs = jobs.Where(j => j.State == query.State);
            else if (!query.IncludeDismissed)
                jobs = jobs.Where(j => j.State != JobPostingState.Dismissed);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                jobs = jobs.Where(j => j.Title.ToLower().Contains(search)
                    || j.CompanyName.ToLower().Contains(search)
                    || (j.Location != null && j.Location.ToLower().Contains(search)));
            }

            return jobs.OrderByDescending(j => j.PostedAt ?? j.CreatedAt).ToListAsync(cancellationToken);
        }
    }
}
