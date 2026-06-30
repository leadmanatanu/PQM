using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("ActivityCalendar")]
    public class ActivityCalendar
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public string? Name { get; set; }
        public string? ObjectType { get; set; }
        public string? Value { get; set; }
        public DateTime DateEntered { get; set; }
    }
}
