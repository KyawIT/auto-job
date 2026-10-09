using Core.Contracts.Repositories;
using Core.Entities;

namespace Core.Contracts
{
    public interface IUnitOfWork
    {
        // Profile
        IProfileRepository Profiles { get; }
        IRepository<WorkExperience> WorkExperiences { get; }
        IRepository<Education> Educations { get; }
        IRepository<Skill> Skills { get; }
        IRepository<ProfileLanguage> ProfileLanguages { get; }
        IRepository<Document> Documents { get; }

        // Jobs
        IRepository<SavedSearch> SavedSearches { get; }
        IJobPostingRepository JobPostings { get; }

        // Applications (attachments are managed through Application.Attachments)
        IApplicationRepository Applications { get; }
        IGeneratedTextRepository GeneratedTexts { get; }
        IRepository<ApplicationEvent> ApplicationEvents { get; }

        // Batch generation
        IGenerationBatchRepository GenerationBatches { get; }
        IGenerationTaskRepository GenerationTasks { get; }

        // Settings
        IAppSettingsRepository AppSettings { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
