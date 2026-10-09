using Core.Enums;
using Persistence.Tests;
using Services.Common;
using Services.Jobs;

namespace Services.Tests.Jobs
{
    public class JobServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private JobService Service() => new(_db.CreateUnitOfWork());

        private static CreateJobRequest Job(string title, string company = "ACME GmbH") =>
            new(title, company, "We build APIs in C#.", Location: "Berlin", Url: "https://acme.example/jobs/1", ContactEmail: "jobs@acme.example");

        [Fact]
        public async Task Manually_entered_job_is_listed_as_new()
        {
            var created = await Service().CreateAsync(Job("Backend Developer"));

            var listed = Assert.Single(await Service().ListAsync(new JobListFilter()));
            Assert.Equal(created.Id, listed.Id);
            Assert.Equal("Backend Developer", listed.Title);
            Assert.Equal(JobSource.Manual, listed.Source);
            Assert.Equal(JobPostingState.New, listed.State);
        }

        [Fact]
        public async Task Job_details_include_the_description_and_job_can_be_deleted()
        {
            var created = await Service().CreateAsync(Job("Backend Developer"));

            var details = await Service().GetAsync(created.Id);
            Assert.Equal("We build APIs in C#.", details.Description);
            Assert.Equal("jobs@acme.example", details.ContactEmail);

            await Service().DeleteAsync(created.Id);
            await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync(created.Id));
        }

        [Fact]
        public async Task Jobs_are_filtered_by_state_and_search_text_and_dismissed_are_hidden()
        {
            var backend = await Service().CreateAsync(Job("Backend Developer", "ACME GmbH"));
            var frontend = await Service().CreateAsync(Job("Angular Developer", "Contoso AG"));
            var support = await Service().CreateAsync(Job("IT Support", "ACME GmbH"));
            await Service().SetStateAsync(backend.Id, JobPostingState.Bookmarked);
            await Service().SetStateAsync(support.Id, JobPostingState.Dismissed);

            Assert.Equal(["Angular Developer", "Backend Developer"], (await Service().ListAsync(new JobListFilter())).Select(j => j.Title).Order());
            Assert.Equal(["Backend Developer"], (await Service().ListAsync(new JobListFilter(Search: "acme"))).Select(j => j.Title));
            Assert.Equal(["Backend Developer"], (await Service().ListAsync(new JobListFilter(State: JobPostingState.Bookmarked))).Select(j => j.Title));
            Assert.Equal(3, (await Service().ListAsync(new JobListFilter(IncludeDismissed: true))).Count);
            Assert.Equal(["IT Support"], (await Service().ListAsync(new JobListFilter(State: JobPostingState.Dismissed))).Select(j => j.Title));
        }
    }
}
