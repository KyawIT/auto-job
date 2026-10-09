using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Persistence.Tests
{
    public class DomainQueryTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        [Fact]
        public async Task Profile_is_loaded_with_all_its_details()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = new Profile { FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com" };
            profile.WorkExperiences.Add(new WorkExperience { Company = "ACME", Role = "Developer", StartDate = new DateOnly(2020, 1, 1) });
            profile.Educations.Add(new Education { Institution = "TU Berlin", Degree = "B.Sc." });
            profile.Skills.Add(new Skill { Name = "C#" });
            profile.Languages.Add(new ProfileLanguage { Name = "English", Level = LanguageLevel.C1 });
            profile.Documents.Add(new Document { Type = DocumentType.Cv, DisplayName = "CV", OriginalFileName = "cv.pdf", StoredFilePath = "documents/cv.pdf", MimeType = "application/pdf" });
            await uow.Profiles.AddAsync(profile);
            await uow.SaveChangesAsync();

            var loaded = await _db.CreateUnitOfWork().Profiles.GetAsync();

            Assert.NotNull(loaded);
            Assert.Equal("ACME", Assert.Single(loaded.WorkExperiences).Company);
            Assert.Equal("TU Berlin", Assert.Single(loaded.Educations).Institution);
            Assert.Equal("C#", Assert.Single(loaded.Skills).Name);
            Assert.Equal("English", Assert.Single(loaded.Languages).Name);
            Assert.Equal("CV", Assert.Single(loaded.Documents).DisplayName);
        }

        private static JobPosting NewJob(JobSource source, string? externalId, string title = "Backend Developer") =>
            new() { Source = source, ExternalId = externalId, Title = title, CompanyName = "ACME", Description = "C# role" };

        [Fact]
        public async Task Job_is_found_by_its_source_and_external_id()
        {
            var uow = _db.CreateUnitOfWork();
            await uow.JobPostings.AddAsync(NewJob(JobSource.Adzuna, "123", "Adzuna job"));
            await uow.JobPostings.AddAsync(NewJob(JobSource.BundesagenturFuerArbeit, "123", "BA job"));
            await uow.SaveChangesAsync();

            var jobs = _db.CreateUnitOfWork().JobPostings;

            Assert.Equal("BA job", (await jobs.FindByExternalIdAsync(JobSource.BundesagenturFuerArbeit, "123"))?.Title);
            Assert.Null(await jobs.FindByExternalIdAsync(JobSource.Adzuna, "999"));
        }

        [Fact]
        public async Task Current_text_version_is_returned_per_kind()
        {
            var uow = _db.CreateUnitOfWork();
            var application = new Application { JobPosting = NewJob(JobSource.Adzuna, "A-1") };
            application.GeneratedTexts.Add(new GeneratedText { Kind = GeneratedTextKind.CoverLetter, Language = "de", Content = "Version 1" });
            application.GeneratedTexts.Add(new GeneratedText { Kind = GeneratedTextKind.CoverLetter, Language = "de", Content = "Version 2", IsCurrent = true });
            application.GeneratedTexts.Add(new GeneratedText { Kind = GeneratedTextKind.EmailBody, Language = "de", Content = "Email", IsCurrent = true });
            await uow.Applications.AddAsync(application);
            await uow.SaveChangesAsync();

            var texts = _db.CreateUnitOfWork().GeneratedTexts;

            Assert.Equal("Version 2", (await texts.GetCurrentAsync(application.Id, GeneratedTextKind.CoverLetter))?.Content);
            Assert.Null(await texts.GetCurrentAsync(application.Id, GeneratedTextKind.EmailSubject));
        }

        // A batch with one task per given status, each for its own job/application, saved in that order.
        private async Task<GenerationBatch> SeedBatchAsync(params GenerationTaskStatus[] statuses)
        {
            var uow = _db.CreateUnitOfWork();
            var batch = new GenerationBatch { Language = "de" };
            for (var i = 0; i < statuses.Length; i++)
            {
                var application = new Application { JobPosting = NewJob(JobSource.Adzuna, Guid.NewGuid().ToString(), $"Job {i}") };
                batch.Tasks.Add(new GenerationTask { Application = application, Status = statuses[i] });
            }
            await uow.GenerationBatches.AddAsync(batch);
            await uow.SaveChangesAsync();
            return batch;
        }

        [Fact]
        public async Task Worker_gets_oldest_queued_tasks_up_to_the_limit()
        {
            var batch = await SeedBatchAsync(
                GenerationTaskStatus.Done, GenerationTaskStatus.Queued, GenerationTaskStatus.Running,
                GenerationTaskStatus.Queued, GenerationTaskStatus.Queued);
            var tasks = batch.Tasks.ToList();

            var next = await _db.CreateUnitOfWork().GenerationTasks.GetQueuedAsync(2);

            Assert.Equal([tasks[1].Id, tasks[3].Id], next.Select(t => t.Id));
        }

        [Fact]
        public async Task Interrupted_running_tasks_are_requeued_on_startup()
        {
            var batch = await SeedBatchAsync(GenerationTaskStatus.Running, GenerationTaskStatus.Done, GenerationTaskStatus.Running);
            var tasks = batch.Tasks.ToList();

            var uow = _db.CreateUnitOfWork();
            var requeued = await uow.GenerationTasks.RequeueInterruptedAsync();
            await uow.SaveChangesAsync();

            Assert.Equal(2, requeued);
            var queued = await _db.CreateUnitOfWork().GenerationTasks.GetQueuedAsync(10);
            Assert.Equal([tasks[0].Id, tasks[2].Id], queued.Select(t => t.Id));
        }

        [Fact]
        public async Task Batch_progress_counts_its_tasks_by_status()
        {
            var batch = await SeedBatchAsync(
                GenerationTaskStatus.Done, GenerationTaskStatus.Done, GenerationTaskStatus.Done,
                GenerationTaskStatus.Failed, GenerationTaskStatus.Running, GenerationTaskStatus.Queued);
            await SeedBatchAsync(GenerationTaskStatus.Done);   // another batch must not be counted

            var progress = await _db.CreateUnitOfWork().GenerationBatches.GetProgressAsync(batch.Id);

            Assert.Equal(new BatchProgress(Total: 6, Queued: 1, Running: 1, Done: 3, Failed: 1, Cancelled: 0), progress);
        }

        [Fact]
        public async Task Settings_are_created_with_defaults_once_and_then_reused()
        {
            var first = _db.CreateUnitOfWork();
            var created = await first.AppSettings.GetAsync();
            created.DefaultLetterLanguage = "en";
            await first.SaveChangesAsync();

            var second = _db.CreateUnitOfWork();
            var loaded = await second.AppSettings.GetAsync();
            await second.SaveChangesAsync();

            Assert.Equal("en", loaded.DefaultLetterLanguage);
            Assert.Single(await _db.CreateUnitOfWork().AppSettings.GetAllAsync());
        }

        [Fact]
        public async Task Fresh_settings_use_german_and_din5008()
        {
            var settings = await _db.CreateUnitOfWork().AppSettings.GetAsync();

            Assert.Equal("de", settings.DefaultLetterLanguage);
            Assert.Equal(LetterLayout.Din5008, settings.LetterLayout);
        }

        [Fact]
        public async Task Application_is_loaded_with_job_attachments_texts_and_timeline()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = new Profile { FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com" };
            var application = new Application { JobPosting = NewJob(JobSource.Adzuna, "A-1", "Backend Developer") };
            application.Attachments.Add(new ApplicationDocument
            {
                Document = new Document { Profile = profile, Type = DocumentType.Cv, DisplayName = "CV", OriginalFileName = "cv.pdf", StoredFilePath = "documents/cv.pdf", MimeType = "application/pdf" }
            });
            application.GeneratedTexts.Add(new GeneratedText { Kind = GeneratedTextKind.CoverLetter, Language = "de", Content = "Sehr geehrte Damen und Herren", IsCurrent = true });
            application.Events.Add(new ApplicationEvent { Type = ApplicationEventType.Created, OccurredAt = new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc) });
            await uow.Applications.AddAsync(application);
            await uow.SaveChangesAsync();

            var loaded = await _db.CreateUnitOfWork().Applications.GetWithDetailsAsync(application.Id);

            Assert.NotNull(loaded);
            Assert.Equal("Backend Developer", loaded.JobPosting.Title);
            Assert.Equal("CV", Assert.Single(loaded.Attachments).Document.DisplayName);
            Assert.Equal("Sehr geehrte Damen und Herren", Assert.Single(loaded.GeneratedTexts).Content);
            Assert.Equal(ApplicationEventType.Created, Assert.Single(loaded.Events).Type);
        }
    }
}
