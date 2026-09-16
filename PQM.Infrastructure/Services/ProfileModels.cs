using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PQM.Infrastructure.Services
{
    public sealed class ProfileColumnInfo
    {
        public int Index { get; set; }
        public string LogicalName { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty;
        public int AttributeIndex { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? Scaler { get; set; }
        public int? UnitCode { get; set; }
        public string? Unit { get; set; }

    }
    public sealed class ProfileRow
    {
        public DateTime? Timestamp { get; set; }
        public List<object?> Values { get; set; } = new();
    }
}
