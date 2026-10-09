using System.Net;
using Core.Enums;
using Infrastructure.JobSources;
using Infrastructure.Secrets;
using Infrastructure.Tests.Support;
using Services.Ports;

namespace Infrastructure.Tests.JobSources
{
    public class AdzunaClientTests
    {
        // Shape of a real Adzuna response (trimmed). Matched terms come wrapped in <strong>.
        private const string SearchResponse = """
            {
              "__CLASS__": "Adzuna::API::Response::JobSearchResults",
              "count": 1,
              "results": [
                {
                  "id": "4987654321",
                  "title": "<strong>.NET</strong> Backend Developer (m/w/d)",
                  "description": "Wir suchen einen <strong>.NET</strong> Entwickler für unsere APIs …",
                  "redirect_url": "https://www.adzuna.de/land/ad/4987654321",
                  "company": { "display_name": "ACME GmbH" },
                  "location": { "area": ["Deutschland", "Berlin"], "display_name": "Berlin, Deutschland" },
                  "salary_min": 60000.0,
                  "salary_max": 75000.5,
                  "salary_is_predicted": "0",
                  "contract_time": "full_time",
                  "contract_type": "permanent",
                  "created": "2026-10-01T09:30:00Z"
                }
              ]
            }
            """;

        private readonly FakeHttpHandler _http = new();
        private readonly InMemorySecretStore _secrets = new();

        private AdzunaClient Client() => new(new HttpClient(_http), _secrets);

        [Fact]
        public async Task Searches_germany_with_credentials_and_maps_results()
        {
            _secrets.Set(SecretName.AdzunaAppId, "my-id");
            _secrets.Set(SecretName.AdzunaAppKey, "my-key");
            _http.When(r => r.RequestUri!.AbsolutePath == "/v1/api/jobs/de/search/1", SearchResponse);

            var jobs = await Client().SearchAsync(new JobQuery(".NET Developer", "Berlin", 25));

            var query = System.Web.HttpUtility.ParseQueryString(_http.Requests.Single().RequestUri!.Query);
            Assert.Equal("my-id", query["app_id"]);
            Assert.Equal("my-key", query["app_key"]);
            Assert.Equal(".NET Developer", query["what"]);
            Assert.Equal("Berlin", query["where"]);
            Assert.Equal("25", query["distance"]);

            var job = Assert.Single(jobs);
            Assert.Equal("4987654321", job.ExternalId);
            Assert.Equal(".NET Backend Developer (m/w/d)", job.Title);
            Assert.Equal("Wir suchen einen .NET Entwickler für unsere APIs …", job.Description);
            Assert.Equal("ACME GmbH", job.CompanyName);
            Assert.Equal("Berlin, Deutschland", job.Location);
            Assert.Equal("https://www.adzuna.de/land/ad/4987654321", job.Url);
            Assert.Equal(60000, job.SalaryMin);
            Assert.Equal(75000, job.SalaryMax);
            Assert.Equal("EUR", job.SalaryCurrency);
            Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
            Assert.Equal(new DateTime(2026, 10, 1, 9, 30, 0, DateTimeKind.Utc), job.PostedAt);
        }

        [Fact]
        public async Task Missing_credentials_fail_without_calling_adzuna()
        {
            var ex = await Assert.ThrowsAsync<JobSourceException>(() => Client().SearchAsync(new JobQuery("dev", null, null)));

            Assert.Contains("not set", ex.Message);
            Assert.Empty(_http.Requests);
        }

        [Fact]
        public async Task Rejected_credentials_are_reported()
        {
            _secrets.Set(SecretName.AdzunaAppId, "wrong");
            _secrets.Set(SecretName.AdzunaAppKey, "wrong");
            _http.When(_ => true, """{"display":"Authorisation failed","exception":"AUTH_FAIL"}""", HttpStatusCode.Unauthorized);

            var ex = await Assert.ThrowsAsync<JobSourceException>(() => Client().SearchAsync(new JobQuery("dev", null, null)));

            Assert.Equal("Adzuna rejected the app ID/key.", ex.Message);
        }
    }
}
