using System.Net;
using System.Text.Json;
using Infrastructure.Ai;
using Infrastructure.Secrets;
using Infrastructure.Tests.Support;
using Services.Ports;

namespace Infrastructure.Tests.Ai
{
    public class NimClientTests
    {
        private const string CompletionResponse = """
            {
              "id": "chatcmpl-1",
              "object": "chat.completion",
              "model": "meta/llama-3.3-70b-instruct",
              "choices": [ { "index": 0, "message": { "role": "assistant", "content": "{\"coverLetter\":\"Sehr geehrte ...\"}" }, "finish_reason": "stop" } ],
              "usage": { "prompt_tokens": 900, "completion_tokens": 400, "total_tokens": 1300 }
            }
            """;

        private readonly FakeHttpHandler _http = new();
        private readonly InMemorySecretStore _secrets = new();

        private NimClient Client() => new(new HttpClient(_http), _secrets);

        private static ChatRequest Request() =>
            new("meta/llama-3.3-70b-instruct", [ChatMessage.System("You write applications."), ChatMessage.User("Job: ...")], Temperature: 0.6, MaxTokens: 2500);

        [Fact]
        public async Task Sends_openai_compatible_request_with_bearer_key_and_returns_the_answer()
        {
            _secrets.Set(SecretName.NimApiKey, "nvapi-test");
            _http.When(r => r.RequestUri!.ToString() == "https://integrate.api.nvidia.com/v1/chat/completions", CompletionResponse);

            var answer = await Client().CompleteAsync(Request());

            Assert.Equal("{\"coverLetter\":\"Sehr geehrte ...\"}", answer);
            var request = _http.Requests.Single();
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("Bearer nvapi-test", request.Headers.Authorization!.ToString());

            using var body = JsonDocument.Parse(_http.RequestBodies.Single());
            Assert.Equal("meta/llama-3.3-70b-instruct", body.RootElement.GetProperty("model").GetString());
            Assert.Equal(2500, body.RootElement.GetProperty("max_tokens").GetInt32());
            Assert.Equal(0.6, body.RootElement.GetProperty("temperature").GetDouble());
            Assert.Equal(["system", "user"], body.RootElement.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("role").GetString()));
        }

        [Fact]
        public async Task Missing_key_fails_without_calling_nvidia()
        {
            await Assert.ThrowsAsync<AiNotConfiguredException>(() => Client().CompleteAsync(Request()));
            Assert.Empty(_http.Requests);
        }

        [Fact]
        public async Task Rejected_key_and_rate_limit_and_outage_are_told_apart()
        {
            _secrets.Set(SecretName.NimApiKey, "nvapi-test");

            _http.When(_ => true, """{"status":401,"title":"Unauthorized"}""", HttpStatusCode.Unauthorized);
            await Assert.ThrowsAsync<AiNotConfiguredException>(() => Client().CompleteAsync(Request()));

            var limited = new FakeHttpHandler().When(_ => true, """{"status":429}""", HttpStatusCode.TooManyRequests);
            var rateLimit = await Assert.ThrowsAsync<AiRateLimitedException>(() => new NimClient(new HttpClient(limited), _secrets).CompleteAsync(Request()));
            Assert.Null(rateLimit.RetryAfter);

            var down = new FakeHttpHandler().When(_ => true, "Bad gateway", HttpStatusCode.BadGateway);
            await Assert.ThrowsAsync<AiRequestException>(() => new NimClient(new HttpClient(down), _secrets).CompleteAsync(Request()));
        }
    }
}
