namespace Services.Ports
{
    public record ChatMessage(string Role, string Content)
    {
        public static ChatMessage System(string content) => new("system", content);
        public static ChatMessage User(string content) => new("user", content);
    }

    public record ChatRequest(string Model, IReadOnlyList<ChatMessage> Messages, double Temperature = 0.6, int MaxTokens = 2500);

    // A chat-completion LLM (NVIDIA NIM). Returns the assistant's text.
    public interface IAiClient
    {
        Task<string> CompleteAsync(ChatRequest request, CancellationToken ct = default);
    }

    public abstract class AiException : Exception
    {
        protected AiException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    // No key set, or the provider rejected it.
    public class AiNotConfiguredException : AiException
    {
        public AiNotConfiguredException(string message) : base(message) { }
    }

    // HTTP 429 from the provider; worth retrying after RetryAfter.
    public class AiRateLimitedException : AiException
    {
        public AiRateLimitedException(TimeSpan? retryAfter) : base("The AI provider's rate limit was reached. Try again shortly.")
        {
            RetryAfter = retryAfter;
        }

        public TimeSpan? RetryAfter { get; }
    }

    // Outage, timeout or an answer that can't be used.
    public class AiRequestException : AiException
    {
        public AiRequestException(string message, Exception? innerException = null) : base(message, innerException) { }
    }
}
