using Services.Ports;

namespace Services.Tests.Support
{
    public class FakeFileStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = new();

        public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            var path = $"{folder}/{Files.Count + 1}{extension}";
            Files[path] = buffer.ToArray();
            return path;
        }

        public Stream OpenRead(string relativePath) => new MemoryStream(Files[relativePath]);

        public void Delete(string relativePath) => Files.Remove(relativePath);

        public string GetFullPath(string relativePath) => Path.Combine("C:/data", relativePath);
    }
}
