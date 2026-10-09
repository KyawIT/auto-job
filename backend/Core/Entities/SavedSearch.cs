using Core.Enums;

namespace Core.Entities
{
    public class SavedSearch : EntityObject
    {
        public string Name { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int? RadiusKm { get; set; }
        public List<JobSource> Sources { get; set; } = new();
        public int RefreshIntervalMinutes { get; set; } = 60;
        public DateTime? LastRunAt { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }
}
