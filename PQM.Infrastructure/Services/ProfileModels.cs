using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PQM.Infrastructure.Services
{
    /// <summary>
    /// A single column descriptor from a profile's capture objects (attribute 3).
    /// </summary>
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

        public override string ToString() =>
            $"Column {Index} | OBIS: {LogicalName} | Attribute: {AttributeIndex} | Type: {ObjectType} | Scaler: {Scaler} | Unit: {Unit} | Description: {Description}";
    }

    /// <summary>
    /// One row from a profile buffer read. Timestamp is the meter's own clock value
    /// extracted from the first DateTime/GXDateTime cell in the row.
    /// Values contains the raw typed objects from the DLMS frame (before ValueFormatter).
    /// </summary>
    public sealed class ProfileRow
    {
        public DateTime? Timestamp { get; set; }
        public List<object?> Values { get; set; } = new();

        public override string ToString()
        {
            var values = string.Join(" | ", Values.Select(x => x?.ToString() ?? "NULL"));
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} | {values}";
        }
    }
}
