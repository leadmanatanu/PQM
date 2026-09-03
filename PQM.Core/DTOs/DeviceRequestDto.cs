using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.DTOs
{
    public class DeviceRequestDto
    {
        public string? Name { get; set; }

        public int? MeterTypeId { get; set; }

        public string? MeterTypeName { get; set; }

        public string? IP { get; set; }

        public int PORT { get; set; }

        public string? SerialNumber { get; set; }

        public string? ConsumerNumber { get; set; }

        public bool IsActive { get; set; }

        public int ClientAddress { get; set; }

        public int ServerAddress { get; set; }

        public string? Authentication { get; set; }

        public string? Password { get; set; }

        public int Timeout { get; set; }

        public string? TimeZoneId { get; set; }

        public int? ScheduleId { get; set; }
    }
}
