using System.Text.Json;
using Services.Ports;

namespace Services.Tests.Support
{
    // Returns queued answers (or throws queued exceptions) and records every request. Thread-safe.
    public class FakeAiClient : IAiClient
    {
        private readonly Queue<Func<string>> _answers = new();
        private readonly List<ChatRequest> _requests = new();
        private Func<string>? _fallback;

        public IReadOnlyList<ChatRequest> Requests
        {
            get { lock (_requests) return _requests.ToList(); }
        }

        public FakeAiClient Returns(string content)
        {
            lock (_answers) _answers.Enqueue(() => content);
            return this;
        }

        public FakeAiClient ReturnsLetter(string coverLetter, string subject = "Bewerbung", string body = "Anbei meine Bewerbung.") =>
            Returns(Letter(coverLetter, subject, body));

        public FakeAiClient Throws(Exception exception)
        {
            lock (_answers) _answers.Enqueue(() => throw exception);
            return this;
        }

        // Used once the queue is empty.
        public FakeAiClient AlwaysReturnsLetter(string coverLetter)
        {
            _fallback = () => Letter(coverLetter, "Bewerbung", "Anbei");
            return this;
        }

        public FakeAiClient AlwaysThrows(Exception exception)
        {
            _fallback = () => throw exception;
            return this;
        }

        public Task<string> CompleteAsync(ChatRequest request, CancellationToken ct = default)
        {
            lock (_requests) _requests.Add(request);
            Func<string>? answer;
            lock (_answers) answer = _answers.Count > 0 ? _answers.Dequeue() : _fallback;
            if (answer == null) throw new InvalidOperationException("FakeAiClient: no answer queued.");
            return Task.FromResult(answer());
        }

        public string LastPrompt => string.Join("\n", Requests.Last().Messages.Select(m => m.Content));

        private static string Letter(string coverLetter, string subject, string body) =>
            JsonSerializer.Serialize(new { coverLetter, emailSubject = subject, emailBody = body });
    }
}
