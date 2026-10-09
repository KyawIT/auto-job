using Core.Contracts.Repositories;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class AppSettingsRepository : Repository<AppSettings>, IAppSettingsRepository
    {
        public AppSettingsRepository(ApplicationDBContext context) : base(context) { }

        public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _set.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
            if (settings == null)
            {
                settings = new AppSettings();
                await _set.AddAsync(settings, cancellationToken);
            }
            return settings;
        }
    }
}
