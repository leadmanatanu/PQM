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
        }
    }
}
