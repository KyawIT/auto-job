using System.Globalization;
using System.Text.Json;
using Core.Contracts;
using Core.Entities;
using Core.Enums;
using Services.Common;
using Services.Ports;
using Services.Profiles;

namespace Services.Generation
{
    // Reads a CV with the AI and fills the profile: empty fields are filled, existing data is never
    // overwritten, and entries that already exist (same company+role, institution+degree, name) are skipped.
    public class ProfileImportService
    {
        private const string Instructions = """
            Extract the applicant's data from the CV text. Answer with JSON only, no markdown, using exactly this shape.
            Use null for anything not in the CV. Never guess. Dates as "YYYY-MM".
            {
              "firstName": "", "lastName": "", "email": "", "phone": "", "street": "", "postalCode": "", "city": "", "country": "",
              "summary": "2-3 sentence professional summary based only on the CV",
              "desiredPosition": "",
              "workExperiences": [ { "company": "", "role": "", "location": "", "startDate": "YYYY-MM", "endDate": "YYYY-MM or null if current", "description": "" } ],
              "educations": [ { "institution": "", "degree": "", "fieldOfStudy": "", "grade": "", "startDate": "YYYY-MM", "endDate": "YYYY-MM" } ],
              "skills": [ { "name": "", "level": "Basic | Intermediate | Advanced | Expert | null" } ],
              "languages": [ { "name": "", "level": "A1 | A2 | B1 | B2 | C1 | C2 | Native" } ]
            }
            """;

        private readonly IUnitOfWork _uow;
        private readonly IAiClient _ai;

        public ProfileImportService(IUnitOfWork uow, IAiClient ai)
        {
            _uow = uow;
            _ai = ai;
        }

        public async Task<ProfileDto> ImportFromDocumentAsync(int documentId, CancellationToken ct = default)
        {
            var document = await _uow.Documents.GetByIdAsync(documentId, ct) ?? throw new NotFoundException("Document", documentId);
            new Validator()
                .Check(!string.IsNullOrWhiteSpace(document.ExtractedText), nameof(documentId), "The document has no readable text (scanned PDFs need OCR).")
                .ThrowIfInvalid();
            var profile = await _uow.Profiles.GetAsync(ct) ?? throw new NotFoundException("Profile");
            var settings = await _uow.AppSettings.GetAsync(ct);
            var model = string.IsNullOrWhiteSpace(settings.NimModel) ? GenerationService.DefaultModel : settings.NimModel;

            var answer = await _ai.CompleteAsync(new ChatRequest(model,
                [ChatMessage.System(Instructions), ChatMessage.User(document.ExtractedText!)], Temperature: 0.1, MaxTokens: 3000), ct);
            Merge(profile, Parse(answer));

            await _uow.SaveChangesAsync(ct);
            return profile.ToDto();
        }

        private static void Merge(Profile profile, ExtractedProfile cv)
        {
            profile.FirstName = FirstSet(profile.FirstName, cv.FirstName) ?? "";
            profile.LastName = FirstSet(profile.LastName, cv.LastName) ?? "";
            profile.Email = FirstSet(profile.Email, cv.Email) ?? "";
            profile.Phone = FirstSet(profile.Phone, cv.Phone);
            profile.Street = FirstSet(profile.Street, cv.Street);
            profile.PostalCode = FirstSet(profile.PostalCode, cv.PostalCode);
            profile.City = FirstSet(profile.City, cv.City);
            profile.Country = FirstSet(profile.Country, cv.Country);
            profile.Summary = FirstSet(profile.Summary, cv.Summary);
            profile.DesiredPosition = FirstSet(profile.DesiredPosition, cv.DesiredPosition);

            foreach (var w in cv.WorkExperiences ?? [])
            {
                var start = ParseMonth(w.StartDate);
                if (string.IsNullOrWhiteSpace(w.Company) || string.IsNullOrWhiteSpace(w.Role) || start == null)
                    continue;
                if (profile.WorkExperiences.Any(e => Same(e.Company, w.Company) && Same(e.Role, w.Role)))
                    continue;
                profile.WorkExperiences.Add(new WorkExperience
                {
                    Company = w.Company, Role = w.Role, Location = w.Location, StartDate = start.Value,
                    EndDate = ParseMonth(w.EndDate), Description = w.Description,
                });
            }

            foreach (var e in cv.Educations ?? [])
            {
                if (string.IsNullOrWhiteSpace(e.Institution) || string.IsNullOrWhiteSpace(e.Degree))
                    continue;
                if (profile.Educations.Any(x => Same(x.Institution, e.Institution) && Same(x.Degree, e.Degree)))
                    continue;
                profile.Educations.Add(new Education
                {
                    Institution = e.Institution, Degree = e.Degree, FieldOfStudy = e.FieldOfStudy, Grade = e.Grade,
                    StartDate = ParseMonth(e.StartDate), EndDate = ParseMonth(e.EndDate),
                });
            }

            foreach (var s in cv.Skills ?? [])
            {
                if (string.IsNullOrWhiteSpace(s.Name) || profile.Skills.Any(x => Same(x.Name, s.Name)))
                    continue;
                profile.Skills.Add(new Skill { Name = s.Name, Level = Enum.TryParse<SkillLevel>(s.Level, true, out var level) ? level : null });
            }

            foreach (var l in cv.Languages ?? [])
            {
                if (string.IsNullOrWhiteSpace(l.Name) || !Enum.TryParse<LanguageLevel>(l.Level, true, out var level) || profile.Languages.Any(x => Same(x.Name, l.Name)))
                    continue;
                profile.Languages.Add(new ProfileLanguage { Name = l.Name, Level = level });
            }
        }

        private static ExtractedProfile Parse(string answer)
        {
            var start = answer.IndexOf('{');
            var end = answer.LastIndexOf('}');
            if (start < 0 || end <= start)
                throw new AiRequestException("The AI answer did not contain the expected JSON.");
            try
            {
                return JsonSerializer.Deserialize<ExtractedProfile>(answer[start..(end + 1)], new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? throw new AiRequestException("The AI answer was empty.");
            }
            catch (JsonException ex)
            {
                throw new AiRequestException("The AI answer was not valid JSON.", ex);
            }
        }

        private static DateOnly? ParseMonth(string? value) =>
            DateOnly.TryParseExact(value, ["yyyy-MM", "yyyy-MM-dd", "yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

        private static string? FirstSet(string? current, string? extracted) =>
            string.IsNullOrWhiteSpace(current) ? (string.IsNullOrWhiteSpace(extracted) ? current : extracted.Trim()) : current;

        private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

        private record ExtractedProfile(
            string? FirstName, string? LastName, string? Email, string? Phone, string? Street, string? PostalCode,
            string? City, string? Country, string? Summary, string? DesiredPosition,
            List<ExtractedWork>? WorkExperiences, List<ExtractedEducation>? Educations,
            List<ExtractedSkill>? Skills, List<ExtractedLanguage>? Languages);

        private record ExtractedWork(string? Company, string? Role, string? Location, string? StartDate, string? EndDate, string? Description);

        private record ExtractedEducation(string? Institution, string? Degree, string? FieldOfStudy, string? Grade, string? StartDate, string? EndDate);

        private record ExtractedSkill(string? Name, string? Level);

        private record ExtractedLanguage(string? Name, string? Level);
    }
}
