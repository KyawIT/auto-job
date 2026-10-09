using Core.Enums;

namespace Core.Entities
{
    // Exactly one row. API keys are NOT stored here (Electron safeStorage).
    public class AppSettings : EntityObject
    {
        public string DefaultLetterLanguage { get; set; } = "de";
        public string? NimModel { get; set; }
        public LetterLayout LetterLayout { get; set; } = LetterLayout.Din5008;
        public int MaxParallelGenerations { get; set; } = 2;
        public string? DefaultSearchLocation { get; set; }
        public int? DefaultSearchRadiusKm { get; set; }
    }
}
