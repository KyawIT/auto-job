using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Services.Ports;

namespace Api.Tests
{
    public class BatchesApiTests : IDisposable
    {
        private readonly ApiFactory _factory = new();
        private readonly HttpClient _client;

        public BatchesApiTests()
        {
            _client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiClient>();
                services.AddSingleton<IAiClient>(new StubAi());
            })).CreateClient();
            _client.PutAsJsonAsync("/api/profile", new { firstName = "Anna", lastName = "Schmidt", email = "anna@example.com" }).Result.EnsureSuccessStatusCode();
        }

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Background_worker_completes_a_started_batch()
        {
            var jobIds = new List<int>();
            foreach (var title in new[] { "Backend Developer", "API Developer" })
            {
                var job = await _client.PostAsJsonAsync("/api/jobs", new { title, companyName = "Contoso AG", description = "C#" });
                jobIds.Add((await job.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
            }

            var started = await _client.PostAsJsonAsync("/api/batches", new { jobPostingIds = jobIds });
            Assert.Equal(HttpStatusCode.Created, started.StatusCode);
            var id = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var batch = await PollUntilAsync(id, b => b.GetProperty("status").GetString() == "Completed");
            Assert.Equal(2, batch.GetProperty("progress").GetProperty("done").GetInt32());
        }

        private async Task<JsonElement> PollUntilAsync(int batchId, Func<JsonElement, bool> done)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (true)
            {
                var batch = await _client.GetFromJsonAsync<JsonElement>($"/api/batches/{batchId}");
                if (done(batch) || DateTime.UtcNow > deadline)
                    return batch;
                await Task.Delay(100);
            }
        }

        private class StubAi : IAiClient
        {
            public Task<string> CompleteAsync(ChatRequest request, CancellationToken ct = default) =>
                Task.FromResult("""{"coverLetter":"Sehr geehrte Damen und Herren","emailSubject":"Bewerbung","emailBody":"Anbei"}""");
        }
    }
}
