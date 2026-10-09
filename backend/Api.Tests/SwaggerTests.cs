using System.Net;

namespace Api.Tests
{
    public class SwaggerTests : IDisposable
    {
        private readonly ApiFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Swagger_ui_and_openapi_document_are_served_in_development()
        {
            var client = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
            Assert.Contains("/api/applications/{id}/generate", await client.GetStringAsync("/openapi/v1.json"));
        }
    }
}
