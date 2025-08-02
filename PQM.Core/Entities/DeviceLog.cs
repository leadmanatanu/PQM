using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    public class DeviceLog : DeviceLogValue
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public int ParameterId { get; set; }
    }

    public class DeviceLogValue
    {
        public string Value { get; set; }
        public DateTime DateStamp { get; set; }
    }

    public class DeviceLogSearch : DeviceLogValue
    {
        public long Id { get; set; }
        public string DeviceName { get; set; }
        public string ParameterName { get; set; }
    }

    public class DeviceLogSearchResult
    {
        public int TotalCount { get; set; }
        public List<DeviceLogSearch> DeviceLogSearch { get; set; }
    }
}
