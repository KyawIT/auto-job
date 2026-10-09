using Core.Enums;
using Microsoft.Extensions.Time.Testing;
using Persistence.Tests;
using Services.Jobs;
using Services.Tests.Support;
using static Services.Tests.Support.FakeJobSearchClient;

namespace Services.Tests.Jobs
{
    public class JobSearchServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();
        private readonly FakeJobSearchClient _adzuna = new(JobSource.Adzuna);
        private readonly FakeJobSearchClient _arbeitsagentur = new(JobSource.BundesagenturFuerArbeit);
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 9, 8, 0, 0, TimeSpan.Zero));

        public void Dispose() => _db.Dispose();

        private JobSearchService Service() => new(_db.CreateUnitOfWork(), [_adzuna, _arbeitsagentur], _time);

        private static SearchJobsRequest DotNetInBerlin(params JobSource[] sources) =>
            new(".NET Developer", "Berlin", RadiusKm: 25, Sources: sources);

        [Fact]
        public async Task Found_jobs_are_saved_once_even_when_searching_again()
        {
            _adzuna.Jobs.Add(Found("A-1", "Backend Developer"));
            _adzuna.Jobs.Add(Found("A-2", "API Developer"));

            var first = await Service().SearchAsync(DotNetInBerlin(JobSource.Adzuna));
            var second = await Service().SearchAsync(DotNetInBerlin(JobSource.Adzuna));

            Assert.Equal(2, first.NewCount);
            Assert.Equal(0, second.NewCount);
            Assert.Equal(["API Developer", "Backend Developer"], second.Jobs.Select(j => j.Title).Order());
            Assert.Equal(2, (await new JobService(_db.CreateUnitOfWork()).ListAsync(new JobListFilter())).Count);
        }

        [Fact]
        public async Task Failing_source_is_reported_while_other_sources_still_deliver()
        {
            _adzuna.Failure = new Services.Ports.JobSourceException("Adzuna API key is missing.");
            _arbeitsagentur.Jobs.Add(Found("BA-1", "Fachinformatiker Anwendungsentwicklung"));

            var result = await Service().SearchAsync(DotNetInBerlin(JobSource.Adzuna, JobSource.BundesagenturFuerArbeit));

            Assert.Equal(["Fachinformatiker Anwendungsentwicklung"], result.Jobs.Select(j => j.Title));
            var error = Assert.Single(result.Errors);
            Assert.Equal(JobSource.Adzuna, error.Source);
            Assert.Equal("Adzuna API key is missing.", error.Message);
        }

        [Fact]
        public async Task Same_job_twice_in_one_response_is_saved_once()
        {
            _adzuna.Jobs.Add(Found("A-1", "Backend Developer"));
            _adzuna.Jobs.Add(Found("A-1", "Backend Developer"));

            var result = await Service().SearchAsync(DotNetInBerlin(JobSource.Adzuna));

            Assert.Equal(1, result.NewCount);
            Assert.Single(result.Jobs);
        }

        private static SaveSearchRequest Saved(string name, int intervalMinutes = 60, bool active = true) =>
            new(name, ".NET Developer", "Berlin", 25, [JobSource.Adzuna], intervalMinutes, active);

        [Fact]
        public async Task Only_due_and_active_saved_searches_are_refreshed()
        {
            _adzuna.Jobs.Add(Found("A-1", "Backend Developer"));
            var hourly = await Service().CreateSavedSearchAsync(Saved("Hourly", intervalMinutes: 60));
            await Service().CreateSavedSearchAsync(Saved("Paused", active: false));

            Assert.Equal(1, await Service().RefreshDueAsync());     // never ran → due
            _time.Advance(TimeSpan.FromMinutes(30));
            Assert.Equal(0, await Service().RefreshDueAsync());     // ran 30 min ago → not due
            Assert.Single(_adzuna.Queries);

            var searches = await Service().ListSavedSearchesAsync();
            Assert.Equal(new DateTime(2026, 10, 9, 8, 0, 0), searches.Single(s => s.Id == hourly.Id).LastRunAt);
            Assert.Null(searches.Single(s => s.Name == "Paused").LastRunAt);
            Assert.Equal(hourly.Id, (await _db.CreateUnitOfWork().JobPostings.FindByExternalIdAsync(JobSource.Adzuna, "A-1"))!.SavedSearchId);
        }

        [Fact]
        public async Task Saved_search_can_be_changed_and_deleted()
        {
            var created = await Service().CreateSavedSearchAsync(Saved("Dev"));

            await Service().UpdateSavedSearchAsync(created.Id, Saved("Dev Hamburg") with { Location = "Hamburg" });
            Assert.Equal("Hamburg", Assert.Single(await Service().ListSavedSearchesAsync()).Location);

            await Service().DeleteSavedSearchAsync(created.Id);
            Assert.Empty(await Service().ListSavedSearchesAsync());
        }
    }
}
