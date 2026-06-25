using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace PQM.Infrastructure
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();

            // For design time, you can provide a placeholder or real connection string
            // We just need the structure to create migrations.
            string connectionString = "Data Source=DESKTOP-5S6S7IN\\SQLEXPRESS;Initial Catalog=PQM;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=\"SQL Server Management Studio\";Command Timeout=0";
;

            return new DataContext(connectionString);
        }
    }
}
