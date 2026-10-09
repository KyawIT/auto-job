using Core.Entities;
using Core.Enums;

namespace Core.Contracts.Repositories
{
    public interface IGeneratedTextRepository : IRepository<GeneratedText>
    {
        // The version marked current for export/email; null if none was generated yet.
        Task<GeneratedText?> GetCurrentAsync(int applicationId, GeneratedTextKind kind, CancellationToken cancellationToken = default);
    }
}
