using System.Net;
using System.Text;

namespace Infrastructure.Tests.Support
{
    // Answers HTTP calls from canned responses and records what was requested.
    public class FakeHttpHandler : HttpMessageHandler
    {
        private readonly List<(Func<HttpRequestMessage, bool> Matches, HttpStatusCode Status, string Body)> _routes = new();

        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> RequestBodies { get; } = new();

        public FakeHttpHandler When(Func<HttpRequestMessage, bool> matches, string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _routes.Add((matches, status, body));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct));

            var route = _routes.FirstOrDefault(r => r.Matches(request));
            if (route.Matches == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            return new HttpResponseMessage(route.Status) { Content = new StringContent(route.Body, Encoding.UTF8, "application/json") };
        }
    }
}
