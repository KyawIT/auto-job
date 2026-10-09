using Core.Enums;

namespace Services.Ports
{
    public record TextExtractionResult(ExtractionStatus Status, string? Text)
    {
        public static TextExtractionResult NotSupported { get; } = new(ExtractionStatus.NotSupported, null);
    }

    // Reads the text of an uploaded document so it can be used as AI context.
    public interface ITextExtractor
    {
        Task<TextExtractionResult> ExtractAsync(Stream content, string fileName, CancellationToken ct = default);
    }
}
