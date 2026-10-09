using Core.Entities;
using Core.Enums;

namespace Core.Contracts.Repositories
{
    public interface IApplicationRepository : IRepository<Application>
    {
        // Application with its job, attached documents, generated texts and timeline.
        Task<Application?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

        // With job; most recently changed first. Null status = all.
        Task<List<Application>> ListAsync(ApplicationStatus? status, CancellationToken cancellationToken = default);
    }
}
