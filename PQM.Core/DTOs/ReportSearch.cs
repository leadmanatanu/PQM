//using System;

//namespace PQM.Core.DTOs
//{
//    public class ReportSearch : SearchParams
//    {
//        public string? ObjectType { get; set; }
//        public int IntervalMinutes { get; set; } = 15;
//    }

//    public class SearchParams
//    {
//        public int DeviceId { get; set; }
//        public int? ProfileId { get; set; }
//        public List<int>? ProfileIds { get; set; }
//        public int ParameterId { get; set; }
//        public List<int>? ParameterIds { get; set; }
//        public int PageNumber { get; set; }
//        public int PageSize { get; set; }
//        public DateTime StartDate { get; set; }
//        public DateTime EndDate { get; set; }
//        public string? EventType { get; set; }
//    }
//    public class AggregatedReportRow
//    {
//        public int ParameterId { get; set; }
//        public string ParameterName { get; set; } = string.Empty;
//        public DateTime DateStamp { get; set; }
//        public string Value { get; set; } = string.Empty;
//    }
//    public class ParameterValueSearch
//    {
//        public long Id { get; set; }
//        public required string Value { get; set; }
//        public DateTime? DateStamp { get; set; }
//        public required string DeviceName { get; set; }
//        public required string ParameterName { get; set; }
//        public int ParameterId { get; set; }
//    }

//    public class ParameterValueSearchResult
//    {
//        public int TotalCount { get; set; }
//        public List<ParameterValueSearch> DeviceLogSearch { get; set; } = new();
//    }
//}


namespace PQM.Core.DTOs
{
    public class ReportSearch : SearchParams
    {
        public string? ObjectType { get; set; }
        public int IntervalMinutes { get; set; } = 15;
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
        // PageNumber / PageSize removed — no longer used for this report
    }

    public class AggregatedReportRow
    {
        public int ParameterId { get; set; }
        public string ParameterName { get; set; } = string.Empty;
        public int? ProfileId { get; set; }          // NEW
        public DateTime DateStamp { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public class ParameterValueSearch
    {
        public long Id { get; set; }
        public required string Value { get; set; }
        public DateTime? DateStamp { get; set; }
        public required string DeviceName { get; set; }
        public required string ParameterName { get; set; }
        public int ParameterId { get; set; }
        public int? ProfileId { get; set; }          // NEW
        public string? ProfileName { get; set; }      // NEW, filled in controller like LiveScanController does
    }

    // ParameterValueSearchResult no longer needed for this flow —
    // the controller now returns grouped data directly. Keep it only
    // if something else in the codebase still depends on it.
}