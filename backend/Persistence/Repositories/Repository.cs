using Core.Contracts.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDBContext _context;
        protected readonly DbSet<T> _set;

        public Repository(ApplicationDBContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            await _set.FindAsync([id], cancellationToken);

        public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _set.ToListAsync(cancellationToken);

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
            await _set.AddAsync(entity, cancellationToken);

        public void Remove(T entity) => _set.Remove(entity);
    }
}
