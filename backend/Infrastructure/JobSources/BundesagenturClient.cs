using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Core.Enums;
using Services.Ports;

namespace Infrastructure.JobSources
{
    // Jobsuche of the Bundesagentur für Arbeit (public key). Formats checked against the live API on 2026-10-09:
    // search /pc/v6/jobs returns no description, so it is loaded per job from /pc/v4/jobdetails/{base64(refnr)}.
    public class BundesagenturClient : IJobSearchClient
    {
        private const string BaseUrl = "https://rest.arbeitsagentur.de/jobboerse/jobsuche-service";
        private const string PublicApiKey = "jobboerse-jobsuche";
        private const int PageSize = 25;
        private const int ParallelDetailRequests = 4;

        private readonly HttpClient _http;

        public BundesagenturClient(HttpClient http)
        {
            _http = http;
        }

        public JobSource Source => JobSource.BundesagenturFuerArbeit;

        public async Task<IReadOnlyList<FoundJob>> SearchAsync(JobQuery query, CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/pc/v6/jobs" + QueryString.From(new()
            {
                ["was"] = query.Keywords,
                ["wo"] = query.Location,
                ["umkreis"] = query.RadiusKm?.ToString(CultureInfo.InvariantCulture),
                ["page"] = query.Page.ToString(CultureInfo.InvariantCulture),
                ["size"] = PageSize.ToString(CultureInfo.InvariantCulture),
            });

            using var response = await SendAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                throw new JobSourceException($"Bundesagentur search failed ({(int)response.StatusCode}).");

            var results = (await response.Content.ReadFromJsonAsync<SearchResponse>(ct))?.Ergebnisliste ?? [];

            var jobs = new FoundJob[results.Count];
            await Parallel.ForAsync(0, results.Count,
                new ParallelOptions { MaxDegreeOfParallelism = ParallelDetailRequests, CancellationToken = ct },
                async (i, token) => jobs[i] = ToFoundJob(results[i], await LoadDescriptionAsync(results[i].Referenznummer, token)));
            return jobs;
        }

        // Best effort: without details the job is still useful, just with a short description.
        private async Task<string?> LoadDescriptionAsync(string referenceNumber, CancellationToken ct)
        {
            var code = Convert.ToBase64String(Encoding.UTF8.GetBytes(referenceNumber));
            try
            {
                using var response = await SendAsync($"{BaseUrl}/pc/v4/jobdetails/{code}", ct);
                if (!response.IsSuccessStatusCode)
                    return null;
                return (await response.Content.ReadFromJsonAsync<DetailsResponse>(ct))?.StellenangebotsBeschreibung;
            }
            catch (JobSourceException)
            {
                return null;
            }
        }

        private async Task<HttpResponseMessage> SendAsync(string url, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-API-Key", PublicApiKey);
            try
            {
                return await _http.SendAsync(request, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new JobSourceException("Bundesagentur für Arbeit is not reachable.", ex);
            }
        }

        private static FoundJob ToFoundJob(Result r, string? description)
        {
            var address = r.Stellenlokationen?.FirstOrDefault()?.Adresse;
            return new FoundJob(
                ExternalId: r.Referenznummer,
                Title: r.StellenangebotsTitel,
                CompanyName: r.Firma ?? "Unknown",
                Location: address == null ? null : string.Join(" ", new[] { address.Plz, address.Ort }.Where(p => !string.IsNullOrWhiteSpace(p))),
                Description: string.IsNullOrWhiteSpace(description) ? $"{r.StellenangebotsTitel} bei {r.Firma}" : description,
                Url: r.ExterneUrl ?? $"https://www.arbeitsagentur.de/jobsuche/jobdetail/{r.Referenznummer}",
                EmploymentType: r.Stellenangebotsart switch
                {
                    "AUSBILDUNG" => EmploymentType.Apprenticeship,
                    "PRAKTIKUM_TRAINEE" => EmploymentType.Internship,
                    "SELBSTAENDIGKEIT" => EmploymentType.Contract,
                    _ => r.ArbeitszeitVollzeit == true ? EmploymentType.FullTime : EmploymentType.Unknown,
                },
                WorkMode: r.Homeofficemoeglich == true ? WorkMode.Hybrid : WorkMode.Unknown,
                PostedAt: r.Veroeffentlichungszeitraum?.Von?.ToDateTime(TimeOnly.MinValue));
        }

        private record SearchResponse(List<Result>? Ergebnisliste);

        private record Result(
            string Referenznummer,
            string StellenangebotsTitel,
            string? Stellenangebotsart,
            string? Firma,
            string? ExterneUrl,
            bool? ArbeitszeitVollzeit,
            bool? Homeofficemoeglich,
            List<Lokation>? Stellenlokationen,
            Zeitraum? Veroeffentlichungszeitraum);

        private record Lokation(Adresse? Adresse);

        private record Adresse(string? Plz, string? Ort);

        private record Zeitraum(DateOnly? Von);

        private record DetailsResponse(string? StellenangebotsBeschreibung);
    }
}
