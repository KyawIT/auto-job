using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Services.Ports;

namespace Infrastructure.Ai
{
    // NVIDIA NIM (build.nvidia.com), OpenAI-compatible chat completions with the user's own key.
    public class NimClient : IAiClient
    {
        private const string Endpoint = "https://integrate.api.nvidia.com/v1/chat/completions";

        private readonly HttpClient _http;
        private readonly ISecretStore _secrets;

        public NimClient(HttpClient http, ISecretStore secrets)
        {
            _http = http;
            _secrets = secrets;
        }

        public async Task<string> CompleteAsync(ChatRequest request, CancellationToken ct = default)
        {
            var key = _secrets.Get(SecretName.NimApiKey)
                ?? throw new AiNotConfiguredException("The NVIDIA NIM API key is not set (Settings → API keys).");

            using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(new CompletionRequest(
                    request.Model,
                    request.Messages.Select(m => new Message(m.Role, m.Content)).ToList(),
                    request.Temperature,
                    request.MaxTokens)),
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(message, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new AiRequestException("NVIDIA NIM is not reachable.", ex);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                throw new AiRequestException("NVIDIA NIM did not answer in time.", ex);
            }

            using (response)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
                        throw new AiNotConfiguredException("NVIDIA rejected the API key.");
                    case HttpStatusCode.TooManyRequests:
                        throw new AiRateLimitedException(response.Headers.RetryAfter?.Delta);
                    case HttpStatusCode.NotFound:
                        throw new AiRequestException($"Model '{request.Model}' is not available on NVIDIA NIM.");
                }
                if (!response.IsSuccessStatusCode)
                    throw new AiRequestException($"NVIDIA NIM request failed ({(int)response.StatusCode}).");

                var completion = await response.Content.ReadFromJsonAsync<CompletionResponse>(ct);
                return completion?.Choices.FirstOrDefault()?.Message.Content
                    ?? throw new AiRequestException("NVIDIA NIM returned no answer.");
            }
        }

        private record CompletionRequest(
            [property: JsonPropertyName("model")] string Model,
            [property: JsonPropertyName("messages")] List<Message> Messages,
            [property: JsonPropertyName("temperature")] double Temperature,
            [property: JsonPropertyName("max_tokens")] int MaxTokens,
            [property: JsonPropertyName("stream")] bool Stream = false);

        private record Message(
            [property: JsonPropertyName("role")] string Role,
            [property: JsonPropertyName("content")] string Content);

        private record CompletionResponse([property: JsonPropertyName("choices")] List<Choice> Choices);

        private record Choice([property: JsonPropertyName("message")] Message Message);
    }
}
