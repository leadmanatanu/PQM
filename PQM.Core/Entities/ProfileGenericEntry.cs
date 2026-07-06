using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("ProfileGenericEntry")]
    public class ProfileGenericEntry
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public required string ObisCode { get; set; }
        public required string ProfileName { get; set; }
        public DateTime EntryTime { get; set; }
        public required string ColumnName { get; set; }
        public double? NumericValue { get; set; }
        public string? TextValue { get; set; }
        public string? Unit { get; set; }
    }
}
