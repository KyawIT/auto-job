using Core.Enums;

namespace Core.Entities
{
    // The user's application for one job posting (one per posting).
    public class Application : EntityObject
    {
        public int JobPostingId { get; set; }
        public JobPosting JobPosting { get; set; } = null!;

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
        public string Language { get; set; } = "de";   // ISO code the letter is written in
        public string? RecipientEmail { get; set; }
        public string? RecipientName { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ResponseAt { get; set; }
        public string? Notes { get; set; }

        public ICollection<GeneratedText> GeneratedTexts { get; set; } = new List<GeneratedText>();
        public ICollection<ApplicationEvent> Events { get; set; } = new List<ApplicationEvent>();
        public ICollection<ApplicationDocument> Attachments { get; set; } = new List<ApplicationDocument>();
        public ICollection<GenerationTask> GenerationTasks { get; set; } = new List<GenerationTask>();
    }
}
