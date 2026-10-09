using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Documents;

namespace api.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentService _documents;

        public DocumentsController(DocumentService documents)
        {
            _documents = documents;
        }

        [HttpGet]
        public Task<IReadOnlyList<DocumentDto>> List(CancellationToken ct) => _documents.ListAsync(ct);

        [HttpPost]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadForm form, CancellationToken ct)
        {
            await using var content = form.File.OpenReadStream();
            var document = await _documents.UploadAsync(new UploadDocumentRequest(
                content, form.File.FileName, form.Type, form.DisplayName, form.AttachByDefault), ct);
            return Created($"/api/documents/{document.Id}", document);
        }

        [HttpPut("{id:int}")]
        public Task<DocumentDto> Update(int id, UpdateDocumentRequest request, CancellationToken ct) =>
            _documents.UpdateAsync(id, request, ct);

        [HttpGet("{id:int}/file")]
        public async Task<IActionResult> Download(int id, CancellationToken ct)
        {
            var file = await _documents.OpenAsync(id, ct);
            return File(file.Content, file.MimeType, file.FileName);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _documents.DeleteAsync(id, ct);
            return NoContent();
        }

        public class UploadForm
        {
            public IFormFile File { get; set; } = null!;
            public DocumentType Type { get; set; }
            public string? DisplayName { get; set; }
            public bool AttachByDefault { get; set; }
        }
    }
}
