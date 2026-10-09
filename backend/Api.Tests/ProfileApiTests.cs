using System.Net;
using System.Net.Http.Json;

namespace Api.Tests
{
    public class ProfileApiTests : IDisposable
    {
        private readonly ApiFactory _factory = new();
        private readonly HttpClient _client;

        public ProfileApiTests()
        {
            _client = _factory.CreateClient();
        }

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Profile_is_not_found_before_setup_and_returned_after_saving()
        {
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/profile")).StatusCode);

            var put = await _client.PutAsJsonAsync("/api/profile", new { firstName = "Anna", lastName = "Schmidt", email = "anna@example.com" });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            var profile = await _client.GetFromJsonAsync<Dictionary<string, object?>>("/api/profile");
            Assert.Equal("Anna", profile!["firstName"]?.ToString());
        }

        [Fact]
        public async Task Invalid_profile_returns_400_with_field_errors()
        {
            var response = await _client.PutAsJsonAsync("/api/profile", new { firstName = "Anna", lastName = "Schmidt", email = "not-an-email" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
            Assert.Contains("Email", problem!.Errors.Keys);
        }

        [Fact]
        public async Task Adding_experience_before_profile_exists_returns_404()
        {
            var response = await _client.PostAsJsonAsync("/api/profile/work-experiences",
                new { company = "ACME", role = "Developer", startDate = "2021-03-01" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private record ValidationProblem(Dictionary<string, string[]> Errors);
    }
}
