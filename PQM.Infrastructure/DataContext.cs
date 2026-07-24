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
        public DbSet<DeviceConnectionEvent> DeviceConnectionEvents { get; set; } = null!;

        public DbSet<Profile> Profiles { get; set; } = null!;
        public DbSet<ReadingSession> ReadingSessions { get; set; } = null!;
        public DbSet<ReadingValue> ReadingValues { get; set; } = null!;
        public DbSet<DeviceProfileSyncState> DeviceProfileSyncStates { get; set; } = null!;
        public DbSet<DeviceLatestReading> DeviceLatestReadings { get; set; } = null!;
        public DbSet<DeviceEvent> DeviceEvents { get; set; } = null!;
        public DbSet<DeviceSyncHistory> DeviceSyncHistories { get; set; } = null!;
        public DbSet<DeviceSyncSchedule> DeviceSyncSchedules { get; set; } = null!;

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
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DeviceSyncSchedule>(entity =>
            {
                entity.ToTable("DeviceSyncSchedule");
                entity.HasKey(e => e.DeviceId);
            });

            modelBuilder.Entity<DeviceSyncHistory>(entity =>
            {
                entity.ToTable("DeviceSyncHistory");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Device>(entity =>
            {
                entity.ToTable("Devices");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IP).HasColumnName("IP");
                entity.Property(e => e.PORT).HasColumnName("PORT");
            });

            modelBuilder.Entity<Profile>(entity =>
            {
                entity.ToTable("Profiles");
                entity.HasKey(e => e.ProfileId);
            });

            modelBuilder.Entity<Parameter>(entity =>
            {
                entity.ToTable("Parameters");
                entity.HasKey(e => e.Id);

                entity.HasOne(d => d.Profile)
                    .WithMany(p => p.Parameters)
                    .HasForeignKey(d => d.ProfileId);
            });

            modelBuilder.Entity<ReadingSession>(entity =>
            {
                entity.ToTable("ReadingSessions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntryTimestampUtc).HasColumnName("EntryTimestampUtc");

                entity.HasOne(d => d.Device)
                    .WithMany()
                    .HasForeignKey(d => d.DeviceId);

                entity.HasOne(d => d.Profile)
                    .WithMany()
                    .HasForeignKey(d => d.ProfileId);

                // Add unique filtered index (prevent duplicates while allowing nulls)
                entity.HasIndex(e => new { e.DeviceId, e.ProfileId, e.EntryTimestampUtc })
                    .HasDatabaseName("IX_ReadingSessions_Device_Profile_Timestamp")
                    .IsUnique()
                    .HasFilter("[EntryTimestampUtc] IS NOT NULL");
            });

            modelBuilder.Entity<ReadingValue>(entity =>
            {
                entity.ToTable("ReadingValues");
                entity.HasKey(e => e.Id);

                entity.HasOne(d => d.Session)
                    .WithMany(s => s.Values)
                    .HasForeignKey(d => d.SessionId);

                entity.HasOne(d => d.Parameter)
                    .WithMany(p => p.ReadingValues)
                    .HasForeignKey(d => d.ParameterId);
            });

            modelBuilder.Entity<DeviceLatestReading>(entity =>
            {
                entity.ToTable("DeviceLatestReadings");
                entity.HasKey(e => new { e.DeviceId, e.ParameterId });

                entity.HasOne(d => d.Device)
                    .WithMany()
                    .HasForeignKey(d => d.DeviceId);

                entity.HasOne(d => d.Parameter)
                    .WithMany()
                    .HasForeignKey(d => d.ParameterId);
            });

            modelBuilder.Entity<DeviceProfileSyncState>(entity =>
            {
                entity.ToTable("DeviceProfileSyncState");
                entity.HasKey(e => new { e.DeviceId, e.ProfileId });

                entity.HasOne(d => d.Device)
                    .WithMany()
                    .HasForeignKey(d => d.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Profile)
                    .WithMany()
                    .HasForeignKey(d => d.ProfileId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<DeviceEvent>(entity =>
            {
                entity.ToTable("DeviceEvent");
                entity.HasKey(e => e.Id);

                entity.HasOne(d => d.Device)
                    .WithMany()
                    .HasForeignKey(d => d.DeviceId);

                entity.HasOne(d => d.Parameter)
                    .WithMany()
                    .HasForeignKey(d => d.ParameterId);
            });


            modelBuilder.Entity<Parameter>().HasData(
                new Parameter { Id = 1, Name = "Accuracy Test Start", ObisCode = "0.128.162.0.128.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 2, Name = "Accuracy Test Stop", ObisCode = "0.128.162.1.128.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 3, Name = "Activity Calendar", ObisCode = "0.0.13.0.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 4, Name = "Apparent Power – kVA", ObisCode = "1.0.9.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 5, Name = "Association LN Meter Reader", ObisCode = "0.0.40.0.2.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 6, Name = "Available Billing Periods", ObisCode = "0.0.0.1.1.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 7, Name = "Billing Date", ObisCode = "0.0.0.1.2.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 8, Name = "Billing Period Script Table", ObisCode = "0.0.10.0.1.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 9, Name = "Capture Period of Daily Load Profile", ObisCode = "1.0.0.8.5.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 10, Name = "Category", ObisCode = "0.0.94.91.11.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 11, Name = "CMRI Reset", ObisCode = "0.128.154.128.128.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 12, Name = "CT Rating", ObisCode = "0.0.94.91.12.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 13, Name = "Cumulative Billing Count", ObisCode = "0.0.0.1.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 14, Name = "Cumulative Energy – kVAh (Export)", ObisCode = "1.0.10.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 15, Name = "Cumulative Energy (kVAh)", ObisCode = "1.0.9.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 16, Name = "Cumulative Energy (kvarh) – Lag", ObisCode = "1.0.5.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 17, Name = "Cumulative Energy (kvarh) – Lead", ObisCode = "1.0.8.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 18, Name = "Cumulative Energy (kWh)", ObisCode = "1.0.1.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 19, Name = "Cumulative Energy (kWh) – Export", ObisCode = "1.0.2.8.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 20, Name = "Cumulative Power Failure Duration", ObisCode = "0.0.94.91.8.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 21, Name = "Cumulative Programming Count", ObisCode = "0.0.96.2.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 22, Name = "Cumulative Tamper Count", ObisCode = "0.0.94.91.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 23, Name = "Current – IB", ObisCode = "1.0.71.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 24, Name = "Current – IR", ObisCode = "1.0.31.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 25, Name = "Current – IY", ObisCode = "1.0.51.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 26, Name = "Current Related Event Code", ObisCode = "0.0.96.11.1.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 27, Name = "Power Failure Related Event Code", ObisCode = "0.0.96.11.2.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 28, Name = "Profile Capture Period", ObisCode = "1.0.0.8.4.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 29, Name = "PT Power Fail Tamper Events", ObisCode = "1.0.128.7.90.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 30, Name = "Reset Type", ObisCode = "0.128.153.128.128.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 31, Name = "Signed Active Power – kW", ObisCode = "1.0.1.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 32, Name = "Signed Power Factor – B Phase", ObisCode = "1.0.73.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 33, Name = "Signed Power Factor – R Phase", ObisCode = "1.0.33.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 34, Name = "Signed Power Factor – Y Phase", ObisCode = "1.0.53.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 35, Name = "Signed Reactive Power – kvar", ObisCode = "1.0.3.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 36, Name = "Single Action Schedule for Billing Dates", ObisCode = "0.0.15.0.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 37, Name = "TCP/UDP Setup", ObisCode = "0.0.25.0.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 38, Name = "TCP/UDP Setup IPv4 Address", ObisCode = "0.0.25.1.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 39, Name = "TCP/UDP Setup MAC Address", ObisCode = "0.0.25.2.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 40, Name = "Transaction Related Event Code", ObisCode = "0.0.96.11.3.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 41, Name = "Voltage – VBN", ObisCode = "1.0.72.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 42, Name = "Voltage – VRN", ObisCode = "1.0.32.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 43, Name = "Voltage – VYN", ObisCode = "1.0.52.7.0.255", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                
                // Seed new example parameters with TypeName = "ABT"
                new Parameter { Id = 44, Name = "Voltage L1", ObisCode = "1.0.32.7.0.251", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 45, Name = "Voltage L2", ObisCode = "1.0.52.7.0.251", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 46, Name = "Current L1", ObisCode = "1.0.31.7.0.251", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 47, Name = "Active Power", ObisCode = "1.0.1.7.0.251", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 48, Name = "Billing Energy", ObisCode = "1.0.9.8.0.251", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 49, Name = "Import Energy", ObisCode = "1.0.1.8.0.251", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 50, Name = "Export Energy", ObisCode = "1.0.2.8.0.251", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" },
                new Parameter { Id = 51, Name = "Maximum Demand", ObisCode = "1.0.9.6.0.251", IsActive = true, CreatedDate = new DateTime(2026, 1, 1), TypeName = "ABT" }
            );
        }
    }
}
