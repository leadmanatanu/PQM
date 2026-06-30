using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("Data")]
    public class Data
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public required string Name { get; set; }
        public string? ObjectType { get; set; }
        public string? Value { get; set; }
        public DateTime DateEntered { get; set; }
    }
}
