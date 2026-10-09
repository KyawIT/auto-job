using System.Text;
using Core.Enums;
using Persistence.Tests;
using Services.Common;
using Services.Documents;
using Services.Profiles;
using Services.Tests.Support;

namespace Services.Tests.Documents
{
    public class DocumentServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();
        private readonly FakeFileStorage _files = new();
        private readonly FakeTextExtractor _extractor = new();

        public DocumentServiceTests()
        {
            new ProfileService(_db.CreateUnitOfWork()).SaveAsync(new SaveProfileRequest("Anna", "Schmidt", "anna@example.com")).GetAwaiter().GetResult();
        }

        public void Dispose() => _db.Dispose();

        private DocumentService Service() => new(_db.CreateUnitOfWork(), _files, _extractor);

        private static UploadDocumentRequest Upload(string fileName, string content, DocumentType type = DocumentType.DegreeCertificate) =>
            new(new MemoryStream(Encoding.UTF8.GetBytes(content)), fileName, type, DisplayName: null, AttachByDefault: true);

        [Fact]
        public async Task Uploaded_document_is_stored_with_its_text()
        {
            var uploaded = await Service().UploadAsync(Upload("Bachelorzeugnis.pdf", "Gesamtnote 1,7"));

            var listed = Assert.Single(await Service().ListAsync());
            Assert.Equal(uploaded.Id, listed.Id);
            Assert.Equal("Bachelorzeugnis", listed.DisplayName);   // defaults to the file name
            Assert.Equal(ExtractionStatus.Done, listed.ExtractionStatus);

            var file = await Service().OpenAsync(uploaded.Id);
            Assert.Equal("Bachelorzeugnis.pdf", file.FileName);
            Assert.Equal("Gesamtnote 1,7", await new StreamReader(file.Content).ReadToEndAsync());
        }

        [Fact]
        public async Task Mime_type_follows_the_file_extension()
        {
            await Service().UploadAsync(Upload("Zeugnis.PDF", "x"));
            await Service().UploadAsync(Upload("cv.docx", "x", DocumentType.Cv));

            Assert.Equal(
                ["application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
                (await Service().ListAsync()).Select(d => d.MimeType).Order());
        }

        [Fact]
        public async Task Document_details_can_be_changed()
        {
            var uploaded = await Service().UploadAsync(Upload("scan.pdf", "x"));

            await Service().UpdateAsync(uploaded.Id, new UpdateDocumentRequest("AWS Zertifikat", DocumentType.Certification, AttachByDefault: false));

            var listed = Assert.Single(await Service().ListAsync());
            Assert.Equal("AWS Zertifikat", listed.DisplayName);
            Assert.Equal(DocumentType.Certification, listed.Type);
            Assert.False(listed.AttachByDefault);
        }

        [Fact]
        public async Task Unsupported_file_type_is_rejected_and_nothing_is_stored()
        {
            var ex = await Assert.ThrowsAsync<ValidationException>(() => Service().UploadAsync(Upload("setup.exe", "MZ")));

            Assert.Equal(["FileName"], ex.Errors.Keys);
            Assert.Empty(_files.Files);
        }

        [Fact]
        public async Task Deleting_a_document_removes_its_file()
        {
            var uploaded = await Service().UploadAsync(Upload("cv.pdf", "CV", DocumentType.Cv));

            await Service().DeleteAsync(uploaded.Id);

            Assert.Empty(await Service().ListAsync());
            Assert.Empty(_files.Files);
        }

        [Fact]
        public async Task Document_in_use_by_an_application_keeps_its_file()
        {
            var uploaded = await Service().UploadAsync(Upload("cv.pdf", "CV", DocumentType.Cv));
            var uow = _db.CreateUnitOfWork();
            var application = new Core.Entities.Application
            {
                JobPosting = new Core.Entities.JobPosting { Title = "Dev", CompanyName = "ACME", Description = "C#" },
            };
            application.Attachments.Add(new Core.Entities.ApplicationDocument { DocumentId = uploaded.Id });
            await uow.Applications.AddAsync(application);
            await uow.SaveChangesAsync();

            await Assert.ThrowsAsync<Core.Exceptions.ReferenceConstraintException>(() => Service().DeleteAsync(uploaded.Id));
            Assert.Single(_files.Files);
        }
    }
}
