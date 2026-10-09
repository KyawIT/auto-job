using Core.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Tests
{
    // A fresh SQLite database per test, built from the real migrations.
    // Each CreateUnitOfWork() call gets its own DbContext, like a separate HTTP request,
    // so reads after a save really come from the database and not from the change tracker.
    public sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection? _sharedConnection;
        private readonly string? _file;
        private readonly DbContextOptions<ApplicationDBContext> _options;
        private readonly List<ApplicationDBContext> _contexts = new();

        // In memory: fast, but all contexts share one connection, so not for parallel use.
        public TestDatabase()
        {
            _sharedConnection = new SqliteConnection("Data Source=:memory:");
            _sharedConnection.Open();
            _options = new DbContextOptionsBuilder<ApplicationDBContext>().UseSqlite(_sharedConnection).Options;
            Migrate();
        }

        private TestDatabase(string file)
        {
            _file = file;
            _options = new DbContextOptionsBuilder<ApplicationDBContext>().UseSqlite($"Data Source={file}").Options;
            Migrate();
        }

        // A temporary file: every context opens its own connection, like the real app (for parallel work).
        public static TestDatabase OnDisk() =>
            new(Path.Combine(Path.GetTempPath(), $"autojob-test-{Guid.NewGuid():N}.db"));

        public IUnitOfWork CreateUnitOfWork()
        {
            var context = new ApplicationDBContext(_options);
            lock (_contexts)
                _contexts.Add(context);
            return new UnitOfWork(context);
        }

        public void Dispose()
        {
            lock (_contexts)
                foreach (var context in _contexts)
                    context.Dispose();
            _sharedConnection?.Dispose();

            if (_file != null)
            {
                SqliteConnection.ClearAllPools();
                File.Delete(_file);
            }
        }

        private void Migrate()
        {
            using var context = new ApplicationDBContext(_options);
            context.Database.Migrate();
        }
    }
}
