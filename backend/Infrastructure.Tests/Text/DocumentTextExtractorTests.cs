using Core.Enums;
using Infrastructure.Tests.Support;
using Infrastructure.Text;

namespace Infrastructure.Tests.Text
{
    public class DocumentTextExtractorTests
    {
        private readonly DocumentTextExtractor _extractor = new();

        [Fact]
        public async Task Text_is_extracted_from_a_pdf()
        {
            var result = await _extractor.ExtractAsync(TestFiles.Pdf("Abschlusszeugnis Gesamtnote 1,7"), "zeugnis.pdf");

            Assert.Equal(ExtractionStatus.Done, result.Status);
            Assert.Contains("Gesamtnote 1,7", result.Text);
        }

        [Fact]
        public async Task Text_is_extracted_from_word_and_plain_text_files()
        {
            var docx = await _extractor.ExtractAsync(TestFiles.Docx("Arbeitszeugnis: stets zu unserer vollsten Zufriedenheit"), "zeugnis.docx");
            var txt = await _extractor.ExtractAsync(new MemoryStream("Zertifikat AZ-900"u8.ToArray()), "zertifikat.txt");

            Assert.Contains("vollsten Zufriedenheit", docx.Text);
            Assert.Equal("Zertifikat AZ-900", txt.Text);
        }

        [Fact]
        public async Task Scanned_pdf_without_text_layer_needs_ocr()
        {
            var result = await _extractor.ExtractAsync(TestFiles.PdfWithoutText(), "scan.pdf");

            Assert.Equal(ExtractionStatus.NotSupported, result.Status);
            Assert.Null(result.Text);
        }

        [Fact]
        public async Task Damaged_file_is_reported_as_failed()
        {
            var result = await _extractor.ExtractAsync(new MemoryStream("not really a pdf"u8.ToArray()), "broken.pdf");

            Assert.Equal(ExtractionStatus.Failed, result.Status);
        }

        [Fact]
        public async Task Images_are_not_supported()
        {
            var result = await _extractor.ExtractAsync(new MemoryStream([0x89, 0x50, 0x4E, 0x47]), "zeugnis.png");

            Assert.Equal(ExtractionStatus.NotSupported, result.Status);
        }
    }
}
