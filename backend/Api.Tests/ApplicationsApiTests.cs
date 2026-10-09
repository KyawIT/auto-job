using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Services.Ports;

namespace Api.Tests
{
    public class ApplicationsApiTests : IDisposable
    {
        private readonly ApiFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        private HttpClient ClientWithAi(IAiClient? ai)
        {
            var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            {
                if (ai == null) return;   // keep the real NIM client
                services.RemoveAll<IAiClient>();
                services.AddSingleton(ai);
            })).CreateClient();

            client.PutAsJsonAsync("/api/profile", new { firstName = "Anna", lastName = "Schmidt", email = "anna@example.com" }).Result.EnsureSuccessStatusCode();
            return client;
        }

        private static async Task<int> IdOf(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        private static async Task<int> StartApplicationAsync(HttpClient client)
        {
            var job = await client.PostAsJsonAsync("/api/jobs", new { title = "Backend Developer", companyName = "Contoso AG", description = "C#" });
            var started = await client.PostAsJsonAsync("/api/applications", new { jobPostingId = await IdOf(job), language = "de" });
            Assert.Equal(HttpStatusCode.Created, started.StatusCode);
            return await IdOf(started);
        }

        [Fact]
        public async Task Generated_letter_can_be_edited_and_its_versions_listed()
        {
            var client = ClientWithAi(new StubAi());
            var id = await StartApplicationAsync(client);

            var generated = await client.PostAsync($"/api/applications/{id}/generate", null);
            Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
            var letter = (await generated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("texts").EnumerateArray()
                .Single(t => t.GetProperty("kind").GetString() == "CoverLetter");

            var edit = await client.PutAsJsonAsync($"/api/texts/{letter.GetProperty("id").GetInt32()}", new { content = "Mein eigener Text" });
            Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

            var versions = await client.GetStringAsync($"/api/applications/{id}/texts?kind=CoverLetter");
            Assert.Contains("Mein eigener Text", versions);
            Assert.Contains("\"isEdited\":true", versions);
        }

        [Fact]
        public async Task Status_change_shows_up_in_the_list()
        {
            var client = ClientWithAi(new StubAi());
            var id = await StartApplicationAsync(client);

            var put = await client.PutAsJsonAsync($"/api/applications/{id}/status", new { status = "Sent" });

            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            Assert.Contains("\"status\":\"Sent\"", await client.GetStringAsync("/api/applications?status=Sent"));
        }

        [Fact]
        public async Task Generating_without_nim_key_explains_what_is_missing()
        {
            var client = ClientWithAi(ai: null);
            var id = await StartApplicationAsync(client);

            var response = await client.PostAsync($"/api/applications/{id}/generate", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("ai-not-configured", problem.GetProperty("type").GetString());
            Assert.Contains("NIM API key is not set", problem.GetProperty("detail").GetString());
        }

        private class StubAi : IAiClient
        {
            public Task<string> CompleteAsync(ChatRequest request, CancellationToken ct = default) =>
                Task.FromResult("""{"coverLetter":"Sehr geehrte Damen und Herren","emailSubject":"Bewerbung","emailBody":"Anbei"}""");
        }
    }
}
