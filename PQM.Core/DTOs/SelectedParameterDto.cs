using System;

namespace PQM.Core.DTOs
{
    public class SelectedParameterDto
    {
        public int ParameterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ObisCode { get; set; }
        public string? ObjectType { get; set; }
        public string? Attribute3 { get; set; }
        public int? Scaler { get; set; }
        public string? Unit { get; set; }
        public string? MeterTypeName { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}
