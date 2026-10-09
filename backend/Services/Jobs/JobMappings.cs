using Core.Entities;
using Core.Enums;
using Services.Ports;

namespace Services.Jobs
{
    internal static class JobMappings
    {
        public static JobSummaryDto ToSummary(this JobPosting j) => new(
            j.Id, j.Title, j.CompanyName, j.Location, j.Source, j.State, j.WorkMode, j.PostedAt, j.CreatedAt,
            j.Application?.Id, j.Application?.Status);

        public static JobDto ToDto(this JobPosting j) => new(
            j.Id, j.Title, j.CompanyName, j.Location, j.Description, j.Url, j.SalaryMin, j.SalaryMax, j.SalaryCurrency,
            j.EmploymentType, j.WorkMode, j.ContactEmail, j.ContactName, j.Source, j.State, j.PostedAt, j.Application?.Id);

        public static SavedSearchDto ToDto(this SavedSearch s) => new(
            s.Id, s.Name, s.Keywords, s.Location, s.RadiusKm, s.Sources, s.RefreshIntervalMinutes, s.IsActive, s.LastRunAt);

        public static JobPosting ToEntity(this FoundJob f, JobSource source) => new()
        {
            Source = source,
            ExternalId = f.ExternalId,
            Title = f.Title,
            CompanyName = f.CompanyName,
            Location = f.Location,
            Description = f.Description,
            Url = f.Url,
            SalaryMin = f.SalaryMin,
            SalaryMax = f.SalaryMax,
            SalaryCurrency = f.SalaryCurrency,
            EmploymentType = f.EmploymentType,
            WorkMode = f.WorkMode,
            PostedAt = f.PostedAt,
            ContactEmail = f.ContactEmail,
        };
    }
}
