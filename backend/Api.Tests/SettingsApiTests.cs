using System.Net;
using System.Net.Http.Json;

namespace Api.Tests
{
    public class SettingsApiTests : IDisposable
    {
        private readonly ApiFactory _factory = new();
        private readonly HttpClient _client;

        public SettingsApiTests()
        {
            _client = _factory.CreateClient();
        }

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Api_key_is_accepted_but_never_returned()
        {
            var put = await _client.PutAsJsonAsync("/api/settings/secrets", new { nimApiKey = "nvapi-secret-123" });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            var json = await _client.GetStringAsync("/api/settings");

            Assert.Contains("\"nimApiKeyConfigured\":true", json);
            Assert.DoesNotContain("nvapi-secret-123", json);
            Assert.DoesNotContain("nvapi-secret-123", await put.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Settings_update_is_returned_with_layout_as_text()
        {
            var put = await _client.PutAsJsonAsync("/api/settings",
                new { defaultLetterLanguage = "en", letterLayout = "International", maxParallelGenerations = 2 });

            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            Assert.Contains("\"letterLayout\":\"International\"", await _client.GetStringAsync("/api/settings"));
        }
    }
}
