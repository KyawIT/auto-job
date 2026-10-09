using Core.Contracts;
using Core.Entities;
using Services.Common;
using Services.Ports;

namespace Services.Documents
{
    // Uploaded documents (CV, Zeugnisse, certificates): file on disk, text extracted once for AI context.
    public class DocumentService
    {
        private const string Folder = "documents";
        private const long MaxSizeBytes = 20 * 1024 * 1024;
        // The extension decides the MIME type; whatever the client claims is ignored.
        private static readonly Dictionary<string, string> MimeTypes = new()
        {
            [".pdf"] = "application/pdf",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".txt"] = "text/plain",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
        };

        private readonly IUnitOfWork _uow;
        private readonly IFileStorage _files;
        private readonly ITextExtractor _extractor;

        public DocumentService(IUnitOfWork uow, IFileStorage files, ITextExtractor extractor)
        {
            _uow = uow;
            _files = files;
            _extractor = extractor;
        }

        public async Task<DocumentDto> UploadAsync(UploadDocumentRequest request, CancellationToken ct = default)
        {
            var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
            new Validator()
                .Check(MimeTypes.ContainsKey(extension), nameof(request.FileName), $"Allowed file types: {string.Join(", ", MimeTypes.Keys)}.")
                .ThrowIfInvalid();
            var profile = await _uow.Profiles.GetAsync(ct) ?? throw new NotFoundException("Profile");

            // Buffered once: the content is both stored and read for text extraction.
            using var buffer = new MemoryStream();
            await request.Content.CopyToAsync(buffer, ct);
            var size = buffer.Length;
            new Validator().Check(size <= MaxSizeBytes, "Content", "File must not be larger than 20 MB.").ThrowIfInvalid();

            buffer.Position = 0;
            var storedPath = await _files.SaveAsync(buffer, Folder, extension, ct);
            buffer.Position = 0;
            var extraction = await _extractor.ExtractAsync(buffer, request.FileName, ct);   // may consume/close the stream

            var document = new Document
            {
                Profile = profile,
                Type = request.Type,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? Path.GetFileNameWithoutExtension(request.FileName) : request.DisplayName,
                OriginalFileName = Path.GetFileName(request.FileName),
                StoredFilePath = storedPath,
                MimeType = MimeTypes[extension],
                SizeBytes = size,
                ExtractedText = extraction.Text,
                ExtractionStatus = extraction.Status,
                AttachByDefault = request.AttachByDefault,
            };
            await _uow.Documents.AddAsync(document, ct);
            await _uow.SaveChangesAsync(ct);
            return document.ToDto();
        }

        public async Task<IReadOnlyList<DocumentDto>> ListAsync(CancellationToken ct = default) =>
            (await _uow.Documents.GetAllAsync(ct)).OrderBy(d => d.Type).ThenBy(d => d.DisplayName).Select(d => d.ToDto()).ToList();

        public async Task<DocumentFile> OpenAsync(int id, CancellationToken ct = default)
        {
            var document = await RequireAsync(id, ct);
            return new DocumentFile(_files.OpenRead(document.StoredFilePath), document.OriginalFileName, document.MimeType);
        }

        public async Task<DocumentDto> UpdateAsync(int id, UpdateDocumentRequest request, CancellationToken ct = default)
        {
            new Validator().Required(nameof(request.DisplayName), request.DisplayName).ThrowIfInvalid();
            var document = await RequireAsync(id, ct);
            document.DisplayName = request.DisplayName;
            document.Type = request.Type;
            document.AttachByDefault = request.AttachByDefault;
            await _uow.SaveChangesAsync(ct);
            return document.ToDto();
        }

        // Row first: if the document is still attached somewhere the delete fails and the file stays.
        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var document = await RequireAsync(id, ct);
            _uow.Documents.Remove(document);
            await _uow.SaveChangesAsync(ct);
            _files.Delete(document.StoredFilePath);
        }

        private async Task<Document> RequireAsync(int id, CancellationToken ct) =>
            await _uow.Documents.GetByIdAsync(id, ct) ?? throw new NotFoundException("Document", id);
    }

    internal static class DocumentMappings
    {
        public static DocumentDto ToDto(this Document d) => new(
            d.Id, d.Type, d.DisplayName, d.OriginalFileName, d.MimeType, d.SizeBytes, d.ExtractionStatus, d.AttachByDefault, d.CreatedAt);
    }
}
