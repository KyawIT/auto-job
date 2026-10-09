using Core.Entities;
using Core.Enums;
using Persistence.Tests;
using Services.Common;
using Services.Generation;
using Services.Profiles;
using Services.Tests.Support;

namespace Services.Tests.Generation
{
    public class ProfileImportServiceTests : IDisposable
    {
        private const string ExtractedCv = """
            {
              "firstName": "Anna", "lastName": "Schmidt", "email": "anna@example.com", "phone": "+49 151 1234567",
              "city": "Berlin", "summary": "Backend developer focused on .NET.", "desiredPosition": "Senior .NET Developer",
              "workExperiences": [
                { "company": "ACME GmbH", "role": "Software Developer", "location": "Berlin", "startDate": "2021-03", "endDate": null, "description": "REST APIs" },
                { "company": "Contoso AG", "role": "Working Student", "startDate": "2019-04", "endDate": "2021-02" }
              ],
              "educations": [ { "institution": "TU Berlin", "degree": "B.Sc.", "fieldOfStudy": "Informatik", "grade": "1,7", "startDate": "2016-10", "endDate": "2020-09" } ],
              "skills": [ { "name": "C#", "level": "Expert" }, { "name": "SQL", "level": null } ],
              "languages": [ { "name": "Deutsch", "level": "Native" }, { "name": "English", "level": "C1" } ]
            }
            """;

        private readonly TestDatabase _db = new();
        private readonly FakeAiClient _ai = new();
        private readonly Profile _profile;

        public ProfileImportServiceTests()
        {
            var uow = _db.CreateUnitOfWork();
            _profile = new Profile { FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com" };
            uow.Profiles.AddAsync(_profile).GetAwaiter().GetResult();
            uow.SaveChangesAsync().GetAwaiter().GetResult();
        }

        public void Dispose() => _db.Dispose();

        private ProfileImportService Service() => new(_db.CreateUnitOfWork(), _ai);

        private Task<ProfileDto?> Profile() => new ProfileService(_db.CreateUnitOfWork()).GetAsync();

        [Fact]
        public async Task Cv_fills_the_profile()
        {
            var cv = await Seed.DocumentAsync(_db.CreateUnitOfWork(), _profile, "Lebenslauf", attachByDefault: true, text: "Anna Schmidt … Lebenslauf …");
            _ai.Returns(ExtractedCv);

            await Service().ImportFromDocumentAsync(cv.Id);

            var profile = (await Profile())!;
            Assert.Equal("+49 151 1234567", profile.Phone);
            Assert.Equal("Senior .NET Developer", profile.DesiredPosition);
            Assert.Equal([("ACME GmbH", new DateOnly(2021, 3, 1), (DateOnly?)null), ("Contoso AG", new DateOnly(2019, 4, 1), new DateOnly(2021, 2, 1))],
                profile.WorkExperiences.Select(w => (w.Company, w.StartDate, w.EndDate)));
            Assert.Equal("1,7", Assert.Single(profile.Educations).Grade);
            Assert.Equal([("C#", (SkillLevel?)SkillLevel.Expert), ("SQL", null)], profile.Skills.Select(s => (s.Name, s.Level)));
            Assert.Equal([("Deutsch", LanguageLevel.Native), ("English", LanguageLevel.C1)], profile.Languages.Select(l => (l.Name, l.Level)));
            Assert.Contains("Anna Schmidt … Lebenslauf …", _ai.LastPrompt);
        }

        [Fact]
        public async Task Existing_data_is_neither_overwritten_nor_duplicated()
        {
            var profiles = new ProfileService(_db.CreateUnitOfWork());
            await profiles.SaveAsync(new SaveProfileRequest("Anna", "Schmidt", "anna@example.com", Phone: "030 999", DesiredPosition: "Tech Lead"));
            await new ProfileService(_db.CreateUnitOfWork()).AddWorkExperienceAsync(new WorkExperienceRequest("acme gmbh", "software developer", null, new DateOnly(2021, 1, 1)));
            await new ProfileService(_db.CreateUnitOfWork()).AddSkillAsync(new SkillRequest("c#", SkillLevel.Advanced));
            var cv = await Seed.DocumentAsync(_db.CreateUnitOfWork(), _profile, "Lebenslauf", attachByDefault: true, text: "CV text");
            _ai.Returns(ExtractedCv);

            await Service().ImportFromDocumentAsync(cv.Id);

            var profile = (await Profile())!;
            Assert.Equal("030 999", profile.Phone);
            Assert.Equal("Tech Lead", profile.DesiredPosition);
            Assert.Equal(["acme gmbh", "Contoso AG"], profile.WorkExperiences.Select(w => w.Company));
            Assert.Equal([("c#", (SkillLevel?)SkillLevel.Advanced), ("SQL", null)], profile.Skills.Select(s => (s.Name, s.Level)));
        }

        [Fact]
        public async Task Document_without_text_is_rejected_before_asking_the_ai()
        {
            var scan = await Seed.DocumentAsync(_db.CreateUnitOfWork(), _profile, "Scan", attachByDefault: false, text: null);

            await Assert.ThrowsAsync<ValidationException>(() => Service().ImportFromDocumentAsync(scan.Id));
            Assert.Empty(_ai.Requests);
        }
    }
}
