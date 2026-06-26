using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("IecHdlcSetup")]
    public class IecHdlcSetup
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public string Name { get; set; } = null!;
        public string? ObjectType { get; set; }
        public string? Value { get; set; }
        public DateTime DateEntered { get; set; }
    }
}
