using Core.Contracts;
using Core.Entities;
using Core.Enums;
using Services.Common;
using Services.Ports;

namespace Services.Jobs
{
    // Searches external job sources and stores what they find (each job only once).
    public class JobSearchService
    {
        private readonly IUnitOfWork _uow;
        private readonly IReadOnlyDictionary<JobSource, IJobSearchClient> _clients;
        private readonly TimeProvider _time;

        public JobSearchService(IUnitOfWork uow, IEnumerable<IJobSearchClient> clients, TimeProvider time)
        {
            _uow = uow;
            _clients = clients.ToDictionary(c => c.Source);
            _time = time;
        }

        public async Task<SearchResultDto> SearchAsync(SearchJobsRequest request, CancellationToken ct = default)
        {
            new Validator()
                .Required(nameof(request.Keywords), request.Keywords)
                .Check(request.Sources.Count > 0, nameof(request.Sources), "Select at least one source.")
                .ThrowIfInvalid();

            return await RunAsync(request, savedSearch: null, ct);
        }

        public async Task<IReadOnlyList<SavedSearchDto>> ListSavedSearchesAsync(CancellationToken ct = default) =>
            (await _uow.SavedSearches.GetAllAsync(ct)).OrderBy(s => s.Name).Select(s => s.ToDto()).ToList();

        public async Task<SavedSearchDto> CreateSavedSearchAsync(SaveSearchRequest request, CancellationToken ct = default)
        {
            var search = new SavedSearch();
            Apply(search, request);
            await _uow.SavedSearches.AddAsync(search, ct);
            await _uow.SaveChangesAsync(ct);
            return search.ToDto();
        }

        public async Task<SavedSearchDto> UpdateSavedSearchAsync(int id, SaveSearchRequest request, CancellationToken ct = default)
        {
            var search = await RequireSavedSearchAsync(id, ct);
            Apply(search, request);
            await _uow.SaveChangesAsync(ct);
            return search.ToDto();
        }

        // The jobs it found are kept (SavedSearchId becomes null).
        public async Task DeleteSavedSearchAsync(int id, CancellationToken ct = default)
        {
            _uow.SavedSearches.Remove(await RequireSavedSearchAsync(id, ct));
            await _uow.SaveChangesAsync(ct);
        }

        // Called periodically by the background refresher. Returns how many new jobs were found.
        public async Task<int> RefreshDueAsync(CancellationToken ct = default)
        {
            var now = _time.GetUtcNow().UtcDateTime;
            var due = (await _uow.SavedSearches.GetAllAsync(ct))
                .Where(s => s.IsActive && (s.LastRunAt == null || s.LastRunAt.Value.AddMinutes(s.RefreshIntervalMinutes) <= now));

            var newJobs = 0;
            foreach (var search in due)
            {
                var result = await RunAsync(new SearchJobsRequest(search.Keywords, search.Location, search.RadiusKm, search.Sources), search, ct);
                search.LastRunAt = now;
                await _uow.SaveChangesAsync(ct);
                newJobs += result.NewCount;
            }
            return newJobs;
        }

        private static void Apply(SavedSearch search, SaveSearchRequest request)
        {
            new Validator()
                .Required(nameof(request.Name), request.Name)
                .Required(nameof(request.Keywords), request.Keywords)
                .Check(request.Sources.Count > 0, nameof(request.Sources), "Select at least one source.")
                .Check(request.RefreshIntervalMinutes >= MinRefreshMinutes, nameof(request.RefreshIntervalMinutes), $"Must be at least {MinRefreshMinutes} minutes.")
                .ThrowIfInvalid();

            search.Name = request.Name;
            search.Keywords = request.Keywords;
            search.Location = request.Location;
            search.RadiusKm = request.RadiusKm;
            search.Sources = request.Sources.Distinct().ToList();
            search.RefreshIntervalMinutes = request.RefreshIntervalMinutes;
            search.IsActive = request.IsActive;
        }

        // Job APIs have request quotas; refreshing more often than this gains nothing.
        private const int MinRefreshMinutes = 15;

        private async Task<SavedSearch> RequireSavedSearchAsync(int id, CancellationToken ct) =>
            await _uow.SavedSearches.GetByIdAsync(id, ct) ?? throw new NotFoundException("Saved search", id);

        private async Task<SearchResultDto> RunAsync(SearchJobsRequest request, SavedSearch? savedSearch, CancellationToken ct)
        {
            var query = new JobQuery(request.Keywords, request.Location, request.RadiusKm);
            var jobs = new List<JobPosting>();
            var errors = new List<SourceErrorDto>();
            var newCount = 0;

            foreach (var source in request.Sources.Distinct())
            {
                IReadOnlyList<FoundJob> found;
                try
                {
                    found = await RequireClient(source).SearchAsync(query, ct);
                }
                catch (JobSourceException ex)
                {
                    // One failing source must not hide the results of the others.
                    errors.Add(new SourceErrorDto(source, ex.Message));
                    continue;
                }

                // Sources sometimes repeat a job within one response; unsaved jobs aren't visible to the lookup.
                foreach (var item in found.DistinctBy(f => f.ExternalId))
                {
                    var job = await _uow.JobPostings.FindByExternalIdAsync(source, item.ExternalId, ct);
                    if (job == null)
                    {
                        job = item.ToEntity(source);
                        job.SavedSearch = savedSearch;
                        await _uow.JobPostings.AddAsync(job, ct);
                        newCount++;
                    }
                    jobs.Add(job);
                }
            }

            await _uow.SaveChangesAsync(ct);
            return new SearchResultDto(jobs.DistinctBy(j => j.Id).Select(j => j.ToSummary()).ToList(), newCount, errors);
        }

        private IJobSearchClient RequireClient(JobSource source) =>
            _clients.TryGetValue(source, out var client)
                ? client
                : throw new ValidationException(new Dictionary<string, string[]> { ["Sources"] = [$"{source} is not a searchable source."] });
    }
}
