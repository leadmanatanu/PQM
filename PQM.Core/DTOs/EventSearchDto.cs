using System;
using System.Collections.Generic;

namespace PQM.Core.DTOs
{
    public class EventSearchDto
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public required string DeviceName { get; set; }
        public required string ParameterName { get; set; }
        public required string Value { get; set; }
        public DateTime DateStamp { get; set; }
    }

    public class EventSearchResultDto
    {
        public int TotalCount { get; set; }
        public List<EventSearchDto> EventLogSearch { get; set; } = new();
    }
}
