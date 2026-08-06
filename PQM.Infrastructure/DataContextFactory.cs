using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;
using System.Text.Json;

namespace PQM.Infrastructure
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            string connectionString = "Server=localhost;Database=PQM;Integrated Security=True;TrustServerCertificate=True;";

            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "../PQM.Server/appsettings.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connSection) &&
                            connSection.TryGetProperty("DefaultConnection", out var connStr))
                        {
                            connectionString = connStr.GetString() ?? connectionString;
                        }
                    }
                }
            }
            catch
            {
                // Fallback to default
            }

            return new DataContext(connectionString);
        }
    }
}
