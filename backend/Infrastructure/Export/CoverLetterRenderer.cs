using System.Globalization;
using Core.Enums;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Services.Ports;
using PdfDocument = QuestPDF.Fluent.Document;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace Infrastructure.Export
{
    // Lays out a cover letter. DIN 5008: sender line, recipient block, place/date right, bold subject, body.
    // International: sender block top right, date left under the recipient.
    public class CoverLetterRenderer : ICoverLetterRenderer
    {
        static CoverLetterRenderer()
        {
            // Free for open-source projects and small companies: https://www.questpdf.com/license/
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] RenderPdf(LetterContent letter)
        {
            var dateLine = DateLine(letter);
            return PdfDocument.Create(container => container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginLeft(25, Unit.Millimetre);
                page.MarginRight(20, Unit.Millimetre);
                page.MarginVertical(20, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontSize(11).LineHeight(1.3f));

                page.Content().Column(column =>
                {
                    var sender = column.Item();
                    if (letter.Layout == LetterLayout.International) sender = sender.AlignRight();
                    sender.Column(s => AddAddress(s, letter.Sender, bold: true));

                    column.Item().PaddingTop(30).Column(r => AddAddress(r, letter.Recipient, bold: false));

                    var date = column.Item().PaddingTop(30);
                    if (letter.Layout == LetterLayout.Din5008) date = date.AlignRight();
                    date.Text(dateLine);

                    column.Item().PaddingTop(20).Text(letter.Subject).Bold();

                    foreach (var paragraph in Paragraphs(letter.Body))
                        column.Item().PaddingTop(10).Text(paragraph);
                });
            })).GeneratePdf();
        }

        public byte[] RenderDocx(LetterContent letter)
        {
            using var stream = new MemoryStream();
            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var body = new Body();
                var senderAlign = letter.Layout == LetterLayout.International ? JustificationValues.Right : JustificationValues.Left;
                body.Append(Paragraph(letter.Sender.Name, bold: true, senderAlign));
                foreach (var line in letter.Sender.Lines)
                    body.Append(Paragraph(line, bold: false, senderAlign));

                body.Append(Paragraph("", false));
                body.Append(Paragraph(letter.Recipient.Name, false));
                foreach (var line in letter.Recipient.Lines)
                    body.Append(Paragraph(line, false));

                body.Append(Paragraph("", false));
                body.Append(Paragraph(DateLine(letter), false, letter.Layout == LetterLayout.Din5008 ? JustificationValues.Right : JustificationValues.Left));
                body.Append(Paragraph("", false));
                body.Append(Paragraph(letter.Subject, bold: true));
                foreach (var paragraph in Paragraphs(letter.Body))
                {
                    body.Append(Paragraph("", false));
                    foreach (var line in paragraph.Split('\n'))
                        body.Append(Paragraph(line, false));
                }

                document.AddMainDocumentPart().Document = new WordDocument(body);
            }
            return stream.ToArray();
        }

        private static void AddAddress(ColumnDescriptor column, LetterAddress address, bool bold)
        {
            var name = column.Item().Text(address.Name);
            if (bold) name.Bold();
            foreach (var line in address.Lines)
                column.Item().Text(line);
        }

        private static Paragraph Paragraph(string text, bool bold, JustificationValues? align = null)
        {
            var run = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text) { Space = SpaceProcessingModeValues.Preserve });
            if (bold)
                run.RunProperties = new RunProperties(new Bold());
            var paragraph = new Paragraph(run);
            if (align != null)
                paragraph.ParagraphProperties = new ParagraphProperties(new Justification { Val = align.Value });
            return paragraph;
        }

        // "Berlin, 9. Oktober 2026" / "Berlin, 9 October 2026"
        private static string DateLine(LetterContent letter)
        {
            var culture = Culture(letter.Language);
            var format = culture.TwoLetterISOLanguageName == "de" ? "d. MMMM yyyy" : "d MMMM yyyy";
            var date = letter.Date.ToString(format, culture);
            return string.IsNullOrWhiteSpace(letter.Place) ? date : $"{letter.Place}, {date}";
        }

        private static CultureInfo Culture(string language)
        {
            try
            {
                return CultureInfo.GetCultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        // Paragraphs are separated by blank lines; single line breaks stay inside a paragraph.
        private static IEnumerable<string> Paragraphs(string body) =>
            body.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
