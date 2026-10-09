using Core.Enums;

namespace Core.Entities
{
    public class JobPosting : EntityObject
    {
        // Null when found by a one-off search or entered manually.
        public int? SavedSearchId { get; set; }
        public SavedSearch? SavedSearch { get; set; }

        // Source + ExternalId is unique, so refreshing a search doesn't create duplicates.
        public JobSource Source { get; set; }
        public string? ExternalId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Url { get; set; }
        // Whole currency units; SQLite can't sort or compare decimals.
        public int? SalaryMin { get; set; }
        public int? SalaryMax { get; set; }
        public string? SalaryCurrency { get; set; }
        public EmploymentType EmploymentType { get; set; }
        public WorkMode WorkMode { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactName { get; set; }
        public DateTime? PostedAt { get; set; }
        public string? DetectedLanguage { get; set; }   // ISO code, e.g. "de", "en"
        public JobPostingState State { get; set; } = JobPostingState.New;

        public Application? Application { get; set; }
    }
}
