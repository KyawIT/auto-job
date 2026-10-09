using Core.Enums;

namespace Services.Jobs
{
    public record CreateJobRequest(
        string Title,
        string CompanyName,
        string Description,
        string? Location = null,
        string? Url = null,
        string? ContactEmail = null,
        string? ContactName = null,
        EmploymentType EmploymentType = EmploymentType.Unknown,
        WorkMode WorkMode = WorkMode.Unknown);

    public record JobListFilter(JobPostingState? State = null, string? Search = null, bool IncludeDismissed = false);

    // List row: no description, plus the application status if one exists.
    public record JobSummaryDto(
        int Id,
        string Title,
        string CompanyName,
        string? Location,
        JobSource Source,
        JobPostingState State,
        WorkMode WorkMode,
        DateTime? PostedAt,
        DateTime CreatedAt,
        int? ApplicationId,
        ApplicationStatus? ApplicationStatus);

    public record JobDto(
        int Id,
        string Title,
        string CompanyName,
        string? Location,
        string Description,
        string? Url,
        int? SalaryMin,
        int? SalaryMax,
        string? SalaryCurrency,
        EmploymentType EmploymentType,
        WorkMode WorkMode,
        string? ContactEmail,
        string? ContactName,
        JobSource Source,
        JobPostingState State,
        DateTime? PostedAt,
        int? ApplicationId);

    public record SearchJobsRequest(string Keywords, string? Location, int? RadiusKm, IReadOnlyList<JobSource> Sources);

    public record SourceErrorDto(JobSource Source, string Message);

    // Jobs = everything the sources returned (new and already known); NewCount = how many were added.
    public record SearchResultDto(IReadOnlyList<JobSummaryDto> Jobs, int NewCount, IReadOnlyList<SourceErrorDto> Errors);

    public record SaveSearchRequest(
        string Name,
        string Keywords,
        string? Location,
        int? RadiusKm,
        IReadOnlyList<JobSource> Sources,
        int RefreshIntervalMinutes = 60,
        bool IsActive = true);

    public record SavedSearchDto(
        int Id,
        string Name,
        string Keywords,
        string? Location,
        int? RadiusKm,
        IReadOnlyList<JobSource> Sources,
        int RefreshIntervalMinutes,
        bool IsActive,
        DateTime? LastRunAt);
}
