using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using PdfDocument = QuestPDF.Fluent.Document;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;
using QuestPDF.Infrastructure;

namespace Infrastructure.Tests.Support
{
    // Real files built in memory, so extraction is tested against actual PDF/DOCX formats.
    public static class TestFiles
    {
        static TestFiles()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static MemoryStream Pdf(string text) => new(PdfDocument.Create(c => c.Page(p => p.Content().Text(text))).GeneratePdf());

        // A page without any text layer, like a scanned Zeugnis.
        public static MemoryStream PdfWithoutText() =>
            new(PdfDocument.Create(c => c.Page(p => p.Content().Height(100).Width(100).Background("#cccccc"))).GeneratePdf());

        public static MemoryStream Docx(string text)
        {
            var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                doc.AddMainDocumentPart().Document = new DocumentFormat.OpenXml.Wordprocessing.Document(
                    new Body(new Paragraph(new Run(new WordText(text)))));
            }
            stream.Position = 0;
            return stream;
        }
    }
}
