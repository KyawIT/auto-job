using Core.Enums;
using Persistence.Tests;
using Services.Common;
using Services.Settings;
using Services.Tests.Support;

namespace Services.Tests.Settings
{
    public class SettingsServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();
        private readonly FakeSecretStore _secrets = new();

        public void Dispose() => _db.Dispose();

        private SettingsService Service() => new(_db.CreateUnitOfWork(), _secrets);

        [Fact]
        public async Task Fresh_settings_have_defaults_and_no_api_keys()
        {
            var settings = await Service().GetAsync();

            Assert.Equal("de", settings.DefaultLetterLanguage);
            Assert.Equal(LetterLayout.Din5008, settings.LetterLayout);
            Assert.Equal(2, settings.MaxParallelGenerations);
            Assert.False(settings.NimApiKeyConfigured);
            Assert.False(settings.AdzunaConfigured);
        }

        private static UpdateSettingsRequest English() =>
            new("en", "meta/llama-3.3-70b-instruct", LetterLayout.International, MaxParallelGenerations: 3, "Berlin", 25);

        [Fact]
        public async Task Updated_settings_are_kept()
        {
            await Service().UpdateAsync(English());

            var settings = await Service().GetAsync();

            Assert.Equal("en", settings.DefaultLetterLanguage);
            Assert.Equal("meta/llama-3.3-70b-instruct", settings.NimModel);
            Assert.Equal(3, settings.MaxParallelGenerations);
        }

        [Fact]
        public async Task Missing_language_or_out_of_range_parallelism_is_rejected()
        {
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => Service().UpdateAsync(English() with { DefaultLetterLanguage = "", MaxParallelGenerations = 0 }));

            Assert.Equal(["DefaultLetterLanguage", "MaxParallelGenerations"], ex.Errors.Keys.Order());
        }

        [Fact]
        public async Task Api_keys_can_be_set_and_cleared_independently()
        {
            await Service().SetSecretsAsync(new SetSecretsRequest(NimApiKey: "nvapi-123", AdzunaAppId: "id", AdzunaAppKey: "key"));
            await Service().SetSecretsAsync(new SetSecretsRequest(NimApiKey: null, AdzunaAppId: "", AdzunaAppKey: null));

            var settings = await Service().GetAsync();

            Assert.True(settings.NimApiKeyConfigured);    // null = leave unchanged
            Assert.False(settings.AdzunaConfigured);      // "" = clear
        }
    }
}
