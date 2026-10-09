using Core.Contracts;
using Core.Entities;
using Services.Common;

namespace Services.Profiles
{
    // The single local profile and its details (experience, education, skills, languages).
    public class ProfileService
    {
        private readonly IUnitOfWork _uow;

        public ProfileService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ProfileDto?> GetAsync(CancellationToken ct = default)
        {
            var profile = await _uow.Profiles.GetAsync(ct);
            return profile?.ToDto();
        }

        // Creates the profile on first save, updates it afterwards.
        public async Task<ProfileDto> SaveAsync(SaveProfileRequest request, CancellationToken ct = default)
        {
            new Validator()
                .Required(nameof(request.FirstName), request.FirstName)
                .Required(nameof(request.LastName), request.LastName)
                .Required(nameof(request.Email), request.Email)
                .Email(nameof(request.Email), request.Email)
                .ThrowIfInvalid();

            var profile = await _uow.Profiles.GetAsync(ct);
            if (profile == null)
            {
                profile = new Profile();
                await _uow.Profiles.AddAsync(profile, ct);
            }

            profile.FirstName = request.FirstName;
            profile.LastName = request.LastName;
            profile.Email = request.Email;
            profile.Phone = request.Phone;
            profile.Street = request.Street;
            profile.PostalCode = request.PostalCode;
            profile.City = request.City;
            profile.Country = request.Country;
            profile.DateOfBirth = request.DateOfBirth;
            profile.Summary = request.Summary;
            profile.DesiredPosition = request.DesiredPosition;

            await _uow.SaveChangesAsync(ct);
            return profile.ToDto();
        }

        public async Task<WorkExperienceDto> AddWorkExperienceAsync(WorkExperienceRequest request, CancellationToken ct = default)
        {
            Validate(request);
            var profile = await RequireProfileAsync(ct);
            var experience = new WorkExperience { Profile = profile };
            Apply(experience, request);
            await _uow.WorkExperiences.AddAsync(experience, ct);
            await _uow.SaveChangesAsync(ct);
            return experience.ToDto();
        }

        public async Task<WorkExperienceDto> UpdateWorkExperienceAsync(int id, WorkExperienceRequest request, CancellationToken ct = default)
        {
            Validate(request);
            var experience = await _uow.WorkExperiences.GetByIdAsync(id, ct) ?? throw new NotFoundException("Work experience", id);
            Apply(experience, request);
            await _uow.SaveChangesAsync(ct);
            return experience.ToDto();
        }

        public async Task DeleteWorkExperienceAsync(int id, CancellationToken ct = default)
        {
            var experience = await _uow.WorkExperiences.GetByIdAsync(id, ct) ?? throw new NotFoundException("Work experience", id);
            _uow.WorkExperiences.Remove(experience);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<EducationDto> AddEducationAsync(EducationRequest request, CancellationToken ct = default)
        {
            Validate(request);
            var education = new Education { Profile = await RequireProfileAsync(ct) };
            Apply(education, request);
            await _uow.Educations.AddAsync(education, ct);
            await _uow.SaveChangesAsync(ct);
            return education.ToDto();
        }

        public async Task<EducationDto> UpdateEducationAsync(int id, EducationRequest request, CancellationToken ct = default)
        {
            Validate(request);
            var education = await _uow.Educations.GetByIdAsync(id, ct) ?? throw new NotFoundException("Education", id);
            Apply(education, request);
            await _uow.SaveChangesAsync(ct);
            return education.ToDto();
        }

        public async Task DeleteEducationAsync(int id, CancellationToken ct = default)
        {
            var education = await _uow.Educations.GetByIdAsync(id, ct) ?? throw new NotFoundException("Education", id);
            _uow.Educations.Remove(education);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<SkillDto> AddSkillAsync(SkillRequest request, CancellationToken ct = default)
        {
            new Validator().Required(nameof(request.Name), request.Name).ThrowIfInvalid();
            var skill = new Skill { Profile = await RequireProfileAsync(ct), Name = request.Name, Level = request.Level };
            await _uow.Skills.AddAsync(skill, ct);
            await _uow.SaveChangesAsync(ct);
            return skill.ToDto();
        }

        public async Task<SkillDto> UpdateSkillAsync(int id, SkillRequest request, CancellationToken ct = default)
        {
            new Validator().Required(nameof(request.Name), request.Name).ThrowIfInvalid();
            var skill = await _uow.Skills.GetByIdAsync(id, ct) ?? throw new NotFoundException("Skill", id);
            skill.Name = request.Name;
            skill.Level = request.Level;
            await _uow.SaveChangesAsync(ct);
            return skill.ToDto();
        }

        public async Task DeleteSkillAsync(int id, CancellationToken ct = default)
        {
            var skill = await _uow.Skills.GetByIdAsync(id, ct) ?? throw new NotFoundException("Skill", id);
            _uow.Skills.Remove(skill);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<LanguageDto> AddLanguageAsync(LanguageRequest request, CancellationToken ct = default)
        {
            new Validator().Required(nameof(request.Name), request.Name).ThrowIfInvalid();
            var language = new ProfileLanguage { Profile = await RequireProfileAsync(ct), Name = request.Name, Level = request.Level };
            await _uow.ProfileLanguages.AddAsync(language, ct);
            await _uow.SaveChangesAsync(ct);
            return language.ToDto();
        }

        public async Task<LanguageDto> UpdateLanguageAsync(int id, LanguageRequest request, CancellationToken ct = default)
        {
            new Validator().Required(nameof(request.Name), request.Name).ThrowIfInvalid();
            var language = await _uow.ProfileLanguages.GetByIdAsync(id, ct) ?? throw new NotFoundException("Language", id);
            language.Name = request.Name;
            language.Level = request.Level;
            await _uow.SaveChangesAsync(ct);
            return language.ToDto();
        }

        public async Task DeleteLanguageAsync(int id, CancellationToken ct = default)
        {
            var language = await _uow.ProfileLanguages.GetByIdAsync(id, ct) ?? throw new NotFoundException("Language", id);
            _uow.ProfileLanguages.Remove(language);
            await _uow.SaveChangesAsync(ct);
        }

        private static void Validate(WorkExperienceRequest request) =>
            new Validator()
                .Required(nameof(request.Company), request.Company)
                .Required(nameof(request.Role), request.Role)
                .Check(request.EndDate == null || request.EndDate >= request.StartDate, nameof(request.EndDate), "Must not be before the start date.")
                .ThrowIfInvalid();

        private static void Apply(WorkExperience experience, WorkExperienceRequest request)
        {
            experience.Company = request.Company;
            experience.Role = request.Role;
            experience.Location = request.Location;
            experience.StartDate = request.StartDate;
            experience.EndDate = request.EndDate;
            experience.Description = request.Description;
        }

        private static void Validate(EducationRequest request) =>
            new Validator()
                .Required(nameof(request.Institution), request.Institution)
                .Required(nameof(request.Degree), request.Degree)
                .Check(request.EndDate == null || request.StartDate == null || request.EndDate >= request.StartDate, nameof(request.EndDate), "Must not be before the start date.")
                .ThrowIfInvalid();

        private static void Apply(Education education, EducationRequest request)
        {
            education.Institution = request.Institution;
            education.Degree = request.Degree;
            education.FieldOfStudy = request.FieldOfStudy;
            education.Grade = request.Grade;
            education.StartDate = request.StartDate;
            education.EndDate = request.EndDate;
        }

        private async Task<Profile> RequireProfileAsync(CancellationToken ct) =>
            await _uow.Profiles.GetAsync(ct) ?? throw new NotFoundException("Profile");
    }
}
