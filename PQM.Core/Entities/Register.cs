using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("Register")]
    public class Register
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public required string Name { get; set; }
        public string? ObjectType { get; set; }
        public string? Value { get; set; }
        public double? NumericValue { get; set; }
        public int? Scaler { get; set; }
        public string? Unit { get; set; }
        public string? ObisCode { get; set; }
        public DateTime DateEntered { get; set; }
    }
}
