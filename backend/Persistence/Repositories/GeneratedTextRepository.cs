using Core.Contracts.Repositories;
using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class GeneratedTextRepository : Repository<GeneratedText>, IGeneratedTextRepository
    {
        public GeneratedTextRepository(ApplicationDBContext context) : base(context) { }

        public Task<GeneratedText?> GetCurrentAsync(int applicationId, GeneratedTextKind kind, CancellationToken cancellationToken = default) =>
            _set.FirstOrDefaultAsync(t => t.ApplicationId == applicationId && t.Kind == kind && t.IsCurrent, cancellationToken);
    }
}
