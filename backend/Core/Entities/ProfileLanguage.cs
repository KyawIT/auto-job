using Core.Enums;

namespace Core.Entities
{
    // A language the user speaks (not the language letters are written in).
    public class ProfileLanguage : EntityObject
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public LanguageLevel Level { get; set; }
    }
}
