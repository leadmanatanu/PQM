using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class DeviceLog
    {
        [Key]
        public long Id { get; set; }
        public string Value { get; set; }
        public int DeviceId { get; set; }
        public int ParameterId { get; set; }
        public DateTime DateStamp { get; set; }
    }
}
