using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("Clock")]
    public class Clock
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public string? Name { get; set; }
        public string? ObjectType { get; set; }
        public string? Value { get; set; }
        public DateTime DateEntered { get; set; }
    }
}
