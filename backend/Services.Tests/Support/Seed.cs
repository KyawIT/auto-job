using Core.Contracts;
using Core.Entities;
using Core.Enums;

namespace Services.Tests.Support
{
    // Minimal valid data, written straight through the UnitOfWork (setup only, never asserted on).
    public static class Seed
    {
        public static async Task<Profile> ProfileAsync(IUnitOfWork uow)
        {
            var profile = new Profile
            {
                FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com", City = "Berlin",
                Summary = "Backend developer with 5 years of C#.", DesiredPosition = ".NET Developer",
            };
            profile.WorkExperiences.Add(new WorkExperience { Company = "ACME GmbH", Role = "Software Developer", StartDate = new DateOnly(2021, 3, 1) });
            profile.Skills.Add(new Skill { Name = "C#", Level = SkillLevel.Expert });
            await uow.Profiles.AddAsync(profile);
            await uow.SaveChangesAsync();
            return profile;
        }

        public static async Task<Document> DocumentAsync(IUnitOfWork uow, Profile profile, string name, bool attachByDefault, string? text = null)
        {
            var document = new Document
            {
                ProfileId = profile.Id, Type = DocumentType.Certification, DisplayName = name, OriginalFileName = $"{name}.pdf",
                StoredFilePath = $"documents/{name}.pdf", MimeType = "application/pdf", AttachByDefault = attachByDefault,
                ExtractedText = text, ExtractionStatus = text == null ? ExtractionStatus.NotSupported : ExtractionStatus.Done,
            };
            await uow.Documents.AddAsync(document);
            await uow.SaveChangesAsync();
            return document;
        }

        public static async Task<JobPosting> JobAsync(IUnitOfWork uow, string title = "Backend Developer", string company = "Contoso AG",
            string? contactEmail = "jobs@contoso.example", string? language = null)
        {
            var job = new JobPosting
            {
                Source = JobSource.Manual, Title = title, CompanyName = company, Location = "Berlin",
                Description = "Wir suchen eine:n Backend Developer (C#, ASP.NET Core).", ContactEmail = contactEmail,
                ContactName = "Frau Müller", DetectedLanguage = language,
            };
            await uow.JobPostings.AddAsync(job);
            await uow.SaveChangesAsync();
            return job;
        }
    }
}
