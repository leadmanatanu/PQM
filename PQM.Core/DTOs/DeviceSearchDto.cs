using PQM.Core.Entities;

namespace PQM.Core.DTOs
{
    public class DeviceSearchRequest
    {
        public string? Search { get; set; }

        public int? MeterTypeId { get; set; }

        public string? Scheduled { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }

    public class DevicePagedResult
    {
        public List<Device> Items { get; set; } = new();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}