using Core.Enums;

namespace Services.Ports
{
    public record JobQuery(string Keywords, string? Location, int? RadiusKm, int Page = 1);

    // A job as delivered by an external source, before it is stored.
    public record FoundJob(
        string ExternalId,
        string Title,
        string CompanyName,
        string? Location,
        string Description,
        string? Url,
        int? SalaryMin = null,
        int? SalaryMax = null,
        string? SalaryCurrency = null,
        EmploymentType EmploymentType = EmploymentType.Unknown,
        WorkMode WorkMode = WorkMode.Unknown,
        DateTime? PostedAt = null,
        string? ContactEmail = null);

    // One external job source (Adzuna, Bundesagentur für Arbeit, ...).
    public interface IJobSearchClient
    {
        JobSource Source { get; }

        // Throws JobSourceException when the source can't be queried (missing key, outage, ...).
        Task<IReadOnlyList<FoundJob>> SearchAsync(JobQuery query, CancellationToken ct = default);
    }

    public class JobSourceException : Exception
    {
        public JobSourceException(string message, Exception? innerException = null) : base(message, innerException) { }
    }
}
