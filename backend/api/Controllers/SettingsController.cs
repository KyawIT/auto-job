using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Settings;

namespace api.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly SettingsService _settings;

        public SettingsController(SettingsService settings)
        {
            _settings = settings;
        }

        [HttpGet]
        public Task<SettingsDto> Get(CancellationToken ct) => _settings.GetAsync(ct);

        [HttpPut]
        public Task<SettingsDto> Update(UpdateSettingsRequest request, CancellationToken ct) => _settings.UpdateAsync(request, ct);

        // Called by Electron on start (keys from safeStorage) and whenever the user changes a key.
        [HttpPut("secrets")]
        public Task<SettingsDto> SetSecrets(SetSecretsRequest request, CancellationToken ct) => _settings.SetSecretsAsync(request, ct);
    }
}
