namespace Core.Entities
{
    // Exactly one row: the app has a single local user.
    public class Profile : EntityObject
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Summary { get; set; }
        public string? DesiredPosition { get; set; }

        public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
        public ICollection<Education> Educations { get; set; } = new List<Education>();
        public ICollection<Skill> Skills { get; set; } = new List<Skill>();
        public ICollection<ProfileLanguage> Languages { get; set; } = new List<ProfileLanguage>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
