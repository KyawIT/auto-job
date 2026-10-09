using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Generation;
using Services.Profiles;

namespace api.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly ProfileService _profiles;
        private readonly ProfileImportService _import;

        public ProfileController(ProfileService profiles, ProfileImportService import)
        {
            _profiles = profiles;
            _import = import;
        }

        [HttpGet]
        public async Task<ActionResult<ProfileDto>> Get(CancellationToken ct) =>
            await _profiles.GetAsync(ct) is { } profile ? profile : NotFound();

        [HttpPut]
        public Task<ProfileDto> Save(SaveProfileRequest request, CancellationToken ct) =>
            _profiles.SaveAsync(request, ct);

        // Fills empty fields and adds missing entries from an uploaded CV (uses the AI).
        [HttpPost("import-cv")]
        public Task<ProfileDto> ImportCv(ImportCvRequest request, CancellationToken ct) =>
            _import.ImportFromDocumentAsync(request.DocumentId, ct);

        public record ImportCvRequest(int DocumentId);

        // Work experience
        [HttpPost("work-experiences")]
        public Task<WorkExperienceDto> AddWorkExperience(WorkExperienceRequest request, CancellationToken ct) =>
            _profiles.AddWorkExperienceAsync(request, ct);

        [HttpPut("work-experiences/{id:int}")]
        public Task<WorkExperienceDto> UpdateWorkExperience(int id, WorkExperienceRequest request, CancellationToken ct) =>
            _profiles.UpdateWorkExperienceAsync(id, request, ct);

        [HttpDelete("work-experiences/{id:int}")]
        public async Task<IActionResult> DeleteWorkExperience(int id, CancellationToken ct)
        {
            await _profiles.DeleteWorkExperienceAsync(id, ct);
            return NoContent();
        }

        // Education
        [HttpPost("educations")]
        public Task<EducationDto> AddEducation(EducationRequest request, CancellationToken ct) =>
            _profiles.AddEducationAsync(request, ct);

        [HttpPut("educations/{id:int}")]
        public Task<EducationDto> UpdateEducation(int id, EducationRequest request, CancellationToken ct) =>
            _profiles.UpdateEducationAsync(id, request, ct);

        [HttpDelete("educations/{id:int}")]
        public async Task<IActionResult> DeleteEducation(int id, CancellationToken ct)
        {
            await _profiles.DeleteEducationAsync(id, ct);
            return NoContent();
        }

        // Skills
        [HttpPost("skills")]
        public Task<SkillDto> AddSkill(SkillRequest request, CancellationToken ct) =>
            _profiles.AddSkillAsync(request, ct);

        [HttpPut("skills/{id:int}")]
        public Task<SkillDto> UpdateSkill(int id, SkillRequest request, CancellationToken ct) =>
            _profiles.UpdateSkillAsync(id, request, ct);

        [HttpDelete("skills/{id:int}")]
        public async Task<IActionResult> DeleteSkill(int id, CancellationToken ct)
        {
            await _profiles.DeleteSkillAsync(id, ct);
            return NoContent();
        }

        // Languages
        [HttpPost("languages")]
        public Task<LanguageDto> AddLanguage(LanguageRequest request, CancellationToken ct) =>
            _profiles.AddLanguageAsync(request, ct);

        [HttpPut("languages/{id:int}")]
        public Task<LanguageDto> UpdateLanguage(int id, LanguageRequest request, CancellationToken ct) =>
            _profiles.UpdateLanguageAsync(id, request, ct);

        [HttpDelete("languages/{id:int}")]
        public async Task<IActionResult> DeleteLanguage(int id, CancellationToken ct)
        {
            await _profiles.DeleteLanguageAsync(id, ct);
            return NoContent();
        }
    }
}
