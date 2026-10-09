using Core.Contracts;
using Core.Entities;
using Core.Enums;
using Services.Applications;
using Services.Common;
using Services.Ports;

namespace Services.Generation
{
    // Generates the cover letter, email subject and email body for an application with the AI.
    // Every generation adds new versions; earlier versions (also edited ones) are kept.
    public class GenerationService
    {
        public const string DefaultModel = "meta/llama-3.3-70b-instruct";

        private readonly IUnitOfWork _uow;
        private readonly IAiClient _ai;
        private readonly TimeProvider _time;

        public GenerationService(IUnitOfWork uow, IAiClient ai, TimeProvider time)
        {
            _uow = uow;
            _ai = ai;
            _time = time;
        }

        public async Task<ApplicationDetailsDto> GenerateAsync(int applicationId, CancellationToken ct = default)
        {
            var application = await _uow.Applications.GetWithDetailsAsync(applicationId, ct) ?? throw new NotFoundException("Application", applicationId);
            var profile = await _uow.Profiles.GetAsync(ct) ?? throw new NotFoundException("Profile");
            var settings = await _uow.AppSettings.GetAsync(ct);
            var model = string.IsNullOrWhiteSpace(settings.NimModel) ? DefaultModel : settings.NimModel;

            var messages = CoverLetterPrompt.Build(profile, application, profile.Documents);
            var letter = CoverLetterPrompt.Parse(await _ai.CompleteAsync(new ChatRequest(model, messages), ct));

            AddCurrentVersion(application, GeneratedTextKind.CoverLetter, letter.CoverLetter, model);
            AddCurrentVersion(application, GeneratedTextKind.EmailSubject, letter.EmailSubject, model);
            AddCurrentVersion(application, GeneratedTextKind.EmailBody, letter.EmailBody, model);
            application.Events.Add(new ApplicationEvent
            {
                Type = ApplicationEventType.Generated,
                OccurredAt = _time.GetUtcNow().UtcDateTime,
                Note = model,
            });

            await _uow.SaveChangesAsync(ct);
            return application.ToDetails();
        }

        // The user's own changes; marked as edited so regenerating never silently replaces them.
        public async Task<GeneratedTextDto> EditTextAsync(int textId, string content, CancellationToken ct = default)
        {
            new Validator().Required(nameof(content), content).ThrowIfInvalid();
            var text = await RequireTextAsync(textId, ct);
            text.Content = content;
            text.IsEdited = true;
            await _uow.SaveChangesAsync(ct);
            return text.ToDto();
        }

        // Newest first.
        public async Task<IReadOnlyList<GeneratedTextDto>> ListVersionsAsync(int applicationId, GeneratedTextKind kind, CancellationToken ct = default)
        {
            var application = await _uow.Applications.GetWithDetailsAsync(applicationId, ct) ?? throw new NotFoundException("Application", applicationId);
            return application.GeneratedTexts.Where(t => t.Kind == kind).OrderByDescending(t => t.Id).Select(t => t.ToDto()).ToList();
        }

        public async Task<ApplicationDetailsDto> SelectVersionAsync(int textId, CancellationToken ct = default)
        {
            var text = await RequireTextAsync(textId, ct);
            var application = await _uow.Applications.GetWithDetailsAsync(text.ApplicationId, ct) ?? throw new NotFoundException("Application", text.ApplicationId);
            foreach (var other in application.GeneratedTexts.Where(t => t.Kind == text.Kind))
                other.IsCurrent = other.Id == text.Id;
            await _uow.SaveChangesAsync(ct);
            return application.ToDetails();
        }

        private async Task<GeneratedText> RequireTextAsync(int id, CancellationToken ct) =>
            await _uow.GeneratedTexts.GetByIdAsync(id, ct) ?? throw new NotFoundException("Text", id);

        private static void AddCurrentVersion(Application application, GeneratedTextKind kind, string content, string model)
        {
            foreach (var previous in application.GeneratedTexts.Where(t => t.Kind == kind))
                previous.IsCurrent = false;

            application.GeneratedTexts.Add(new GeneratedText
            {
                Kind = kind,
                Language = application.Language,
                Content = content,
                Model = model,
                IsCurrent = true,
            });
        }
    }
}
