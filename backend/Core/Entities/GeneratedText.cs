using Core.Enums;

namespace Core.Entities
{
    // One version of an AI-generated text. Regenerating adds a new row instead of overwriting.
    public class GeneratedText : EntityObject
    {
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public GeneratedTextKind Kind { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Model { get; set; }
        public bool IsEdited { get; set; }    // user changed it after generation
        public bool IsCurrent { get; set; }   // the version used for export/email
    }
}
