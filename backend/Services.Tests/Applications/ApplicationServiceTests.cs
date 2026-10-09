using Core.Entities;
using Core.Enums;
using Microsoft.Extensions.Time.Testing;
using Persistence.Tests;
using Services.Applications;
using Services.Common;
using Services.Tests.Support;

namespace Services.Tests.Applications
{
    public class ApplicationServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero));
        private readonly Profile _profile;

        public ApplicationServiceTests()
        {
            _profile = Seed.ProfileAsync(_db.CreateUnitOfWork()).GetAwaiter().GetResult();
        }

        public void Dispose() => _db.Dispose();

        private ApplicationService Service() => new(_db.CreateUnitOfWork(), _time);

        [Fact]
        public async Task Started_application_is_a_draft_with_defaults_from_settings_job_and_documents()
        {
            var uow = _db.CreateUnitOfWork();
            (await uow.AppSettings.GetAsync()).DefaultLetterLanguage = "en";
            await uow.SaveChangesAsync();
            var cv = await Seed.DocumentAsync(uow, _profile, "Lebenslauf", attachByDefault: true);
            await Seed.DocumentAsync(uow, _profile, "Führerschein", attachByDefault: false);
            var job = await Seed.JobAsync(uow);

            var started = await Service().StartAsync(job.Id);

            var application = await Service().GetAsync(started.Id);
            Assert.Equal(ApplicationStatus.Draft, application.Status);
            Assert.Equal("en", application.Language);
            Assert.Equal("jobs@contoso.example", application.RecipientEmail);
            Assert.Equal("Frau Müller", application.RecipientName);
            Assert.Equal("Backend Developer", application.Job.Title);
            Assert.Equal([cv.Id], application.Attachments.Select(a => a.DocumentId));
            Assert.Equal(ApplicationEventType.Created, Assert.Single(application.Events).Type);
        }

        [Fact]
        public async Task Starting_again_for_the_same_job_returns_the_existing_application()
        {
            var job = await Seed.JobAsync(_db.CreateUnitOfWork(), language: "fr");

            var first = await Service().StartAsync(job.Id);
            var second = await Service().StartAsync(job.Id);

            Assert.Equal(first.Id, second.Id);
            Assert.Equal("fr", second.Language);     // the posting's language beats the default
        }

        [Fact]
        public async Task Status_changes_are_timestamped_and_recorded_in_the_timeline()
        {
            var application = await Service().StartAsync((await Seed.JobAsync(_db.CreateUnitOfWork())).Id);

            await Service().ChangeStatusAsync(application.Id, new ChangeStatusRequest(ApplicationStatus.Sent));
            _time.Advance(TimeSpan.FromDays(5));
            await Service().ChangeStatusAsync(application.Id, new ChangeStatusRequest(ApplicationStatus.Interview, "Call with Frau Müller"));

            var result = await Service().GetAsync(application.Id);
            Assert.Equal(ApplicationStatus.Interview, result.Status);
            Assert.Equal(new DateTime(2026, 10, 9, 10, 0, 0), result.SentAt);
            Assert.Equal(new DateTime(2026, 10, 14, 10, 0, 0), result.ResponseAt);
            Assert.Equal(
                [(ApplicationEventType.Created, null), (ApplicationEventType.StatusChanged, "Draft → Sent"), (ApplicationEventType.StatusChanged, "Sent → Interview: Call with Frau Müller")],
                result.Events.Select(e => (e.Type, e.Note)));
        }

        [Fact]
        public async Task Attachments_are_replaced_in_the_given_order()
        {
            var uow = _db.CreateUnitOfWork();
            var cv = await Seed.DocumentAsync(uow, _profile, "Lebenslauf", attachByDefault: true);
            var zeugnis = await Seed.DocumentAsync(uow, _profile, "Zeugnis", attachByDefault: false);
            var application = await Service().StartAsync((await Seed.JobAsync(uow)).Id);

            await Service().SetAttachmentsAsync(application.Id, [zeugnis.Id, cv.Id]);

            Assert.Equal([zeugnis.Id, cv.Id], (await Service().GetAsync(application.Id)).Attachments.Select(a => a.DocumentId));
            await Assert.ThrowsAsync<ValidationException>(() => Service().SetAttachmentsAsync(application.Id, [999]));
        }

        [Fact]
        public async Task Applications_are_listed_by_status_newest_first()
        {
            var uow = _db.CreateUnitOfWork();
            var draft = await Service().StartAsync((await Seed.JobAsync(uow, "Backend Developer")).Id);
            var sent = await Service().StartAsync((await Seed.JobAsync(uow, "API Developer")).Id);
            await Service().ChangeStatusAsync(sent.Id, new ChangeStatusRequest(ApplicationStatus.Sent));

            Assert.Equal(["API Developer"], (await Service().ListAsync(ApplicationStatus.Sent)).Select(a => a.JobTitle));
            Assert.Equal(["API Developer", "Backend Developer"], (await Service().ListAsync(null)).Select(a => a.JobTitle));
        }

        [Fact]
        public async Task Note_is_added_to_timeline_and_deleting_keeps_the_job()
        {
            var job = await Seed.JobAsync(_db.CreateUnitOfWork());
            var application = await Service().StartAsync(job.Id);

            await Service().AddNoteAsync(application.Id, "Called HR, they will answer next week");
            Assert.Equal("Called HR, they will answer next week", (await Service().GetAsync(application.Id)).Events.Last().Note);

            await Service().DeleteAsync(application.Id);
            await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync(application.Id));
            Assert.NotNull(await _db.CreateUnitOfWork().JobPostings.GetByIdAsync(job.Id));
        }

        [Fact]
        public async Task Recipient_language_and_notes_can_be_edited()
        {
            var application = await Service().StartAsync((await Seed.JobAsync(_db.CreateUnitOfWork())).Id);

            await Service().UpdateAsync(application.Id, new UpdateApplicationRequest("EN", "hr@contoso.example", "Mr Smith", "Referral from Tom"));

            var result = await Service().GetAsync(application.Id);
            Assert.Equal(("en", "hr@contoso.example", "Mr Smith", "Referral from Tom"), (result.Language, result.RecipientEmail, result.RecipientName, result.Notes));
            await Assert.ThrowsAsync<ValidationException>(() =>
                Service().UpdateAsync(application.Id, new UpdateApplicationRequest("en", "not-an-email", null, null)));
        }
    }
}
