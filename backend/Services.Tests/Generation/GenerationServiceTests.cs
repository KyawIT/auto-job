using Core.Entities;
using Core.Enums;
using Microsoft.Extensions.Time.Testing;
using Persistence.Tests;
using Services.Applications;
using Services.Generation;
using Services.Tests.Support;

namespace Services.Tests.Generation
{
    public class GenerationServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero));
        private readonly FakeAiClient _ai = new();
        private readonly Profile _profile;

        public GenerationServiceTests()
        {
            _profile = Seed.ProfileAsync(_db.CreateUnitOfWork()).GetAwaiter().GetResult();
        }

        public void Dispose() => _db.Dispose();

        private GenerationService Service() => new(_db.CreateUnitOfWork(), _ai, _time);

        private ApplicationService Applications() => new(_db.CreateUnitOfWork(), _time);

        private async Task<int> StartApplicationAsync(string? language = null)
        {
            var job = await Seed.JobAsync(_db.CreateUnitOfWork());
            return (await Applications().StartAsync(job.Id, new StartApplicationRequest(language))).Id;
        }

        [Fact]
        public async Task Generating_stores_letter_subject_and_email_as_current_texts()
        {
            var applicationId = await StartApplicationAsync("de");
            _ai.ReturnsLetter("Sehr geehrte Frau Müller, ...", "Bewerbung als Backend Developer", "Sehr geehrte Frau Müller, anbei ...");

            var result = await Service().GenerateAsync(applicationId);

            Assert.Equal(
                [(GeneratedTextKind.CoverLetter, "Sehr geehrte Frau Müller, ..."),
                 (GeneratedTextKind.EmailSubject, "Bewerbung als Backend Developer"),
                 (GeneratedTextKind.EmailBody, "Sehr geehrte Frau Müller, anbei ...")],
                result.Texts.Select(t => (t.Kind, t.Content)));
            Assert.All(result.Texts, t => Assert.Equal("de", t.Language));
            Assert.Equal(ApplicationEventType.Generated, result.Events.Last().Type);
        }

        [Fact]
        public async Task Ai_gets_job_profile_document_facts_and_the_target_language()
        {
            await Seed.DocumentAsync(_db.CreateUnitOfWork(), _profile, "Bachelorzeugnis", attachByDefault: false, text: "Bachelor of Science Informatik, Gesamtnote 1,7");
            var applicationId = await StartApplicationAsync("en");
            _ai.ReturnsLetter("Dear Ms Müller, ...");

            await Service().GenerateAsync(applicationId);

            var prompt = _ai.LastPrompt;
            Assert.Contains("Write in English", prompt);
            Assert.Contains("Wir suchen eine:n Backend Developer (C#, ASP.NET Core).", prompt);   // job description
            Assert.Contains("Software Developer at ACME GmbH", prompt);                            // work experience
            Assert.Contains("C# (Expert)", prompt);                                                // skills
            Assert.Contains("Gesamtnote 1,7", prompt);                                             // document text
            Assert.Contains("Frau Müller", prompt);                                                // recipient
            Assert.Equal(GenerationService.DefaultModel, _ai.Requests.Single().Model);
        }

        [Fact]
        public async Task Versions_are_kept_edits_are_marked_and_an_older_version_can_be_restored()
        {
            var applicationId = await StartApplicationAsync();
            _ai.ReturnsLetter("Version 1").ReturnsLetter("Version 2");

            var first = await Service().GenerateAsync(applicationId);
            var v1 = first.Texts.Single(t => t.Kind == GeneratedTextKind.CoverLetter);
            await Service().EditTextAsync(v1.Id, "Version 1, edited by me");
            await Service().GenerateAsync(applicationId);

            var versions = await Service().ListVersionsAsync(applicationId, GeneratedTextKind.CoverLetter);
            Assert.Equal([("Version 2", true, false), ("Version 1, edited by me", false, true)], versions.Select(v => (v.Content, v.IsCurrent, v.IsEdited)));

            var restored = await Service().SelectVersionAsync(v1.Id);
            Assert.Equal("Version 1, edited by me", restored.Texts.Single(t => t.Kind == GeneratedTextKind.CoverLetter).Content);
        }

        [Fact]
        public async Task Unusable_answer_stores_nothing()
        {
            var applicationId = await StartApplicationAsync();
            _ai.Returns("Sorry, I can't help with that.");

            await Assert.ThrowsAsync<Services.Ports.AiRequestException>(() => Service().GenerateAsync(applicationId));

            Assert.Empty((await Applications().GetAsync(applicationId)).Texts);
        }

        [Fact]
        public async Task Json_wrapped_in_a_code_fence_is_accepted()
        {
            var applicationId = await StartApplicationAsync();
            _ai.Returns("""
                Here is the application:
                ```json
                {"coverLetter": "Dear team", "emailSubject": "Application", "emailBody": "Please find attached"}
                ```
                """);

            var result = await Service().GenerateAsync(applicationId);

            Assert.Equal("Dear team", result.Texts.Single(t => t.Kind == GeneratedTextKind.CoverLetter).Content);
        }

        [Fact]
        public async Task Model_from_settings_is_used()
        {
            var uow = _db.CreateUnitOfWork();
            (await uow.AppSettings.GetAsync()).NimModel = "mistralai/mistral-large-2-instruct";
            await uow.SaveChangesAsync();
            var applicationId = await StartApplicationAsync();
            _ai.ReturnsLetter("...");

            var result = await Service().GenerateAsync(applicationId);

            Assert.Equal("mistralai/mistral-large-2-instruct", _ai.Requests.Single().Model);
            Assert.All(result.Texts, t => Assert.Equal("mistralai/mistral-large-2-instruct", t.Model));
        }
    }
}
