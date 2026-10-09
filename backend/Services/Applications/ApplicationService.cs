using Core.Contracts;
using Core.Entities;
using Core.Enums;
using Services.Common;

namespace Services.Applications
{
    // An application for one job: status, recipient, attachments and its timeline.
    public class ApplicationService
    {
        private readonly IUnitOfWork _uow;
        private readonly TimeProvider _time;

        public ApplicationService(IUnitOfWork uow, TimeProvider time)
        {
            _uow = uow;
            _time = time;
        }

        public async Task<ApplicationDetailsDto> StartAsync(int jobPostingId, StartApplicationRequest? request = null, CancellationToken ct = default)
        {
            var job = await _uow.JobPostings.GetWithApplicationAsync(jobPostingId, ct) ?? throw new NotFoundException("Job", jobPostingId);
            if (job.Application != null)
                return await GetAsync(job.Application.Id, ct);   // one application per job; starting is idempotent

            var settings = await _uow.AppSettings.GetAsync(ct);

            var application = new Application
            {
                JobPosting = job,
                Language = request?.Language ?? job.DetectedLanguage ?? settings.DefaultLetterLanguage,
                RecipientEmail = job.ContactEmail,
                RecipientName = job.ContactName,
            };

            var defaults = (await _uow.Documents.GetAllAsync(ct)).Where(d => d.AttachByDefault).OrderBy(d => d.Type).ToList();
            for (var i = 0; i < defaults.Count; i++)
                application.Attachments.Add(new ApplicationDocument { Document = defaults[i], SortOrder = i });

            AddEvent(application, ApplicationEventType.Created);
            await _uow.Applications.AddAsync(application, ct);
            await _uow.SaveChangesAsync(ct);
            return application.ToDetails();
        }

        public async Task<ApplicationDetailsDto> GetAsync(int id, CancellationToken ct = default) =>
            (await RequireAsync(id, ct)).ToDetails();

        public async Task<ApplicationDetailsDto> ChangeStatusAsync(int id, ChangeStatusRequest request, CancellationToken ct = default)
        {
            var application = await RequireAsync(id, ct);
            if (application.Status == request.Status)
                return application.ToDetails();

            var now = _time.GetUtcNow().UtcDateTime;
            if (request.Status == ApplicationStatus.Sent)
                application.SentAt ??= now;
            if (request.Status is ApplicationStatus.Interview or ApplicationStatus.Offer or ApplicationStatus.Rejected)
                application.ResponseAt ??= now;

            var transition = $"{application.Status} → {request.Status}";
            AddEvent(application, ApplicationEventType.StatusChanged,
                string.IsNullOrWhiteSpace(request.Note) ? transition : $"{transition}: {request.Note}");
            application.Status = request.Status;

            await _uow.SaveChangesAsync(ct);
            return application.ToDetails();
        }

        public async Task<ApplicationDetailsDto> UpdateAsync(int id, UpdateApplicationRequest request, CancellationToken ct = default)
        {
            new Validator()
                .Required(nameof(request.Language), request.Language)
                .Email(nameof(request.RecipientEmail), request.RecipientEmail)
                .ThrowIfInvalid();

            var application = await RequireAsync(id, ct);
            application.Language = request.Language.Trim().ToLowerInvariant();
            application.RecipientEmail = request.RecipientEmail;
            application.RecipientName = request.RecipientName;
            application.Notes = request.Notes;
            await _uow.SaveChangesAsync(ct);
            return application.ToDetails();
        }

        // Replaces all attachments; the list order is the order in the email.
        public async Task<ApplicationDetailsDto> SetAttachmentsAsync(int id, IReadOnlyList<int> documentIds, CancellationToken ct = default)
        {
            var application = await RequireAsync(id, ct);
            var documents = (await _uow.Documents.GetAllAsync(ct)).ToDictionary(d => d.Id);
            var unknown = documentIds.Where(d => !documents.ContainsKey(d)).ToList();
            new Validator().Check(unknown.Count == 0, nameof(documentIds), $"Unknown documents: {string.Join(", ", unknown)}.").ThrowIfInvalid();

            application.Attachments.Clear();
            var order = 0;
            foreach (var documentId in documentIds.Distinct())
                application.Attachments.Add(new ApplicationDocument { Document = documents[documentId], SortOrder = order++ });

            await _uow.SaveChangesAsync(ct);
            return application.ToDetails();
        }

        public async Task<IReadOnlyList<ApplicationSummaryDto>> ListAsync(ApplicationStatus? status, CancellationToken ct = default) =>
            (await _uow.Applications.ListAsync(status, ct)).Select(a => a.ToSummary()).ToList();

        public async Task<ApplicationDetailsDto> AddNoteAsync(int id, string note, CancellationToken ct = default)
        {
            new Validator().Required(nameof(note), note).ThrowIfInvalid();
            var application = await RequireAsync(id, ct);
            AddEvent(application, ApplicationEventType.Note, note);
            await _uow.SaveChangesAsync(ct);
            return application.ToDetails();
        }

        // Texts, events and attachment links go with it; the job and the documents stay.
        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            _uow.Applications.Remove(await RequireAsync(id, ct));
            await _uow.SaveChangesAsync(ct);
        }

        private void AddEvent(Application application, ApplicationEventType type, string? note = null) =>
            application.Events.Add(new ApplicationEvent { Type = type, OccurredAt = _time.GetUtcNow().UtcDateTime, Note = note });

        private async Task<Application> RequireAsync(int id, CancellationToken ct) =>
            await _uow.Applications.GetWithDetailsAsync(id, ct) ?? throw new NotFoundException("Application", id);
    }
}
