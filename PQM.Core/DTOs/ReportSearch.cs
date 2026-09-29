namespace PQM.Core.DTOs
{
    public class ReportSearch : SearchParams
    {
        public string? ObjectType { get; set; }

        public int IntervalMinutes { get; set; } = 15;

        // Pagination is applied only to BlockLoad Profile timestamps
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 96;

        // Set internally by the controller/repository.
        // This is NOT something the user sees in the report.
        public int? BlockLoadProfileId { get; set; }
    }

    public class SearchParams
    {
        public int DeviceId { get; set; }

        public int? ProfileId { get; set; }

        public List<int>? ProfileIds { get; set; }

        public int ParameterId { get; set; }

        public List<int>? ParameterIds { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string? EventType { get; set; }
    }

    // Represents one parameter value at one timestamp.
    // There is NO TotalTimestampCount here.
    public class AggregatedReportRow
    {
        public int ParameterId { get; set; }

        public string ParameterName { get; set; } = string.Empty;

        public int? ProfileId { get; set; }

        public DateTime DateStamp { get; set; }

        public string Value { get; set; } = string.Empty;
    }

    // Pagination information for BlockLoad Profile.
    // This is metadata, not a report row.
    public class ReportPagination
    {
        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalTimestamps { get; set; }

        public int TotalPages { get; set; }
    }

    // Result returned by the repository for the report screen.
    public class AggregatedReportResult
    {
        public List<ParameterValueSearch> Items { get; set; } = new();

        public ReportPagination? Pagination { get; set; }
    }

    public class ParameterValueSearch
    {
        public long Id { get; set; }

        public required string Value { get; set; }

        public DateTime? DateStamp { get; set; }

        public required string DeviceName { get; set; }

        public required string ParameterName { get; set; }

        public int ParameterId { get; set; }

        public int? ProfileId { get; set; }

        public string? ProfileName { get; set; }
    }
}