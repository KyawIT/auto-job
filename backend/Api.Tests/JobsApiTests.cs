using System.Net;
using System.Net.Http.Json;
using Core.Enums;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Services.Ports;

namespace Api.Tests
{
    public class JobsApiTests : IDisposable
    {
        private readonly ApiFactory _factory = new();
        private readonly HttpClient _client;

        public JobsApiTests()
        {
            _client = _factory
                .WithWebHostBuilder(b => b.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IJobSearchClient>();
                    services.AddSingleton<IJobSearchClient>(new StubSource());
                }))
                .CreateClient();
        }

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Manual_job_can_be_created_bookmarked_listed_and_deleted()
        {
            var created = await _client.PostAsJsonAsync("/api/jobs",
                new { title = "Backend Developer", companyName = "ACME GmbH", description = "C# APIs" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, object>>())!["id"].ToString();

            var bookmark = await _client.PutAsJsonAsync($"/api/jobs/{id}/state", new { state = "Bookmarked" });
            Assert.Equal(HttpStatusCode.OK, bookmark.StatusCode);
            Assert.Contains("\"state\":\"Bookmarked\"", await _client.GetStringAsync("/api/jobs?state=Bookmarked"));

            Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/jobs/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/jobs/{id}")).StatusCode);
        }

        [Fact]
        public async Task Search_stores_jobs_from_the_selected_source()
        {
            var response = await _client.PostAsJsonAsync("/api/jobs/search",
                new { keywords = ".NET", location = "Berlin", radiusKm = 25, sources = new[] { "BundesagenturFuerArbeit" } });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"newCount\":1", json);
            Assert.Contains("Fachinformatiker", await _client.GetStringAsync("/api/jobs"));
        }

        [Fact]
        public async Task Saved_search_can_be_created_and_listed()
        {
            var created = await _client.PostAsJsonAsync("/api/saved-searches",
                new { name = "Dev Berlin", keywords = ".NET", location = "Berlin", radiusKm = 25, sources = new[] { "BundesagenturFuerArbeit" }, refreshIntervalMinutes = 60, isActive = true });

            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Contains("Dev Berlin", await _client.GetStringAsync("/api/saved-searches"));
        }

        private class StubSource : IJobSearchClient
        {
            public JobSource Source => JobSource.BundesagenturFuerArbeit;

            public Task<IReadOnlyList<FoundJob>> SearchAsync(JobQuery query, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<FoundJob>>([new FoundJob("BA-1", "Fachinformatiker Anwendungsentwicklung", "Contoso AG", "Berlin", "C#", null)]);
        }
    }
}
