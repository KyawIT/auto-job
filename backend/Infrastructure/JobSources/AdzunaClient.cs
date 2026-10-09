using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Core.Enums;
using Services.Ports;

namespace Infrastructure.JobSources
{
    // https://developer.adzuna.com/docs/search — each user brings their own app_id/app_key.
    public class AdzunaClient : IJobSearchClient
    {
        private const string Country = "de";
        private const int ResultsPerPage = 50;

        private readonly HttpClient _http;
        private readonly ISecretStore _secrets;

        public AdzunaClient(HttpClient http, ISecretStore secrets)
        {
            _http = http;
            _secrets = secrets;
        }

        public JobSource Source => JobSource.Adzuna;

        public async Task<IReadOnlyList<FoundJob>> SearchAsync(JobQuery query, CancellationToken ct = default)
        {
            var appId = _secrets.Get(SecretName.AdzunaAppId);
            var appKey = _secrets.Get(SecretName.AdzunaAppKey);
            if (appId == null || appKey == null)
                throw new JobSourceException("Adzuna app ID and key are not set (Settings → API keys).");

            var url = $"https://api.adzuna.com/v1/api/jobs/{Country}/search/{query.Page}" + QueryString.From(new()
            {
                ["app_id"] = appId,
                ["app_key"] = appKey,
                ["what"] = query.Keywords,
                ["where"] = query.Location,
                ["distance"] = query.RadiusKm?.ToString(CultureInfo.InvariantCulture),
                ["results_per_page"] = ResultsPerPage.ToString(CultureInfo.InvariantCulture),
                ["content-type"] = "application/json",
            });

            using var response = await SendAsync(url, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new JobSourceException("Adzuna rejected the app ID/key.");
            if (!response.IsSuccessStatusCode)
                throw new JobSourceException($"Adzuna search failed ({(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<SearchResponse>(ct);
            return body?.Results.Select(ToFoundJob).ToList() ?? [];
        }

        private async Task<HttpResponseMessage> SendAsync(string url, CancellationToken ct)
        {
            try
            {
                return await _http.GetAsync(url, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new JobSourceException("Adzuna is not reachable.", ex);
            }
        }

        private static FoundJob ToFoundJob(Result r) => new(
            ExternalId: r.Id,
            Title: Html.ToText(r.Title),
            CompanyName: r.Company?.DisplayName ?? "Unknown",
            Location: r.Location?.DisplayName,
            Description: Html.ToText(r.Description),
            Url: r.RedirectUrl,
            SalaryMin: (int?)r.SalaryMin,
            SalaryMax: (int?)r.SalaryMax,
            SalaryCurrency: r.SalaryMin != null || r.SalaryMax != null ? "EUR" : null,
            EmploymentType: r.ContractTime switch
            {
                "full_time" => EmploymentType.FullTime,
                "part_time" => EmploymentType.PartTime,
                _ => r.ContractType == "contract" ? EmploymentType.Contract : EmploymentType.Unknown,
            },
            PostedAt: r.Created?.UtcDateTime);

        private record SearchResponse([property: JsonPropertyName("results")] List<Result> Results);

        private record Result(
            [property: JsonPropertyName("id")] string Id,
            [property: JsonPropertyName("title")] string Title,
            [property: JsonPropertyName("description")] string Description,
            [property: JsonPropertyName("redirect_url")] string? RedirectUrl,
            [property: JsonPropertyName("company")] Named? Company,
            [property: JsonPropertyName("location")] Named? Location,
            [property: JsonPropertyName("salary_min")] double? SalaryMin,
            [property: JsonPropertyName("salary_max")] double? SalaryMax,
            [property: JsonPropertyName("contract_time")] string? ContractTime,
            [property: JsonPropertyName("contract_type")] string? ContractType,
            [property: JsonPropertyName("created")] DateTimeOffset? Created);

        private record Named([property: JsonPropertyName("display_name")] string? DisplayName);
    }
}
