using System.Collections.Generic;

namespace PQM.Core.DTOs
{
    public class DeviceConfigurationDto
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; }
        public string MeterType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public List<AvailableParameterDto> AvailableParameters { get; set; } = new();
        public List<int> SelectedParameterIds { get; set; } = new();
    }
}
