using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class DeviceParameterMapping
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int ParameterId { get; set; }
        public DateTime DateStamp { get; set; }
    }
}
