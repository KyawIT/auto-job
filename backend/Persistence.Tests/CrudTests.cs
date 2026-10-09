using Core.Entities;
using Core.Enums;

namespace Persistence.Tests
{
    public class CrudTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        [Fact]
        public async Task Saved_profile_can_be_loaded_by_id()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = new Profile { FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com" };
            await uow.Profiles.AddAsync(profile);
            await uow.SaveChangesAsync();

            var loaded = await _db.CreateUnitOfWork().Profiles.GetByIdAsync(profile.Id);

            Assert.NotNull(loaded);
            Assert.Equal("Anna", loaded.FirstName);
            Assert.Equal("anna@example.com", loaded.Email);
        }

        [Fact]
        public async Task Removed_entity_can_no_longer_be_loaded()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = new Profile { FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com" };
            await uow.Profiles.AddAsync(profile);
            await uow.SaveChangesAsync();

            var second = _db.CreateUnitOfWork();
            second.Profiles.Remove((await second.Profiles.GetByIdAsync(profile.Id))!);
            await second.SaveChangesAsync();

            Assert.Null(await _db.CreateUnitOfWork().Profiles.GetByIdAsync(profile.Id));
        }

        [Fact]
        public async Task All_saved_searches_are_listed()
        {
            var uow = _db.CreateUnitOfWork();
            await uow.SavedSearches.AddAsync(new SavedSearch { Name = "Dev Berlin", Keywords = ".NET", Sources = [JobSource.Adzuna] });
            await uow.SavedSearches.AddAsync(new SavedSearch { Name = "Support", Keywords = "IT Support", Sources = [JobSource.Adzuna, JobSource.BundesagenturFuerArbeit] });
            await uow.SaveChangesAsync();

            var all = await _db.CreateUnitOfWork().SavedSearches.GetAllAsync();

            Assert.Equal(["Dev Berlin", "Support"], all.Select(s => s.Name).Order());
            Assert.Equal([JobSource.Adzuna, JobSource.BundesagenturFuerArbeit], all.Single(s => s.Name == "Support").Sources);
        }

        [Fact]
        public async Task Every_entity_type_round_trips_through_its_repository()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = new Profile { FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com" };
            var experience = new WorkExperience { Profile = profile, Company = "ACME", Role = "Developer", StartDate = new DateOnly(2020, 1, 1) };
            var education = new Education { Profile = profile, Institution = "TU Berlin", Degree = "B.Sc. Informatik", Grade = "1,7" };
            var skill = new Skill { Profile = profile, Name = "C#", Level = SkillLevel.Advanced };
            var language = new ProfileLanguage { Profile = profile, Name = "English", Level = LanguageLevel.C1 };
            var document = new Document { Profile = profile, Type = DocumentType.DegreeCertificate, DisplayName = "Bachelorzeugnis", OriginalFileName = "zeugnis.pdf", StoredFilePath = "documents/1.pdf", MimeType = "application/pdf" };
            var job = new JobPosting { Source = JobSource.Adzuna, ExternalId = "A-1", Title = "Backend Developer", CompanyName = "ACME", Description = "C# role" };
            var application = new Application { JobPosting = job, Language = "en" };
            var text = new GeneratedText { Application = application, Kind = GeneratedTextKind.CoverLetter, Language = "en", Content = "Dear ACME", IsCurrent = true };
            var appEvent = new ApplicationEvent { Application = application, Type = ApplicationEventType.Created, OccurredAt = new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc) };
            var batch = new GenerationBatch { Language = "en" };
            var task = new GenerationTask { GenerationBatch = batch, Application = application };
            var settings = new AppSettings { DefaultLetterLanguage = "fr" };

            await uow.Profiles.AddAsync(profile);
            await uow.WorkExperiences.AddAsync(experience);
            await uow.Educations.AddAsync(education);
            await uow.Skills.AddAsync(skill);
            await uow.ProfileLanguages.AddAsync(language);
            await uow.Documents.AddAsync(document);
            await uow.JobPostings.AddAsync(job);
            await uow.Applications.AddAsync(application);
            await uow.GeneratedTexts.AddAsync(text);
            await uow.ApplicationEvents.AddAsync(appEvent);
            await uow.GenerationBatches.AddAsync(batch);
            await uow.GenerationTasks.AddAsync(task);
            await uow.AppSettings.AddAsync(settings);
            await uow.SaveChangesAsync();

            var read = _db.CreateUnitOfWork();
            Assert.Equal("ACME", (await read.WorkExperiences.GetByIdAsync(experience.Id))!.Company);
            Assert.Equal("1,7", (await read.Educations.GetByIdAsync(education.Id))!.Grade);
            Assert.Equal(SkillLevel.Advanced, (await read.Skills.GetByIdAsync(skill.Id))!.Level);
            Assert.Equal(LanguageLevel.C1, (await read.ProfileLanguages.GetByIdAsync(language.Id))!.Level);
            Assert.Equal("Bachelorzeugnis", (await read.Documents.GetByIdAsync(document.Id))!.DisplayName);
            Assert.Equal("A-1", (await read.JobPostings.GetByIdAsync(job.Id))!.ExternalId);
            Assert.Equal(job.Id, (await read.Applications.GetByIdAsync(application.Id))!.JobPostingId);
            Assert.Equal("Dear ACME", (await read.GeneratedTexts.GetByIdAsync(text.Id))!.Content);
            Assert.Equal(ApplicationEventType.Created, (await read.ApplicationEvents.GetByIdAsync(appEvent.Id))!.Type);
            Assert.Equal(BatchStatus.Running, (await read.GenerationBatches.GetByIdAsync(batch.Id))!.Status);
            Assert.Equal(GenerationTaskStatus.Queued, (await read.GenerationTasks.GetByIdAsync(task.Id))!.Status);
            Assert.Equal("fr", (await read.AppSettings.GetByIdAsync(settings.Id))!.DefaultLetterLanguage);
        }
    }
}
