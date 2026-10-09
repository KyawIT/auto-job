using Core.Enums;

namespace Services.Settings
{
    // Keys are never returned, only whether they are configured.
    public record SettingsDto(
        string DefaultLetterLanguage,
        string? NimModel,
        LetterLayout LetterLayout,
        int MaxParallelGenerations,
        string? DefaultSearchLocation,
        int? DefaultSearchRadiusKm,
        bool NimApiKeyConfigured,
        bool AdzunaConfigured);

    public record UpdateSettingsRequest(
        string DefaultLetterLanguage,
        string? NimModel,
        LetterLayout LetterLayout,
        int MaxParallelGenerations,
        string? DefaultSearchLocation = null,
        int? DefaultSearchRadiusKm = null);

    // Per key: null = leave unchanged, "" = remove, anything else = set.
    public record SetSecretsRequest(string? NimApiKey = null, string? AdzunaAppId = null, string? AdzunaAppKey = null);
}
