using Core.Enums;

namespace Services.Documents
{
    // DisplayName null = use the file name without extension.
    public record UploadDocumentRequest(
        Stream Content,
        string FileName,
        DocumentType Type,
        string? DisplayName = null,
        bool AttachByDefault = false);

    public record DocumentDto(
        int Id,
        DocumentType Type,
        string DisplayName,
        string OriginalFileName,
        string MimeType,
        long SizeBytes,
        ExtractionStatus ExtractionStatus,
        bool AttachByDefault,
        DateTime CreatedAt);

    public record UpdateDocumentRequest(string DisplayName, DocumentType Type, bool AttachByDefault);

    public record DocumentFile(Stream Content, string FileName, string MimeType);
}
