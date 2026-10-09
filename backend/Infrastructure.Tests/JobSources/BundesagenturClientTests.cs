using System.Net;
using Core.Enums;
using Infrastructure.JobSources;
using Infrastructure.Tests.Support;
using Services.Ports;

namespace Infrastructure.Tests.JobSources
{
    public class BundesagenturClientTests
    {
        // Shapes as returned by the live API on 2026-10-09 (trimmed).
        private const string SearchResponse = """
            {
              "ergebnisliste": [
                {
                  "stellenangebotsart": "ARBEIT",
                  "stellenangebotsTitel": "(Senior) Backend Developer (C#/.NET)",
                  "arbeitszeitVollzeit": true,
                  "stellenlokationen": [ { "adresse": { "plz": "10247", "ort": "Berlin", "region": "BERLIN", "land": "DEUTSCHLAND" } } ],
                  "homeofficemoeglich": true,
                  "veroeffentlichungszeitraum": { "von": "2026-09-29" },
                  "hauptberuf": "Backend-Entwickler/in",
                  "firma": "Best Profile GmbH",
                  "referenznummer": "10001-1000078287-S"
                },
                {
                  "stellenangebotsart": "AUSBILDUNG",
                  "stellenangebotsTitel": "Ausbildung Fachinformatiker/in",
                  "stellenlokationen": [ { "adresse": { "ort": "Potsdam" } } ],
                  "veroeffentlichungszeitraum": { "von": "2026-10-01" },
                  "firma": "Contoso AG",
                  "referenznummer": "10000-2000000001-S",
                  "externeUrl": "https://contoso.example/karriere/42"
                }
              ],
              "maxErgebnisse": 2,
              "page": 1,
              "size": 25
            }
            """;

        private const string DetailsResponse = """
            {
              "stellenangebotsTitel": "(Senior) Backend Developer (C#/.NET)",
              "stellenangebotsBeschreibung": "Wir entwickeln kritische Kommunikationssysteme mit C# und .NET.",
              "referenznummer": "10001-1000078287-S"
            }
            """;

        private readonly FakeHttpHandler _http = new();

        private BundesagenturClient Client() => new(new HttpClient(_http));

        [Fact]
        public async Task Searches_with_public_key_and_loads_descriptions_from_details()
        {
            _http.When(r => r.RequestUri!.AbsolutePath.EndsWith("/pc/v6/jobs"), SearchResponse)
                 .When(r => r.RequestUri!.AbsolutePath.EndsWith("/pc/v4/jobdetails/MTAwMDEtMTAwMDA3ODI4Ny1T"), DetailsResponse);

            var jobs = await Client().SearchAsync(new JobQuery("C# Entwickler", "Berlin", 25));

            var search = _http.Requests.First();
            var query = System.Web.HttpUtility.ParseQueryString(search.RequestUri!.Query);
            Assert.Equal("jobboerse-jobsuche", search.Headers.GetValues("X-API-Key").Single());
            Assert.Equal("C# Entwickler", query["was"]);
            Assert.Equal("Berlin", query["wo"]);
            Assert.Equal("25", query["umkreis"]);

            var backend = jobs.Single(j => j.ExternalId == "10001-1000078287-S");
            Assert.Equal("(Senior) Backend Developer (C#/.NET)", backend.Title);
            Assert.Equal("Best Profile GmbH", backend.CompanyName);
            Assert.Equal("10247 Berlin", backend.Location);
            Assert.Equal("Wir entwickeln kritische Kommunikationssysteme mit C# und .NET.", backend.Description);
            Assert.Equal("https://www.arbeitsagentur.de/jobsuche/jobdetail/10001-1000078287-S", backend.Url);
            Assert.Equal(EmploymentType.FullTime, backend.EmploymentType);
            Assert.Equal(WorkMode.Hybrid, backend.WorkMode);
            Assert.Equal(new DateTime(2026, 9, 29), backend.PostedAt);

            // Details missing (404 here): job is still returned, with a fallback description.
            var apprenticeship = jobs.Single(j => j.ExternalId == "10000-2000000001-S");
            Assert.Equal(EmploymentType.Apprenticeship, apprenticeship.EmploymentType);
            Assert.Equal("https://contoso.example/karriere/42", apprenticeship.Url);
            Assert.Equal("Ausbildung Fachinformatiker/in bei Contoso AG", apprenticeship.Description);
        }

        [Fact]
        public async Task Failed_search_is_reported()
        {
            _http.When(_ => true, "", HttpStatusCode.ServiceUnavailable);

            var ex = await Assert.ThrowsAsync<JobSourceException>(() => Client().SearchAsync(new JobQuery("dev", null, null)));

            Assert.Contains("503", ex.Message);
        }
    }
}
