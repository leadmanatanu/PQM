using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Parameter = PQM.Core.Entities.Parameter;

namespace PQM.Infrastructure
{
    public class DataContext : DbContext
    {
        private readonly string _connectionString;
        public DbSet<Device> Device { get; set; } = null!;
        public DbSet<Parameter> Parameter { get; set; } = null!;
        public DbSet<DeviceParameterMapping> DeviceParameterMapping { get; set; } = null!;
        public DbSet<DeviceLog> DeviceLog { get; set; } = null!;

        public DataContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            //var configuration = new ConfigurationBuilder()
            //    .AddJsonFile("AppSettings.json")
            //    .Build();

            //string ConnectionString = configuration.GetSection("ConnectionString").Value!;

            //optionsBuilder.UseSqlServer(ConnectionString);

            optionsBuilder.UseSqlServer(_connectionString);
        }
    }
}
