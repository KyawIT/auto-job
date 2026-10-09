using System.Text;
using Core.Enums;
using Services.Ports;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace Infrastructure.Text
{
    public class DocumentTextExtractor : ITextExtractor
    {
        public async Task<TextExtractionResult> ExtractAsync(Stream content, string fileName, CancellationToken ct = default)
        {
            // PdfPig needs a seekable stream; uploads aren't always.
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            string? text;
            try
            {
                text = Path.GetExtension(fileName).ToLowerInvariant() switch
                {
                    ".pdf" => ReadPdf(buffer),
                    ".docx" => ReadDocx(buffer),
                    ".txt" => Encoding.UTF8.GetString(buffer.ToArray()).Trim(),
                    _ => null,
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Damaged or password-protected file: keep the upload, just without text.
                return new TextExtractionResult(ExtractionStatus.Failed, null);
            }

            // No text at all (e.g. a scanned PDF) would need OCR, which isn't available yet.
            return string.IsNullOrWhiteSpace(text)
                ? TextExtractionResult.NotSupported
                : new TextExtractionResult(ExtractionStatus.Done, text);
        }

        private static string ReadPdf(Stream pdf)
        {
            using var document = PdfDocument.Open(pdf);
            var text = new StringBuilder();
            foreach (var page in document.GetPages())
                text.AppendLine(page.Text);
            return text.ToString().Trim();
        }

        private static string ReadDocx(Stream docx)
        {
            using var document = WordprocessingDocument.Open(docx, isEditable: false);
            var paragraphs = document.MainDocumentPart?.Document?.Body?.Descendants<Paragraph>() ?? [];
            return string.Join(Environment.NewLine, paragraphs.Select(p => p.InnerText)).Trim();
        }
    }
}
