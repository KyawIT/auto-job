using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Jobs;

namespace api.Controllers
{
    [ApiController]
    [Route("api/saved-searches")]
    public class SavedSearchesController : ControllerBase
    {
        private readonly JobSearchService _search;

        public SavedSearchesController(JobSearchService search)
        {
            _search = search;
        }

        [HttpGet]
        public Task<IReadOnlyList<SavedSearchDto>> List(CancellationToken ct) => _search.ListSavedSearchesAsync(ct);

        [HttpPost]
        public async Task<ActionResult<SavedSearchDto>> Create(SaveSearchRequest request, CancellationToken ct)
        {
            var saved = await _search.CreateSavedSearchAsync(request, ct);
            return Created($"/api/saved-searches/{saved.Id}", saved);
        }

        [HttpPut("{id:int}")]
        public Task<SavedSearchDto> Update(int id, SaveSearchRequest request, CancellationToken ct) =>
            _search.UpdateSavedSearchAsync(id, request, ct);

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _search.DeleteSavedSearchAsync(id, ct);
            return NoContent();
        }
    }
}
