using System.Text;
using Infrastructure.Files;

namespace Infrastructure.Tests.Files
{
    public class LocalFileStorageTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "autojob-tests", Guid.NewGuid().ToString("N"));
        private readonly LocalFileStorage _storage;

        public LocalFileStorageTests()
        {
            _storage = new LocalFileStorage(_root);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        [Fact]
        public async Task Saved_file_can_be_read_back_and_deleted()
        {
            var path = await _storage.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("Zeugnis")), "documents", ".pdf");

            Assert.StartsWith("documents/", path);
            Assert.EndsWith(".pdf", path);
            using (var reader = new StreamReader(_storage.OpenRead(path)))
                Assert.Equal("Zeugnis", await reader.ReadToEndAsync());

            _storage.Delete(path);
            Assert.False(File.Exists(_storage.GetFullPath(path)));
        }

        [Fact]
        public void Paths_outside_the_data_folder_are_refused()
        {
            Assert.Throws<ArgumentException>(() => _storage.GetFullPath("../../Windows/win.ini"));
        }
    }
}
