using Core.Enums;
using Services.Ports;

namespace Services.Tests.Support
{
    // Returns the file's bytes as text, unless a fixed result is set.
    public class FakeTextExtractor : ITextExtractor
    {
        public TextExtractionResult? Result { get; set; }

        public async Task<TextExtractionResult> ExtractAsync(Stream content, string fileName, CancellationToken ct = default)
        {
            if (Result != null) return Result;
            using var reader = new StreamReader(content);
            return new TextExtractionResult(ExtractionStatus.Done, await reader.ReadToEndAsync(ct));
        }
    }
}
