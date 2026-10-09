using Core.Entities;

namespace Services.Profiles
{
    internal static class ProfileMappings
    {
        public static ProfileDto ToDto(this Profile p) => new(
            p.Id, p.FirstName, p.LastName, p.Email, p.Phone, p.Street, p.PostalCode,
            p.City, p.Country, p.DateOfBirth, p.Summary, p.DesiredPosition,
            p.WorkExperiences.OrderByDescending(w => w.StartDate).Select(ToDto).ToList(),
            p.Educations.OrderByDescending(e => e.StartDate).Select(ToDto).ToList(),
            p.Skills.OrderBy(s => s.Name).Select(ToDto).ToList(),
            p.Languages.OrderBy(l => l.Name).Select(ToDto).ToList());

        public static WorkExperienceDto ToDto(this WorkExperience w) =>
            new(w.Id, w.Company, w.Role, w.Location, w.StartDate, w.EndDate, w.Description);

        public static EducationDto ToDto(this Education e) =>
            new(e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.Grade, e.StartDate, e.EndDate);

        public static SkillDto ToDto(this Skill s) => new(s.Id, s.Name, s.Level);

        public static LanguageDto ToDto(this ProfileLanguage l) => new(l.Id, l.Name, l.Level);
    }
}
