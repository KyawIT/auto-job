using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Services.Jobs;

namespace api.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly JobService _jobs;
        private readonly JobSearchService _search;

        public JobsController(JobService jobs, JobSearchService search)
        {
            _jobs = jobs;
            _search = search;
        }

        [HttpGet]
        public Task<IReadOnlyList<JobSummaryDto>> List([FromQuery] JobPostingState? state, [FromQuery] string? search,
            [FromQuery] bool includeDismissed, CancellationToken ct) =>
            _jobs.ListAsync(new JobListFilter(state, search, includeDismissed), ct);

        [HttpGet("{id:int}")]
        public Task<JobDto> Get(int id, CancellationToken ct) => _jobs.GetAsync(id, ct);

        [HttpPost]
        public async Task<ActionResult<JobDto>> Create(CreateJobRequest request, CancellationToken ct)
        {
            var job = await _jobs.CreateAsync(request, ct);
            return Created($"/api/jobs/{job.Id}", job);
        }

        [HttpPut("{id:int}/state")]
        public Task<JobDto> SetState(int id, SetJobStateRequest request, CancellationToken ct) =>
            _jobs.SetStateAsync(id, request.State, ct);

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _jobs.DeleteAsync(id, ct);
            return NoContent();
        }

        [HttpPost("search")]
        public Task<SearchResultDto> Search(SearchJobsRequest request, CancellationToken ct) => _search.SearchAsync(request, ct);

        public record SetJobStateRequest(JobPostingState State);
    }
}
