using Core.Enums;

namespace Core.Models
{
    // Search text matches title, company and location. Dismissed jobs are hidden unless asked for.
    public record JobPostingQuery(JobPostingState? State = null, string? Search = null, bool IncludeDismissed = false);
}
