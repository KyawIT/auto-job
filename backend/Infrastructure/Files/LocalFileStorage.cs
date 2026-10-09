using Services.Ports;

namespace Infrastructure.Files
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _root;

        public LocalFileStorage(string root)
        {
            _root = Path.GetFullPath(root);
        }

        public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
        {
            var relativePath = $"{folder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullPath = GetFullPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using var file = File.Create(fullPath);
            await content.CopyToAsync(file, ct);
            return relativePath;
        }

        public Stream OpenRead(string relativePath) => File.OpenRead(GetFullPath(relativePath));

        public void Delete(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        // Stored paths come from the database; never let one point outside the data folder.
        public string GetFullPath(string relativePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
            if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Path '{relativePath}' is outside the data folder.", nameof(relativePath));
            return fullPath;
        }
    }
}
