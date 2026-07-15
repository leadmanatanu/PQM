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
        public DbSet<EventLog> EventLog { get; set; } = null!;
        public DbSet<FTPSetting> FTPSetting { get; set; } = null!;
        public DbSet<Register> Register { get; set; } = null!;
        public DbSet<Data> Data { get; set; } = null!;
        public DbSet<IecHdlcSetup> IecHdlcSetup { get; set; } = null!;
        public DbSet<TcpUdpSetup> TcpUdpSetup { get; set; } = null!;
        public DbSet<Ip4Setup> Ip4Setup { get; set; } = null!;
        public DbSet<MacAddressSetup> MacAddressSetup { get; set; } = null!;
        public DbSet<AssociationLogicalName> AssociationLogicalName { get; set; } = null!;
        public DbSet<Clock> Clock { get; set; } = null!;
        public DbSet<ScriptTable> ScriptTable { get; set; } = null!;
        public DbSet<ProfileGeneric> ProfileGeneric { get; set; } = null!;
        public DbSet<ProfileGenericEntry> ProfileGenericEntry { get; set; } = null!;
        public DbSet<ActionSchedule> ActionSchedule { get; set; } = null!;
        public DbSet<ActivityCalendar> ActivityCalendar { get; set; } = null!;
        public DbSet<ConnectedHeader> ConnectedHeader { get; set; } = null!;
        public DbSet<DLMSObject> DLMSObject { get; set; } = null!;
        public DbSet<ObjectParameter> ObjectParameter { get; set; } = null!;
        public DbSet<ParameterValue> ParameterValue { get; set; } = null!;
        public DbSet<EventStatusMapping> EventStatusMapping { get; set; } = null!;
        public DbSet<User> User { get; set; } = null!;

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
            modelBuilder.Entity<DeviceLogSearch>().HasNoKey().ToTable((string?)null);

            modelBuilder.Entity<EventStatusMapping>().HasData(
                // Voltage Related Events
                new EventStatusMapping { Id = 1, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 0, EventCode = 1, Label = "R-Phase - Voltage Missing - Occurrence" },
                new EventStatusMapping { Id = 2, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 1, EventCode = 2, Label = "R-Phase - Voltage Missing - Restoration" },
                new EventStatusMapping { Id = 3, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 2, EventCode = 3, Label = "Y-Phase - Voltage Missing - Occurrence" },
                new EventStatusMapping { Id = 4, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 3, EventCode = 4, Label = "Y-Phase - Voltage Missing - Restoration" },
                new EventStatusMapping { Id = 5, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 4, EventCode = 5, Label = "B-Phase - Voltage Missing - Occurrence" },
                new EventStatusMapping { Id = 6, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 5, EventCode = 6, Label = "B-Phase - Voltage Missing - Restoration" },
                new EventStatusMapping { Id = 7, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 6, EventCode = 7, Label = "Over Voltage in any Phase - Occurrence" },
                new EventStatusMapping { Id = 8, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 7, EventCode = 8, Label = "Over Voltage in any Phase - Restoration" },
                new EventStatusMapping { Id = 9, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 8, EventCode = 9, Label = "Low Voltage in any Phase - Occurrence" },
                new EventStatusMapping { Id = 10, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 9, EventCode = 10, Label = "Low Voltage in any Phase - Restoration" },
                new EventStatusMapping { Id = 11, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 10, EventCode = 11, Label = "Voltage Unbalance - Occurrence" },
                new EventStatusMapping { Id = 12, Category = "voltage", ObisCode = "0.0.96.11.0.255", BitIndex = 11, EventCode = 12, Label = "Voltage Unbalance - Restoration" },

                // Current Related Events
                new EventStatusMapping { Id = 13, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 4, EventCode = 51, Label = "R Phase - Current reverse - Occurrence" },
                new EventStatusMapping { Id = 14, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 5, EventCode = 52, Label = "R Phase - Current reverse - Restoration" },
                new EventStatusMapping { Id = 15, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 8, EventCode = 53, Label = "Y Phase - Current reverse - Occurrence" },
                new EventStatusMapping { Id = 16, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 9, EventCode = 54, Label = "Y Phase - Current reverse - Restoration" },
                new EventStatusMapping { Id = 17, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 10, EventCode = 55, Label = "B Phase - Current reverse - Occurrence" },
                new EventStatusMapping { Id = 18, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 11, EventCode = 56, Label = "B Phase - Current reverse - Restoration" },
                new EventStatusMapping { Id = 19, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 7, EventCode = 63, Label = "Current Unbalance - Occurrence" },
                new EventStatusMapping { Id = 20, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 6, EventCode = 64, Label = "Current Unbalance - Restoration" },
                new EventStatusMapping { Id = 21, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 0, EventCode = 65, Label = "Current bypass - Occurrence" },
                new EventStatusMapping { Id = 22, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 1, EventCode = 66, Label = "Current bypass - Restoration" },
                new EventStatusMapping { Id = 23, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 2, EventCode = 67, Label = "Over current in any phase - Occurrence" },
                new EventStatusMapping { Id = 24, Category = "current", ObisCode = "0.0.96.11.1.255", BitIndex = 3, EventCode = 68, Label = "Over current in any phase - Restoration" },

                // Power Related Events
                new EventStatusMapping { Id = 25, Category = "power", ObisCode = "0.0.96.11.2.255", BitIndex = 0, EventCode = 101, Label = "Power failure - Occurrence" },
                new EventStatusMapping { Id = 26, Category = "power", ObisCode = "0.0.96.11.2.255", BitIndex = 1, EventCode = 102, Label = "Power failure - Restoration" },

                // Transaction Related Events
                new EventStatusMapping { Id = 27, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 0, EventCode = 151, Label = "Real Time Clock - Date and Time" },
                new EventStatusMapping { Id = 28, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 1, EventCode = 152, Label = "Demand Integration Period" },
                new EventStatusMapping { Id = 29, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 2, EventCode = 153, Label = "Profile Capture Period" },
                new EventStatusMapping { Id = 30, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 3, EventCode = 154, Label = "Single-action Schedule for Billing Dates" },
                new EventStatusMapping { Id = 31, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 4, EventCode = 155, Label = "Activity Calendar Time Zones" },
                new EventStatusMapping { Id = 32, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 5, EventCode = 157, Label = "New Firmware Activated" },
                new EventStatusMapping { Id = 33, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 6, EventCode = 158, Label = "Load limit (kW) set" },
                new EventStatusMapping { Id = 34, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 7, EventCode = 159, Label = "Enabled - load limit function" },
                new EventStatusMapping { Id = 35, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 8, EventCode = 160, Label = "Disabled - load limit function" },
                new EventStatusMapping { Id = 36, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 9, EventCode = 161, Label = "LLS secret (MR) change" },
                new EventStatusMapping { Id = 37, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 10, EventCode = 162, Label = "HLS key (US) change" },
                new EventStatusMapping { Id = 38, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 11, EventCode = 163, Label = "HLS key (FW) change" },
                new EventStatusMapping { Id = 39, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 12, EventCode = 164, Label = "Global key change(encryption and authentication)" },
                new EventStatusMapping { Id = 40, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 13, EventCode = 165, Label = "ESWF change" },
                new EventStatusMapping { Id = 41, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 14, EventCode = 166, Label = "MD reset" },
                new EventStatusMapping { Id = 42, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 15, EventCode = 169, Label = "Single Action Schedule for Image Activation" },
                new EventStatusMapping { Id = 43, Category = "transaction", ObisCode = "0.0.96.11.3.255", BitIndex = 16, EventCode = 182, Label = "Passive Relay time." },

                // Others Events
                new EventStatusMapping { Id = 44, Category = "others", ObisCode = "0.0.96.11.4.255", BitIndex = 0, EventCode = 201, Label = "Influence of permanent magnet - Occurrence" },
                new EventStatusMapping { Id = 45, Category = "others", ObisCode = "0.0.96.11.4.255", BitIndex = 1, EventCode = 202, Label = "Influence of permanent magnet - Restoration" },
                new EventStatusMapping { Id = 46, Category = "others", ObisCode = "0.0.96.11.4.255", BitIndex = 2, EventCode = 203, Label = "Neutral disturbance - Occurrence" },
                new EventStatusMapping { Id = 47, Category = "others", ObisCode = "0.0.96.11.4.255", BitIndex = 3, EventCode = 204, Label = "Neutral disturbance - Restoration" },
                new EventStatusMapping { Id = 48, Category = "others", ObisCode = "0.0.96.11.4.255", BitIndex = 4, EventCode = 205, Label = "Meter cover opened" },
                new EventStatusMapping { Id = 50, Category = "others", ObisCode = "0.0.96.11.4.255", BitIndex = 5, EventCode = 206, Label = "Terminal cover opened" }
            );
        }
    }
}
