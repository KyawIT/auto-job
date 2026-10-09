using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Applications;
using Services.Generation;

namespace api.Controllers
{
    // A single generated text version (cover letter, email subject or body).
    [ApiController]
    [Route("api/texts")]
    public class TextsController : ControllerBase
    {
        private readonly GenerationService _generation;

        public TextsController(GenerationService generation)
        {
            _generation = generation;
        }

        [HttpPut("{id:int}")]
        public Task<GeneratedTextDto> Edit(int id, EditTextRequest request, CancellationToken ct) =>
            _generation.EditTextAsync(id, request.Content, ct);

        // Makes this version the one used for export and email.
        [HttpPost("{id:int}/select")]
        public Task<ApplicationDetailsDto> Select(int id, CancellationToken ct) => _generation.SelectVersionAsync(id, ct);

        public record EditTextRequest(string Content);
    }
}
