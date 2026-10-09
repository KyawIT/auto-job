using Core.Entities;
using Core.Enums;
using Core.Exceptions;

namespace Persistence.Tests
{
    public class IntegrityTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private static JobPosting NewJob(JobSource source, string? externalId) =>
            new() { Source = source, ExternalId = externalId, Title = "Backend Developer", CompanyName = "ACME", Description = "C# role" };

        [Fact]
        public async Task Same_job_from_the_same_source_is_rejected_as_duplicate()
        {
            var first = _db.CreateUnitOfWork();
            await first.JobPostings.AddAsync(NewJob(JobSource.Adzuna, "A-1"));
            await first.SaveChangesAsync();

            var second = _db.CreateUnitOfWork();
            await second.JobPostings.AddAsync(NewJob(JobSource.Adzuna, "A-1"));

            await Assert.ThrowsAsync<UniqueConstraintException>(() => second.SaveChangesAsync());
        }

        [Fact]
        public async Task Manually_entered_jobs_without_external_id_are_not_duplicates()
        {
            var uow = _db.CreateUnitOfWork();
            await uow.JobPostings.AddAsync(NewJob(JobSource.Manual, null));
            await uow.JobPostings.AddAsync(NewJob(JobSource.Manual, null));
            await uow.SaveChangesAsync();

            Assert.Equal(2, (await _db.CreateUnitOfWork().JobPostings.GetAllAsync()).Count);
        }

        [Fact]
        public async Task Second_application_for_the_same_job_is_rejected()
        {
            var uow = _db.CreateUnitOfWork();
            var job = NewJob(JobSource.Adzuna, "A-1");
            await uow.Applications.AddAsync(new Application { JobPosting = job });
            await uow.SaveChangesAsync();

            var second = _db.CreateUnitOfWork();
            await second.Applications.AddAsync(new Application { JobPostingId = job.Id });

            await Assert.ThrowsAsync<UniqueConstraintException>(() => second.SaveChangesAsync());
        }

        [Fact]
        public async Task Document_attached_to_an_application_cannot_be_deleted()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = new Profile { FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com" };
            var document = new Document { Profile = profile, Type = DocumentType.Cv, DisplayName = "CV", OriginalFileName = "cv.pdf", StoredFilePath = "documents/cv.pdf", MimeType = "application/pdf" };
            var application = new Application { JobPosting = NewJob(JobSource.Adzuna, "A-1") };
            application.Attachments.Add(new ApplicationDocument { Document = document });
            await uow.Applications.AddAsync(application);
            await uow.SaveChangesAsync();

            var delete = _db.CreateUnitOfWork();
            delete.Documents.Remove((await delete.Documents.GetByIdAsync(document.Id))!);

            await Assert.ThrowsAsync<ReferenceConstraintException>(() => delete.SaveChangesAsync());
        }

        [Fact]
        public async Task Job_with_an_application_cannot_be_deleted()
        {
            var uow = _db.CreateUnitOfWork();
            var job = NewJob(JobSource.Adzuna, "A-1");
            await uow.Applications.AddAsync(new Application { JobPosting = job });
            await uow.SaveChangesAsync();

            var delete = _db.CreateUnitOfWork();
            delete.JobPostings.Remove((await delete.JobPostings.GetByIdAsync(job.Id))!);

            await Assert.ThrowsAsync<ReferenceConstraintException>(() => delete.SaveChangesAsync());
        }

        [Fact]
        public async Task Deleting_a_saved_search_keeps_the_jobs_it_found()
        {
            var uow = _db.CreateUnitOfWork();
            var search = new SavedSearch { Name = "Dev", Keywords = ".NET" };
            var job = NewJob(JobSource.Adzuna, "A-1");
            job.SavedSearch = search;
            await uow.JobPostings.AddAsync(job);
            await uow.SaveChangesAsync();

            var delete = _db.CreateUnitOfWork();
            delete.SavedSearches.Remove((await delete.SavedSearches.GetByIdAsync(search.Id))!);
            await delete.SaveChangesAsync();

            var kept = await _db.CreateUnitOfWork().JobPostings.GetByIdAsync(job.Id);
            Assert.NotNull(kept);
            Assert.Null(kept.SavedSearchId);
        }
    }
}
