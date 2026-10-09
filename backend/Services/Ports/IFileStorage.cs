namespace Services.Ports
{
    // Files inside the app's data folder. Paths are relative ("documents/abc.pdf") so the folder can move.
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default);
        Stream OpenRead(string relativePath);
        void Delete(string relativePath);
        string GetFullPath(string relativePath);
    }
}
