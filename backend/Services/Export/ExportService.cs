using System.Text.RegularExpressions;
using Core.Contracts;
using Core.Entities;
using Core.Enums;
using Services.Common;
using Services.Ports;

namespace Services.Export
{
    public enum ExportFormat
    {
        Pdf,
        Docx
    }

    public record ExportedFile(byte[] Content, string FileName, string MimeType);

    // FullPath is what Electron opens with the system mail client (shell.openPath).
    public record EmailDraftDto(string RelativePath, string FullPath);

    // Turns the current texts of an application into files: the cover letter (PDF/DOCX)
    // and a ready-to-send email draft (.eml) with the letter and the attached documents.
    public partial class ExportService
    {
        private const string PdfMime = "application/pdf";
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private readonly IUnitOfWork _uow;
        private readonly IFileStorage _files;
        private readonly ICoverLetterRenderer _renderer;
        private readonly IEmailDraftBuilder _drafts;
        private readonly TimeProvider _time;

        public ExportService(IUnitOfWork uow, IFileStorage files, ICoverLetterRenderer renderer, IEmailDraftBuilder drafts, TimeProvider time)
        {
            _uow = uow;
            _files = files;
            _renderer = renderer;
            _drafts = drafts;
            _time = time;
        }

        public async Task<ExportedFile> ExportLetterAsync(int applicationId, ExportFormat format, CancellationToken ct = default)
        {
            var (application, profile, settings) = await LoadAsync(applicationId, ct);
            var file = RenderLetter(application, profile, settings, format);

            AddEvent(application, ApplicationEventType.Exported, format.ToString().ToUpperInvariant());
            await _uow.SaveChangesAsync(ct);
            return file;
        }

        public async Task<EmailDraftDto> CreateEmailDraftAsync(int applicationId, CancellationToken ct = default)
        {
            var (application, profile, settings) = await LoadAsync(applicationId, ct);

            var letter = RenderLetter(application, profile, settings, ExportFormat.Pdf);
            var attachments = new List<EmailAttachment> { new(letter.FileName, letter.MimeType, letter.Content) };
            foreach (var attached in application.Attachments.OrderBy(a => a.SortOrder))
                attachments.Add(new EmailAttachment(attached.Document.OriginalFileName, attached.Document.MimeType, await ReadAsync(attached.Document.StoredFilePath, ct)));

            var eml = _drafts.Build(new EmailDraft(
                From: profile.Email,
                To: application.RecipientEmail,
                Subject: Current(application, GeneratedTextKind.EmailSubject) ?? application.JobPosting.Title,
                Body: Current(application, GeneratedTextKind.EmailBody) ?? "",
                Attachments: attachments));
            var path = await _files.SaveAsync(new MemoryStream(eml), "drafts", ".eml", ct);

            AddEvent(application, ApplicationEventType.EmailOpened, application.RecipientEmail);
            await _uow.SaveChangesAsync(ct);
            return new EmailDraftDto(path, _files.GetFullPath(path));
        }

        private async Task<byte[]> ReadAsync(string relativePath, CancellationToken ct)
        {
            await using var file = _files.OpenRead(relativePath);
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }

        private ExportedFile RenderLetter(Application application, Profile profile, AppSettings settings, ExportFormat format)
        {
            var letter = new LetterContent(
                Sender: new LetterAddress($"{profile.FirstName} {profile.LastName}", NonEmpty(
                    profile.Street,
                    $"{profile.PostalCode} {profile.City}".Trim(),
                    profile.Phone,
                    profile.Email)),
                Recipient: new LetterAddress(application.JobPosting.CompanyName, NonEmpty(application.RecipientName, application.JobPosting.Location)),
                Place: profile.City,
                Date: DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime),
                Subject: Current(application, GeneratedTextKind.EmailSubject) ?? application.JobPosting.Title,
                Body: Current(application, GeneratedTextKind.CoverLetter)!,
                Language: application.Language,
                Layout: settings.LetterLayout);

            var name = $"{(application.Language == "de" ? "Anschreiben" : "Cover_Letter")}_{SafeFileName(application.JobPosting.CompanyName)}";
            return format == ExportFormat.Pdf
                ? new ExportedFile(_renderer.RenderPdf(letter), name + ".pdf", PdfMime)
                : new ExportedFile(_renderer.RenderDocx(letter), name + ".docx", DocxMime);
        }

        private async Task<(Application, Profile, AppSettings)> LoadAsync(int applicationId, CancellationToken ct)
        {
            var application = await _uow.Applications.GetWithDetailsAsync(applicationId, ct) ?? throw new NotFoundException("Application", applicationId);
            new Validator()
                .Check(Current(application, GeneratedTextKind.CoverLetter) != null, "CoverLetter", "Generate or write the cover letter first.")
                .ThrowIfInvalid();
            var profile = await _uow.Profiles.GetAsync(ct) ?? throw new NotFoundException("Profile");
            return (application, profile, await _uow.AppSettings.GetAsync(ct));
        }

        private void AddEvent(Application application, ApplicationEventType type, string? note) =>
            application.Events.Add(new ApplicationEvent { Type = type, OccurredAt = _time.GetUtcNow().UtcDateTime, Note = note });

        private static string? Current(Application application, GeneratedTextKind kind) =>
            application.GeneratedTexts.FirstOrDefault(t => t.Kind == kind && t.IsCurrent)?.Content;

        private static IReadOnlyList<string> NonEmpty(params string?[] lines) =>
            lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l!).ToList();

        private static string SafeFileName(string value) => Unsafe().Replace(value.Trim(), "_");

        [GeneratedRegex(@"[^\p{L}\p{N}\-]+")]
        private static partial Regex Unsafe();
    }
}
