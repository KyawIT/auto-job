using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MimeKit;
using Services.Ports;

namespace Api.Tests
{
    // Real renderer and .eml builder; only the AI is replaced.
    public class ExportApiTests : IDisposable
    {
        private readonly ApiFactory _factory = new();
        private readonly HttpClient _client;

        public ExportApiTests()
        {
            _client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiClient>();
                services.AddSingleton<IAiClient>(new StubAi());
            })).CreateClient();
            _client.PutAsJsonAsync("/api/profile", new { firstName = "Anna", lastName = "Schmidt", email = "anna@example.com", city = "Berlin" })
                .Result.EnsureSuccessStatusCode();
        }

        public void Dispose() => _factory.Dispose();

        private async Task<int> GeneratedApplicationAsync()
        {
            var job = await _client.PostAsJsonAsync("/api/jobs", new { title = "Backend Developer", companyName = "Contoso AG", description = "C#", contactEmail = "jobs@contoso.example" });
            var jobId = (await job.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
            var started = await _client.PostAsJsonAsync("/api/applications", new { jobPostingId = jobId, language = "de" });
            var id = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
            (await _client.PostAsync($"/api/applications/{id}/generate", null)).EnsureSuccessStatusCode();
            return id;
        }

        [Fact]
        public async Task Cover_letter_downloads_as_pdf_and_docx()
        {
            var id = await GeneratedApplicationAsync();

            var pdf = await _client.GetAsync($"/api/applications/{id}/export?format=Pdf");
            var docx = await _client.GetAsync($"/api/applications/{id}/export?format=Docx");

            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
            Assert.Equal("Anschreiben_Contoso_AG.pdf", pdf.Content.Headers.ContentDisposition!.FileName);
            Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
            Assert.Equal("Anschreiben_Contoso_AG.docx", docx.Content.Headers.ContentDisposition!.FileName);
        }

        [Fact]
        public async Task Email_draft_file_is_written_to_the_data_folder()
        {
            var id = await GeneratedApplicationAsync();

            var response = await _client.PostAsync($"/api/applications/{id}/email-draft", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fullPath = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("fullPath").GetString()!;
            Assert.StartsWith(Path.GetFullPath(_factory.DataDirectory), fullPath);
            var message = await MimeMessage.LoadAsync(fullPath);
            Assert.Equal("jobs@contoso.example", message.To.Mailboxes.Single().Address);
            Assert.Equal("Anschreiben_Contoso_AG.pdf", message.Attachments.OfType<MimePart>().Single().FileName);
        }

        private class StubAi : IAiClient
        {
            public Task<string> CompleteAsync(ChatRequest request, CancellationToken ct = default) =>
                Task.FromResult("""{"coverLetter":"Sehr geehrte Damen und Herren,\n\nhiermit bewerbe ich mich.\n\nMit freundlichen Grüßen\nAnna Schmidt","emailSubject":"Bewerbung als Backend Developer","emailBody":"Anbei meine Unterlagen."}""");
        }
    }
}
