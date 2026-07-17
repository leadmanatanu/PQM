using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("ParameterValue")]
    public class ParameterValue
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public int ParameterId { get; set; }
        public string? Value { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}
