using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Services.Applications;
using Services.Export;
using Services.Generation;

namespace api.Controllers
{
    [ApiController]
    [Route("api/applications")]
    public class ApplicationsController : ControllerBase
    {
        private readonly ApplicationService _applications;
        private readonly GenerationService _generation;
        private readonly ExportService _export;

        public ApplicationsController(ApplicationService applications, GenerationService generation, ExportService export)
        {
            _applications = applications;
            _generation = generation;
            _export = export;
        }

        [HttpGet]
        public Task<IReadOnlyList<ApplicationSummaryDto>> List([FromQuery] ApplicationStatus? status, CancellationToken ct) =>
            _applications.ListAsync(status, ct);

        // Idempotent: returns the existing application if the job already has one.
        [HttpPost]
        public async Task<ActionResult<ApplicationDetailsDto>> Start(StartRequest request, CancellationToken ct)
        {
            var application = await _applications.StartAsync(request.JobPostingId, new StartApplicationRequest(request.Language), ct);
            return Created($"/api/applications/{application.Id}", application);
        }

        [HttpGet("{id:int}")]
        public Task<ApplicationDetailsDto> Get(int id, CancellationToken ct) => _applications.GetAsync(id, ct);

        [HttpPut("{id:int}")]
        public Task<ApplicationDetailsDto> Update(int id, UpdateApplicationRequest request, CancellationToken ct) =>
            _applications.UpdateAsync(id, request, ct);

        [HttpPut("{id:int}/status")]
        public Task<ApplicationDetailsDto> ChangeStatus(int id, ChangeStatusRequest request, CancellationToken ct) =>
            _applications.ChangeStatusAsync(id, request, ct);

        [HttpPut("{id:int}/attachments")]
        public Task<ApplicationDetailsDto> SetAttachments(int id, AttachmentsRequest request, CancellationToken ct) =>
            _applications.SetAttachmentsAsync(id, request.DocumentIds, ct);

        [HttpPost("{id:int}/notes")]
        public Task<ApplicationDetailsDto> AddNote(int id, NoteRequest request, CancellationToken ct) =>
            _applications.AddNoteAsync(id, request.Note, ct);

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _applications.DeleteAsync(id, ct);
            return NoContent();
        }

        [HttpPost("{id:int}/generate")]
        public Task<ApplicationDetailsDto> Generate(int id, CancellationToken ct) => _generation.GenerateAsync(id, ct);

        [HttpGet("{id:int}/texts")]
        public Task<IReadOnlyList<GeneratedTextDto>> Versions(int id, [FromQuery] GeneratedTextKind kind, CancellationToken ct) =>
            _generation.ListVersionsAsync(id, kind, ct);

        [HttpGet("{id:int}/export")]
        public async Task<IActionResult> Export(int id, [FromQuery] ExportFormat format, CancellationToken ct)
        {
            var file = await _export.ExportLetterAsync(id, format, ct);
            return File(file.Content, file.MimeType, file.FileName);
        }

        // Writes an .eml draft into the data folder; Electron opens FullPath with the mail client.
        [HttpPost("{id:int}/email-draft")]
        public Task<EmailDraftDto> CreateEmailDraft(int id, CancellationToken ct) => _export.CreateEmailDraftAsync(id, ct);

        public record StartRequest(int JobPostingId, string? Language = null);

        public record AttachmentsRequest(IReadOnlyList<int> DocumentIds);

        public record NoteRequest(string Note);
    }
}
