using Core.Enums;
using DocumentFormat.OpenXml.Packaging;
using Infrastructure.Export;
using Services.Ports;
using UglyToad.PdfPig;

namespace Infrastructure.Tests.Export
{
    public class CoverLetterRendererTests
    {
        private readonly CoverLetterRenderer _renderer = new();

        private static LetterContent Letter(string language) => new(
            Sender: new LetterAddress("Anna Schmidt", ["Hauptstraße 1", "10115 Berlin", "anna@example.com"]),
            Recipient: new LetterAddress("Contoso AG", ["Frau Müller", "Berlin"]),
            Place: "Berlin",
            Date: new DateOnly(2026, 10, 9),
            Subject: "Bewerbung als Backend Developer",
            Body: "Sehr geehrte Frau Müller,\n\nmit großem Interesse habe ich Ihre Anzeige gelesen.\n\nMit freundlichen Grüßen\nAnna Schmidt",
            Language: language,
            Layout: LetterLayout.Din5008);

        private static string PdfText(byte[] pdf)
        {
            using var document = PdfDocument.Open(pdf);
            return string.Join(" ", document.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))));
        }

        [Fact]
        public void Pdf_contains_addresses_localized_date_subject_and_every_paragraph()
        {
            var text = PdfText(_renderer.RenderPdf(Letter("de")));

            Assert.Contains("Anna Schmidt", text);
            Assert.Contains("Hauptstraße 1", text);
            Assert.Contains("Contoso AG", text);
            Assert.Contains("Berlin, 9. Oktober 2026", text);
            Assert.Contains("Bewerbung als Backend Developer", text);
            Assert.Contains("mit großem Interesse habe ich Ihre Anzeige gelesen.", text);
            Assert.Contains("Mit freundlichen Grüßen", text);
        }

        [Fact]
        public void English_letter_uses_an_english_date()
        {
            var text = PdfText(_renderer.RenderPdf(Letter("en")));

            Assert.Contains("9 October 2026", text);
        }

        [Fact]
        public void Docx_contains_the_same_letter()
        {
            using var stream = new MemoryStream(_renderer.RenderDocx(Letter("de")));
            using var document = WordprocessingDocument.Open(stream, isEditable: false);
            var text = document.MainDocumentPart!.Document!.Body!.InnerText;

            Assert.Contains("Contoso AG", text);
            Assert.Contains("Berlin, 9. Oktober 2026", text);
            Assert.Contains("mit großem Interesse habe ich Ihre Anzeige gelesen.", text);
        }
    }
}
