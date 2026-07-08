using System;

namespace PQM.Core.Entities
{
    public class EventStatusMapping
    {
        public int Id { get; set; }
        public required string Category { get; set; }
        public required string ObisCode { get; set; }
        public int BitIndex { get; set; }
        public int EventCode { get; set; }
        public required string Label { get; set; }
    }
}
