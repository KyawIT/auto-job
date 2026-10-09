using Core.Enums;

namespace Services.Profiles
{
    public record SaveProfileRequest(
        string FirstName,
        string LastName,
        string Email,
        string? Phone = null,
        string? Street = null,
        string? PostalCode = null,
        string? City = null,
        string? Country = null,
        DateOnly? DateOfBirth = null,
        string? Summary = null,
        string? DesiredPosition = null);

    public record ProfileDto(
        int Id,
        string FirstName,
        string LastName,
        string Email,
        string? Phone,
        string? Street,
        string? PostalCode,
        string? City,
        string? Country,
        DateOnly? DateOfBirth,
        string? Summary,
        string? DesiredPosition,
        IReadOnlyList<WorkExperienceDto> WorkExperiences,
        IReadOnlyList<EducationDto> Educations,
        IReadOnlyList<SkillDto> Skills,
        IReadOnlyList<LanguageDto> Languages);

    public record WorkExperienceRequest(
        string Company,
        string Role,
        string? Location,
        DateOnly StartDate,
        DateOnly? EndDate = null,
        string? Description = null);

    public record WorkExperienceDto(int Id, string Company, string Role, string? Location, DateOnly StartDate, DateOnly? EndDate, string? Description);

    public record EducationRequest(
        string Institution,
        string Degree,
        string? FieldOfStudy = null,
        string? Grade = null,
        DateOnly? StartDate = null,
        DateOnly? EndDate = null);

    public record EducationDto(int Id, string Institution, string Degree, string? FieldOfStudy, string? Grade, DateOnly? StartDate, DateOnly? EndDate);

    public record SkillRequest(string Name, SkillLevel? Level = null);

    public record SkillDto(int Id, string Name, SkillLevel? Level);

    public record LanguageRequest(string Name, LanguageLevel Level);

    public record LanguageDto(int Id, string Name, LanguageLevel Level);
}
