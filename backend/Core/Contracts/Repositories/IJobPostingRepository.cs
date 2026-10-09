using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Contracts.Repositories
{
    public interface IJobPostingRepository : IRepository<JobPosting>
    {
        // Used before inserting fetched jobs, to skip ones already stored.
        Task<JobPosting?> FindByExternalIdAsync(JobSource source, string externalId, CancellationToken cancellationToken = default);

        Task<JobPosting?> GetWithApplicationAsync(int id, CancellationToken cancellationToken = default);

        // Newest first, with the application (if any) loaded.
        Task<List<JobPosting>> ListAsync(JobPostingQuery query, CancellationToken cancellationToken = default);
    }
}
