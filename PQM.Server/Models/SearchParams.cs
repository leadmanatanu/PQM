using System;
using System.Collections.Generic;

namespace PQM.Server.Models
{
    public class SearchParams
    {
        public int DeviceId { get; set; }
        public int? ProfileId { get; set; }

        // Legacy single-parameterId — kept for backward compatibility.
        // If both ParameterId and ParameterIds are provided, they are merged.
        public int ParameterId { get; set; }

        // Multi-select: list of parameter IDs to filter by.
        // Bound from repeated query-string keys: ?parameterIds=1&parameterIds=2
        public List<int>? ParameterIds { get; set; }

        public int PageNumber { get; set; }

        // PageSize now controls how many distinct TIMESTAMPS (columns) to return
        // in the pivoted table, not the number of reading rows.
        public int PageSize { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? EventType { get; set; }
    }
}
