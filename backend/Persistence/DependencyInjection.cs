using Core.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string databasePath)
        {
            var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

            services.AddDbContext<ApplicationDBContext>(options => options.UseSqlite(connectionString));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        // Users never run `dotnet ef`, so the database is created/upgraded on every start.
        public static void ApplyMigrations(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDBContext>().Database.Migrate();
        }
    }
}
