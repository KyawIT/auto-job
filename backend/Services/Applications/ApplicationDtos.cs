using Core.Enums;
using Services.Jobs;

namespace Services.Applications
{
    // Language null = posting's detected language, else the default letter language from settings.
    public record StartApplicationRequest(string? Language = null);

    public record UpdateApplicationRequest(string Language, string? RecipientEmail, string? RecipientName, string? Notes);

    public record ChangeStatusRequest(ApplicationStatus Status, string? Note = null);

    public record ApplicationSummaryDto(
        int Id,
        int JobPostingId,
        string JobTitle,
        string CompanyName,
        ApplicationStatus Status,
        string Language,
        DateTime? SentAt,
        DateTime UpdatedAt);

    public record ApplicationDetailsDto(
        int Id,
        ApplicationStatus Status,
        string Language,
        string? RecipientEmail,
        string? RecipientName,
        string? Notes,
        DateTime? SentAt,
        DateTime? ResponseAt,
        DateTime CreatedAt,
        JobSummaryDto Job,
        IReadOnlyList<AttachmentDto> Attachments,
        IReadOnlyList<GeneratedTextDto> Texts,
        IReadOnlyList<ApplicationEventDto> Events);

    public record AttachmentDto(int DocumentId, string DisplayName, DocumentType Type, string FileName);

    public record GeneratedTextDto(
        int Id,
        GeneratedTextKind Kind,
        string Language,
        string Content,
        string? Model,
        bool IsEdited,
        bool IsCurrent,
        DateTime CreatedAt);

    public record ApplicationEventDto(ApplicationEventType Type, DateTime OccurredAt, string? Note);
}
