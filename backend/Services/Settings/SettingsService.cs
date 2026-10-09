using Core.Contracts;
using Core.Entities;
using Services.Common;
using Services.Ports;

namespace Services.Settings
{
    public class SettingsService
    {
        private readonly IUnitOfWork _uow;
        private readonly ISecretStore _secrets;

        public SettingsService(IUnitOfWork uow, ISecretStore secrets)
        {
            _uow = uow;
            _secrets = secrets;
        }

        public async Task<SettingsDto> GetAsync(CancellationToken ct = default) =>
            ToDto(await _uow.AppSettings.GetAsync(ct));

        public async Task<SettingsDto> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default)
        {
            new Validator()
                .Required(nameof(request.DefaultLetterLanguage), request.DefaultLetterLanguage)
                .Check(request.MaxParallelGenerations is >= 1 and <= MaxParallelLimit, nameof(request.MaxParallelGenerations), $"Must be between 1 and {MaxParallelLimit}.")
                .Check(request.DefaultSearchRadiusKm is null or >= 0, nameof(request.DefaultSearchRadiusKm), "Must not be negative.")
                .ThrowIfInvalid();

            var settings = await _uow.AppSettings.GetAsync(ct);
            settings.DefaultLetterLanguage = request.DefaultLetterLanguage.Trim().ToLowerInvariant();
            settings.NimModel = request.NimModel;
            settings.LetterLayout = request.LetterLayout;
            settings.MaxParallelGenerations = request.MaxParallelGenerations;
            settings.DefaultSearchLocation = request.DefaultSearchLocation;
            settings.DefaultSearchRadiusKm = request.DefaultSearchRadiusKm;
            await _uow.SaveChangesAsync(ct);
            return ToDto(settings);
        }

        public Task<SettingsDto> SetSecretsAsync(SetSecretsRequest request, CancellationToken ct = default)
        {
            SetIfProvided(SecretName.NimApiKey, request.NimApiKey);
            SetIfProvided(SecretName.AdzunaAppId, request.AdzunaAppId);
            SetIfProvided(SecretName.AdzunaAppKey, request.AdzunaAppKey);
            return GetAsync(ct);
        }

        private void SetIfProvided(SecretName name, string? value)
        {
            if (value != null)
                _secrets.Set(name, value.Trim());
        }

        // Free NIM keys are rate limited; more parallel requests only cause 429s.
        private const int MaxParallelLimit = 5;

        private SettingsDto ToDto(AppSettings s) => new(
            s.DefaultLetterLanguage, s.NimModel, s.LetterLayout, s.MaxParallelGenerations,
            s.DefaultSearchLocation, s.DefaultSearchRadiusKm,
            NimApiKeyConfigured: _secrets.Get(SecretName.NimApiKey) != null,
            AdzunaConfigured: _secrets.Get(SecretName.AdzunaAppId) != null && _secrets.Get(SecretName.AdzunaAppKey) != null);
    }
}
