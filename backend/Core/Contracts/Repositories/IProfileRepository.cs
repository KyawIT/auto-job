using Core.Entities;

namespace Core.Contracts.Repositories
{
    public interface IProfileRepository : IRepository<Profile>
    {
        // The single profile with experience, education, skills, languages and documents; null before setup.
        Task<Profile?> GetAsync(CancellationToken cancellationToken = default);
    }
}
