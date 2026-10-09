using Core.Entities;
using Services.Jobs;

namespace Services.Applications
{
    internal static class ApplicationMappings
    {
        // Only the current text per kind; older versions come from the versions endpoint.
        public static ApplicationDetailsDto ToDetails(this Application a) => new(
            a.Id, a.Status, a.Language, a.RecipientEmail, a.RecipientName, a.Notes, a.SentAt, a.ResponseAt, a.CreatedAt,
            a.JobPosting.ToSummary(),
            a.Attachments.OrderBy(x => x.SortOrder)
                .Select(x => new AttachmentDto(x.DocumentId, x.Document.DisplayName, x.Document.Type, x.Document.OriginalFileName)).ToList(),
            a.GeneratedTexts.Where(t => t.IsCurrent).OrderBy(t => t.Kind).Select(t => t.ToDto()).ToList(),
            a.Events.OrderBy(e => e.OccurredAt).ThenBy(e => e.Id).Select(e => new ApplicationEventDto(e.Type, e.OccurredAt, e.Note)).ToList());

        public static ApplicationSummaryDto ToSummary(this Application a) => new(
            a.Id, a.JobPostingId, a.JobPosting.Title, a.JobPosting.CompanyName, a.Status, a.Language, a.SentAt, a.UpdatedAt);

        public static GeneratedTextDto ToDto(this GeneratedText t) =>
            new(t.Id, t.Kind, t.Language, t.Content, t.Model, t.IsEdited, t.IsCurrent, t.CreatedAt);
    }
}
