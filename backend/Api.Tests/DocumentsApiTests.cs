using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Api.Tests
{
    public class DocumentsApiTests : IDisposable
    {
        private readonly ApiFactory _factory = new();
        private readonly HttpClient _client;

        public DocumentsApiTests()
        {
            _client = _factory.CreateClient();
            _client.PutAsJsonAsync("/api/profile", new { firstName = "Anna", lastName = "Schmidt", email = "anna@example.com" })
                .GetAwaiter().GetResult().EnsureSuccessStatusCode();
        }

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Uploaded_document_is_listed_with_extracted_text_and_can_be_downloaded()
        {
            var form = new MultipartFormDataContent
            {
                { new ByteArrayContent(Encoding.UTF8.GetBytes("Zertifikat AZ-900")), "file", "zertifikat.txt" },
                { new StringContent("Certification"), "type" },
                { new StringContent("true"), "attachByDefault" },
            };

            var upload = await _client.PostAsync("/api/documents", form);
            Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

            var list = await _client.GetStringAsync("/api/documents");
            Assert.Contains("\"displayName\":\"zertifikat\"", list);
            Assert.Contains("\"extractionStatus\":\"Done\"", list);

            var id = (await upload.Content.ReadFromJsonAsync<Dictionary<string, object>>())!["id"].ToString();
            var download = await _client.GetAsync($"/api/documents/{id}/file");
            Assert.Equal("Zertifikat AZ-900", await download.Content.ReadAsStringAsync());
            Assert.Equal("zertifikat.txt", download.Content.Headers.ContentDisposition?.FileName);
        }

        [Fact]
        public async Task Deleted_document_is_gone()
        {
            var form = new MultipartFormDataContent { { new ByteArrayContent("x"u8.ToArray()), "file", "cv.txt" }, { new StringContent("Cv"), "type" } };
            var id = (await (await _client.PostAsync("/api/documents", form)).Content.ReadFromJsonAsync<Dictionary<string, object>>())!["id"].ToString();

            Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/documents/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/documents/{id}/file")).StatusCode);
        }
    }
}
