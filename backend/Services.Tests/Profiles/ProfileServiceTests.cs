using Core.Enums;
using Persistence.Tests;
using Services.Common;
using Services.Profiles;

namespace Services.Tests.Profiles
{
    public class ProfileServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        // A new service per call, like one HTTP request each.
        private ProfileService Service() => new(_db.CreateUnitOfWork());

        private static SaveProfileRequest Anna() =>
            new("Anna", "Schmidt", "anna@example.com", Phone: "0151 123", Street: "Hauptstr. 1", PostalCode: "10115",
                City: "Berlin", Country: "Deutschland", DateOfBirth: null, Summary: "Backend developer", DesiredPosition: ".NET Developer");

        [Fact]
        public async Task Saved_profile_is_returned()
        {
            await Service().SaveAsync(Anna());

            var profile = await Service().GetAsync();

            Assert.NotNull(profile);
            Assert.Equal("Anna", profile.FirstName);
            Assert.Equal("Berlin", profile.City);
            Assert.Equal(".NET Developer", profile.DesiredPosition);
        }

        [Fact]
        public async Task Saving_again_updates_the_single_profile()
        {
            var first = await Service().SaveAsync(Anna());
            var second = await Service().SaveAsync(Anna() with { City = "Hamburg" });

            Assert.Equal(first.Id, second.Id);
            Assert.Equal("Hamburg", (await Service().GetAsync())!.City);
        }

        [Fact]
        public async Task Profile_without_name_or_with_invalid_email_is_rejected()
        {
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => Service().SaveAsync(Anna() with { FirstName = " ", Email = "not-an-email" }));

            Assert.Equal(["Email", "FirstName"], ex.Errors.Keys.Order());
            Assert.Null(await Service().GetAsync());
        }

        private static WorkExperienceRequest Acme() =>
            new("ACME GmbH", "Software Developer", "Berlin", new DateOnly(2021, 3, 1), EndDate: null, Description: "APIs in C#");

        [Fact]
        public async Task Work_experience_is_added_to_the_profile()
        {
            await Service().SaveAsync(Anna());

            await Service().AddWorkExperienceAsync(Acme());

            var experience = Assert.Single((await Service().GetAsync())!.WorkExperiences);
            Assert.Equal("ACME GmbH", experience.Company);
            Assert.Null(experience.EndDate);
        }

        [Fact]
        public async Task Work_experience_ending_before_it_starts_is_rejected()
        {
            await Service().SaveAsync(Anna());

            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                Service().AddWorkExperienceAsync(Acme() with { StartDate = new DateOnly(2022, 1, 1), EndDate = new DateOnly(2021, 12, 31) }));

            Assert.Equal(["EndDate"], ex.Errors.Keys);
        }

        [Fact]
        public async Task Work_experience_can_be_updated_and_deleted()
        {
            await Service().SaveAsync(Anna());
            var added = await Service().AddWorkExperienceAsync(Acme());

            await Service().UpdateWorkExperienceAsync(added.Id, Acme() with { Role = "Senior Developer" });
            Assert.Equal("Senior Developer", Assert.Single((await Service().GetAsync())!.WorkExperiences).Role);

            await Service().DeleteWorkExperienceAsync(added.Id);
            Assert.Empty((await Service().GetAsync())!.WorkExperiences);
        }

        [Fact]
        public async Task Updating_unknown_work_experience_is_not_found()
        {
            await Service().SaveAsync(Anna());

            await Assert.ThrowsAsync<NotFoundException>(() => Service().UpdateWorkExperienceAsync(999, Acme()));
        }

        [Fact]
        public async Task Education_can_be_added_updated_and_deleted()
        {
            await Service().SaveAsync(Anna());
            var bachelor = new EducationRequest("TU Berlin", "B.Sc. Informatik", "Informatik", "1,7", new DateOnly(2016, 10, 1), new DateOnly(2020, 9, 30));

            var added = await Service().AddEducationAsync(bachelor);
            await Service().UpdateEducationAsync(added.Id, bachelor with { Grade = "1,5" });
            Assert.Equal("1,5", Assert.Single((await Service().GetAsync())!.Educations).Grade);

            await Service().DeleteEducationAsync(added.Id);
            Assert.Empty((await Service().GetAsync())!.Educations);
        }

        [Fact]
        public async Task Skills_can_be_added_updated_and_deleted()
        {
            await Service().SaveAsync(Anna());

            var csharp = await Service().AddSkillAsync(new SkillRequest("C#", SkillLevel.Advanced));
            await Service().UpdateSkillAsync(csharp.Id, new SkillRequest("C#", SkillLevel.Expert));
            Assert.Equal(SkillLevel.Expert, Assert.Single((await Service().GetAsync())!.Skills).Level);

            await Service().DeleteSkillAsync(csharp.Id);
            Assert.Empty((await Service().GetAsync())!.Skills);
        }

        [Fact]
        public async Task Languages_can_be_added_updated_and_deleted()
        {
            await Service().SaveAsync(Anna());

            var english = await Service().AddLanguageAsync(new LanguageRequest("English", LanguageLevel.B2));
            await Service().UpdateLanguageAsync(english.Id, new LanguageRequest("English", LanguageLevel.C1));
            Assert.Equal(LanguageLevel.C1, Assert.Single((await Service().GetAsync())!.Languages).Level);

            await Service().DeleteLanguageAsync(english.Id);
            Assert.Empty((await Service().GetAsync())!.Languages);
        }
    }
}
