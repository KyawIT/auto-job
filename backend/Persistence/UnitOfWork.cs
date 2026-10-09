using Core.Contracts;
using Core.Contracts.Repositories;
using Core.Entities;
using Core.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories;

namespace Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDBContext _context;

        public UnitOfWork(ApplicationDBContext context)
        {
            _context = context;

            Profiles = new ProfileRepository(context);
            WorkExperiences = new Repository<WorkExperience>(context);
            Educations = new Repository<Education>(context);
            Skills = new Repository<Skill>(context);
            ProfileLanguages = new Repository<ProfileLanguage>(context);
            Documents = new Repository<Document>(context);

            SavedSearches = new Repository<SavedSearch>(context);
            JobPostings = new JobPostingRepository(context);

            Applications = new ApplicationRepository(context);
            GeneratedTexts = new GeneratedTextRepository(context);
            ApplicationEvents = new Repository<ApplicationEvent>(context);

            GenerationBatches = new GenerationBatchRepository(context);
            GenerationTasks = new GenerationTaskRepository(context);

            AppSettings = new AppSettingsRepository(context);
        }

        public IProfileRepository Profiles { get; }
        public IRepository<WorkExperience> WorkExperiences { get; }
        public IRepository<Education> Educations { get; }
        public IRepository<Skill> Skills { get; }
        public IRepository<ProfileLanguage> ProfileLanguages { get; }
        public IRepository<Document> Documents { get; }

        public IRepository<SavedSearch> SavedSearches { get; }
        public IJobPostingRepository JobPostings { get; }

        public IApplicationRepository Applications { get; }
        public IGeneratedTextRepository GeneratedTexts { get; }
        public IRepository<ApplicationEvent> ApplicationEvents { get; }

        public IGenerationBatchRepository GenerationBatches { get; }
        public IGenerationTaskRepository GenerationTasks { get; }

        public IAppSettingsRepository AppSettings { get; }

        // Translates EF/SQLite errors into Core exceptions so callers never depend on EF.
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyConflictException(ex);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolation })
            {
                throw new UniqueConstraintException(ex);
            }
            catch (DbUpdateException ex) when (IsForeignKeyViolation(ex.InnerException))
            {
                throw new ReferenceConstraintException(ex);
            }
        }

        private const int SqliteConstraint = 19;              // SQLITE_CONSTRAINT
        private const int SqliteUniqueViolation = 2067;       // SQLITE_CONSTRAINT_UNIQUE

        // ON DELETE RESTRICT reports SQLITE_CONSTRAINT_TRIGGER (1811), other FK errors
        // SQLITE_CONSTRAINT_FOREIGNKEY (787); both carry the same message.
        private static bool IsForeignKeyViolation(Exception? ex) =>
            ex is SqliteException { SqliteErrorCode: SqliteConstraint } sqlite
            && sqlite.Message.Contains("FOREIGN KEY", StringComparison.Ordinal);
    }
}
