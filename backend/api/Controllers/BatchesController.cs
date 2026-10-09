using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Generation;

namespace api.Controllers
{
    // Generate letters for many jobs at once. Work happens in the background; poll GET for progress.
    [ApiController]
    [Route("api/batches")]
    public class BatchesController : ControllerBase
    {
        private readonly BatchService _batches;

        public BatchesController(BatchService batches)
        {
            _batches = batches;
        }

        [HttpPost]
        public async Task<ActionResult<BatchDto>> Start(StartBatchRequest request, CancellationToken ct)
        {
            var batch = await _batches.StartAsync(request, ct);
            return Created($"/api/batches/{batch.Id}", batch);
        }

        [HttpGet("{id:int}")]
        public Task<BatchDto> Get(int id, CancellationToken ct) => _batches.GetAsync(id, ct);

        [HttpPost("{id:int}/cancel")]
        public Task<BatchDto> Cancel(int id, CancellationToken ct) => _batches.CancelAsync(id, ct);

        [HttpPost("{id:int}/retry")]
        public Task<BatchDto> RetryFailed(int id, CancellationToken ct) => _batches.RetryFailedAsync(id, ct);
    }
}
