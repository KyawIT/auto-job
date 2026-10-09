using Core.Contracts.Repositories;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class ProfileRepository : Repository<Profile>, IProfileRepository
    {
        public ProfileRepository(ApplicationDBContext context) : base(context) { }

        public Task<Profile?> GetAsync(CancellationToken cancellationToken = default) =>
            _set
                .Include(p => p.WorkExperiences)
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Languages)
                .Include(p => p.Documents)
                .AsSplitQuery()
                .FirstOrDefaultAsync(cancellationToken);
    }
}
