using Core.Contracts;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

        // Profile
        public DbSet<Profile> Profiles => Set<Profile>();
        public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
        public DbSet<Education> Educations => Set<Education>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<ProfileLanguage> ProfileLanguages => Set<ProfileLanguage>();
        public DbSet<Document> Documents => Set<Document>();

        // Jobs
        public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
        public DbSet<JobPosting> JobPostings => Set<JobPosting>();

        // Applications
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<GeneratedText> GeneratedTexts => Set<GeneratedText>();
        public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
        public DbSet<ApplicationEvent> ApplicationEvents => Set<ApplicationEvent>();

        // Batch generation
        public DbSet<GenerationBatch> GenerationBatches => Set<GenerationBatch>();
        public DbSet<GenerationTask> GenerationTasks => Set<GenerationTask>();

        // Settings
        public DbSet<AppSettings> AppSettings => Set<AppSettings>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Enums as readable text ("Draft", "Sent") instead of numbers.
            configurationBuilder.Properties<Enum>().HaveConversion<string>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDBContext).Assembly);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampEntities();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampEntities();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void StampEntities()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<IEntityObject>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.RowVersion = Guid.NewGuid();
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.RowVersion = Guid.NewGuid();
                }
            }
        }
    }
}
