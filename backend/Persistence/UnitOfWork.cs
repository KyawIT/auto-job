using Core.Contracts;

namespace Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDBContext _context;

        public UnitOfWork(ApplicationDBContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);
    }
}
