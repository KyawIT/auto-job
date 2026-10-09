using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests
{
    // The real backend in memory, with its own empty data folder (SQLite file, logs, documents).
    public class ApiFactory : WebApplicationFactory<api.Program>
    {
        public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), "autojob-tests", Guid.NewGuid().ToString("N"));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Sidecar:DataDirectory", DataDirectory);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                try { Directory.Delete(DataDirectory, recursive: true); } catch (IOException) { }
            }
        }
    }
}
