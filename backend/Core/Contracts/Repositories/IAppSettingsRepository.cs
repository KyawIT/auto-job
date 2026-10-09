using Core.Entities;

namespace Core.Contracts.Repositories
{
    public interface IAppSettingsRepository : IRepository<AppSettings>
    {
        // The single settings row. Created with defaults on first use (persisted on the next save).
        Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);
    }
}
