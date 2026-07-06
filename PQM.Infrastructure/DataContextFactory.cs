using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
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
            string connectionString = "Data Source=RIJUL_KASANA\\SQLEXPRESS;Initial Catalog=PQM;Integrated Security=True;TrustServerCertificate=True";
            return new DataContext(connectionString);
        }
    }
}
