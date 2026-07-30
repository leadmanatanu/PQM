using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.DTOs
{
    public class DeviceDto
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public int? MeterTypeId { get; set; }

        public string? MeterTypeName { get; set; }

        public string? IP { get; set; }

        public int PORT { get; set; }

        public string? SerialNumber { get; set; }

        public string? ConsumerNumber { get; set; }

        public bool IsActive { get; set; }

        public DateTime? LastSync { get; set; }

        public string Status { get; set; } = "Offline";

    }
}