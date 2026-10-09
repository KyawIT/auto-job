namespace Core.Contracts
{
    public interface IUnitOfWork
    {
        // Repositories get exposed here as properties, e.g. IJobRepository Jobs { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
