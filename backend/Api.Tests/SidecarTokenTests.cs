using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Api.Tests
{
    public class SidecarTokenTests : IDisposable
    {
        private readonly ApiFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task With_a_token_configured_only_requests_carrying_it_get_through()
        {
            var client = _factory.WithWebHostBuilder(b => b.UseSetting("Sidecar:Token", "launch-secret")).CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/settings")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

            client.DefaultRequestHeaders.Add("X-AutoJob-Token", "launch-secret");
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/settings")).StatusCode);
        }
    }
}
