using System;
using System.Collections.Generic;

namespace PQM.Core.DTOs
{
    public class ParameterValueSearchDto
    {
        public long Id { get; set; }
        public required string Value { get; set; }
        public DateTime? DateStamp { get; set; }
        public required string DeviceName { get; set; }
        public required string ParameterName { get; set; }
        public int ParameterId { get; set; }
    }

    public class ParameterValueSearchResultDto
    {
        public int TotalCount { get; set; }
        public List<ParameterValueSearchDto> DeviceLogSearch { get; set; } = new();
    }
}
