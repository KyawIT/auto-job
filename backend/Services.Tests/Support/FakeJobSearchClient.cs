using Core.Enums;
using Services.Ports;

namespace Services.Tests.Support
{
    public class FakeJobSearchClient : IJobSearchClient
    {
        public FakeJobSearchClient(JobSource source)
        {
            Source = source;
        }

        public JobSource Source { get; }
        public List<FoundJob> Jobs { get; } = new();
        public Exception? Failure { get; set; }
        public List<JobQuery> Queries { get; } = new();

        public Task<IReadOnlyList<FoundJob>> SearchAsync(JobQuery query, CancellationToken ct = default)
        {
            Queries.Add(query);
            if (Failure != null) throw Failure;
            return Task.FromResult<IReadOnlyList<FoundJob>>(Jobs.ToList());
        }

        public static FoundJob Found(string externalId, string title, string company = "ACME GmbH") =>
            new(externalId, title, company, "Berlin", $"{title} at {company}", $"https://jobs.example/{externalId}");
    }
}
