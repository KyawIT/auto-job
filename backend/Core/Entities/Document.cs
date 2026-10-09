using Core.Enums;

namespace Core.Entities
{
    // Uploaded file. The file itself lives in the data folder; only its path is stored here.
    public class Document : EntityObject
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; } = null!;

        public DocumentType Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFilePath { get; set; } = string.Empty;   // relative to the data folder
        public string MimeType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }

        // Extracted once (PDF text / OCR) and reused as AI context.
        public string? ExtractedText { get; set; }
        public ExtractionStatus ExtractionStatus { get; set; } = ExtractionStatus.Pending;

        public bool AttachByDefault { get; set; }

        public ICollection<ApplicationDocument> Applications { get; set; } = new List<ApplicationDocument>();
    }
}
