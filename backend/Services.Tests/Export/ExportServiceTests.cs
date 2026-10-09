using System.Text;
using Core.Entities;
using Core.Enums;
using Microsoft.Extensions.Time.Testing;
using Persistence.Tests;
using Services.Applications;
using Services.Common;
using Services.Export;
using Services.Generation;
using Services.Tests.Support;

namespace Services.Tests.Export
{
    public class ExportServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero));
        private readonly FakeAiClient _ai = new();
        private readonly FakeFileStorage _files = new();
        private readonly FakeCoverLetterRenderer _renderer = new();
        private readonly FakeEmailDraftBuilder _drafts = new();
        private readonly Profile _profile;

        public ExportServiceTests()
        {
            _profile = Seed.ProfileAsync(_db.CreateUnitOfWork()).GetAwaiter().GetResult();
        }

        public void Dispose() => _db.Dispose();

        private ExportService Service() => new(_db.CreateUnitOfWork(), _files, _renderer, _drafts, _time);

        private async Task<ApplicationDetailsDto> GeneratedApplicationAsync(string language = "de")
        {
            var job = await Seed.JobAsync(_db.CreateUnitOfWork());
            var application = await new ApplicationService(_db.CreateUnitOfWork(), _time).StartAsync(job.Id, new StartApplicationRequest(language));
            _ai.ReturnsLetter("Sehr geehrte Frau Müller, ...", "Bewerbung als Backend Developer", "Anbei meine Unterlagen.");
            return await new GenerationService(_db.CreateUnitOfWork(), _ai, _time).GenerateAsync(application.Id);
        }

        [Fact]
        public async Task Pdf_uses_the_current_letter_with_sender_and_recipient()
        {
            var application = await GeneratedApplicationAsync();
            var letter = application.Texts.Single(t => t.Kind == GeneratedTextKind.CoverLetter);
            await new GenerationService(_db.CreateUnitOfWork(), _ai, _time).EditTextAsync(letter.Id, "Mein überarbeiteter Brief");

            var file = await Service().ExportLetterAsync(application.Id, ExportFormat.Pdf);

            Assert.Equal("Anschreiben_Contoso_AG.pdf", file.FileName);
            Assert.Equal("application/pdf", file.MimeType);
            Assert.Equal("PDF:Mein überarbeiteter Brief", Encoding.UTF8.GetString(file.Content));
            var rendered = _renderer.Rendered.Single();
            Assert.Equal("Anna Schmidt", rendered.Sender.Name);
            Assert.Contains("anna@example.com", rendered.Sender.Lines);
            Assert.Equal("Contoso AG", rendered.Recipient.Name);
            Assert.Equal(["Frau Müller", "Berlin"], rendered.Recipient.Lines);
            Assert.Equal("Bewerbung als Backend Developer", rendered.Subject);
            Assert.Equal(new DateOnly(2026, 10, 9), rendered.Date);
            Assert.Equal(ApplicationEventType.Exported, (await new ApplicationService(_db.CreateUnitOfWork(), _time).GetAsync(application.Id)).Events.Last().Type);
        }

        [Fact]
        public async Task Nothing_to_export_before_a_letter_exists()
        {
            var job = await Seed.JobAsync(_db.CreateUnitOfWork());
            var application = await new ApplicationService(_db.CreateUnitOfWork(), _time).StartAsync(job.Id);

            var ex = await Assert.ThrowsAsync<ValidationException>(() => Service().ExportLetterAsync(application.Id, ExportFormat.Pdf));

            Assert.Equal(["CoverLetter"], ex.Errors.Keys);
        }

        [Fact]
        public async Task Email_draft_has_texts_letter_pdf_and_attached_documents_and_is_saved()
        {
            var zeugnis = await Seed.DocumentAsync(_db.CreateUnitOfWork(), _profile, "Zeugnis", attachByDefault: true);
            _files.Files[zeugnis.StoredFilePath] = Encoding.UTF8.GetBytes("%PDF zeugnis");
            var application = await GeneratedApplicationAsync();

            var draft = await Service().CreateEmailDraftAsync(application.Id);

            var email = _drafts.Built.Single();
            Assert.Equal(("anna@example.com", "jobs@contoso.example", "Bewerbung als Backend Developer", "Anbei meine Unterlagen."),
                (email.From, email.To, email.Subject, email.Body));
            Assert.Equal(["Anschreiben_Contoso_AG.pdf", "Zeugnis.pdf"], email.Attachments.Select(a => a.FileName));
            Assert.Equal("%PDF zeugnis", Encoding.UTF8.GetString(email.Attachments[1].Content));

            Assert.StartsWith("drafts/", draft.RelativePath);
            Assert.EndsWith(".eml", draft.RelativePath);
            Assert.Equal("EML:Bewerbung als Backend Developer", Encoding.UTF8.GetString(_files.Files[draft.RelativePath]));
            Assert.Equal(_files.GetFullPath(draft.RelativePath), draft.FullPath);
            Assert.Equal(ApplicationEventType.EmailOpened, (await new ApplicationService(_db.CreateUnitOfWork(), _time).GetAsync(application.Id)).Events.Last().Type);
        }
    }
}
