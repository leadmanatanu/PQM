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
        public DbSet<User> User { get; set; } = null!;
        public DbSet<Device> Device { get; set; } = null!;
        public DbSet<Parameter> Parameter { get; set; } = null!;
        public DbSet<ParameterValue> ParameterValue { get; set; } = null!;
        public DbSet<Event> Event { get; set; } = null!;

        public DataContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            modelBuilder.Entity<Parameter>().HasData(
                new Parameter { Id = 1, Name = "Accuracy Test Start", ObisCode = "0.128.162.0.128.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 2, Name = "Accuracy Test Stop", ObisCode = "0.128.162.1.128.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 3, Name = "Activity Calendar", ObisCode = "0.0.13.0.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 4, Name = "Apparent Power – kVA", ObisCode = "1.0.9.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 5, Name = "Association LN Meter Reader", ObisCode = "0.0.40.0.2.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 6, Name = "Available Billing Periods", ObisCode = "0.0.0.1.1.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 7, Name = "Billing Date", ObisCode = "0.0.0.1.2.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 8, Name = "Billing Period Script Table", ObisCode = "0.0.10.0.1.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 9, Name = "Capture Period of Daily Load Profile", ObisCode = "1.0.0.8.5.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 10, Name = "Category", ObisCode = "0.0.94.91.11.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 11, Name = "CMRI Reset", ObisCode = "0.128.154.128.128.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 12, Name = "CT Rating", ObisCode = "0.0.94.91.12.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 13, Name = "Cumulative Billing Count", ObisCode = "0.0.0.1.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 14, Name = "Cumulative Energy – kVAh (Export)", ObisCode = "1.0.10.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 15, Name = "Cumulative Energy (kVAh)", ObisCode = "1.0.9.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 16, Name = "Cumulative Energy (kvarh) – Lag", ObisCode = "1.0.5.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 17, Name = "Cumulative Energy (kvarh) – Lead", ObisCode = "1.0.8.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 18, Name = "Cumulative Energy (kWh)", ObisCode = "1.0.1.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 19, Name = "Cumulative Energy (kWh) – Export", ObisCode = "1.0.2.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 20, Name = "Cumulative Power Failure Duration", ObisCode = "0.0.94.91.8.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 21, Name = "Cumulative Programming Count", ObisCode = "0.0.96.2.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 22, Name = "Cumulative Tamper Count", ObisCode = "0.0.94.91.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 23, Name = "Current – IB", ObisCode = "1.0.71.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 24, Name = "Current – IR", ObisCode = "1.0.31.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 25, Name = "Current – IY", ObisCode = "1.0.51.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 26, Name = "Current Related Event Code", ObisCode = "0.0.96.11.1.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 27, Name = "Power Failure Related Event Code", ObisCode = "0.0.96.11.2.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 28, Name = "Profile Capture Period", ObisCode = "1.0.0.8.4.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 29, Name = "PT Power Fail Tamper Events", ObisCode = "1.0.128.7.90.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 30, Name = "Reset Type", ObisCode = "0.128.153.128.128.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 31, Name = "Signed Active Power – kW", ObisCode = "1.0.1.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 32, Name = "Signed Power Factor – B Phase", ObisCode = "1.0.73.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 33, Name = "Signed Power Factor – R Phase", ObisCode = "1.0.33.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 34, Name = "Signed Power Factor – Y Phase", ObisCode = "1.0.53.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 35, Name = "Signed Reactive Power – kvar", ObisCode = "1.0.3.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 36, Name = "Single Action Schedule for Billing Dates", ObisCode = "0.0.15.0.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 37, Name = "TCP/UDP Setup", ObisCode = "0.0.25.0.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 38, Name = "TCP/UDP Setup IPv4 Address", ObisCode = "0.0.25.1.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 39, Name = "TCP/UDP Setup MAC Address", ObisCode = "0.0.25.2.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 40, Name = "Transaction Related Event Code", ObisCode = "0.0.96.11.3.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 41, Name = "Voltage – VBN", ObisCode = "1.0.72.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 42, Name = "Voltage – VRN", ObisCode = "1.0.32.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) },
                new Parameter { Id = 43, Name = "Voltage – VYN", ObisCode = "1.0.52.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1) }
            );
        }
    }
}
