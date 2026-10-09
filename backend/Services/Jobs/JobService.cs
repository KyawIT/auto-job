using Core.Contracts;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Services.Common;

namespace Services.Jobs
{
    // Job postings: manual entry, listing/filtering and triage (seen, bookmarked, dismissed).
    public class JobService
    {
        private readonly IUnitOfWork _uow;

        public JobService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<JobDto> CreateAsync(CreateJobRequest request, CancellationToken ct = default)
        {
            new Validator()
                .Required(nameof(request.Title), request.Title)
                .Required(nameof(request.CompanyName), request.CompanyName)
                .Required(nameof(request.Description), request.Description)
                .Email(nameof(request.ContactEmail), request.ContactEmail)
                .ThrowIfInvalid();

            var job = new JobPosting
            {
                Source = JobSource.Manual,
                Title = request.Title,
                CompanyName = request.CompanyName,
                Description = request.Description,
                Location = request.Location,
                Url = request.Url,
                ContactEmail = request.ContactEmail,
                ContactName = request.ContactName,
                EmploymentType = request.EmploymentType,
                WorkMode = request.WorkMode,
            };
            await _uow.JobPostings.AddAsync(job, ct);
            await _uow.SaveChangesAsync(ct);
            return job.ToDto();
        }

        public async Task<IReadOnlyList<JobSummaryDto>> ListAsync(JobListFilter filter, CancellationToken ct = default)
        {
            var jobs = await _uow.JobPostings.ListAsync(new JobPostingQuery(filter.State, filter.Search, filter.IncludeDismissed), ct);
            return jobs.Select(j => j.ToSummary()).ToList();
        }

        public async Task<JobDto> GetAsync(int id, CancellationToken ct = default) =>
            (await RequireAsync(id, ct)).ToDto();

        // A job with an application can't be deleted (ReferenceConstraintException → 409); dismiss it instead.
        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            _uow.JobPostings.Remove(await RequireAsync(id, ct));
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<JobDto> SetStateAsync(int id, JobPostingState state, CancellationToken ct = default)
        {
            var job = await RequireAsync(id, ct);
            job.State = state;
            await _uow.SaveChangesAsync(ct);
            return job.ToDto();
        }

        private async Task<JobPosting> RequireAsync(int id, CancellationToken ct) =>
            await _uow.JobPostings.GetWithApplicationAsync(id, ct) ?? throw new NotFoundException("Job", id);
    }
}
