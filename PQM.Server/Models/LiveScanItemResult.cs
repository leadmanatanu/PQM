namespace PQM.Server.Models
{
    public class LiveScanItemResult
    {
        public int ParameterId { get; set; }
        public string ParameterName { get; set; } = "";
        public string ObisCode { get; set; } = "";
        public string Value { get; set; } = "";
        public string? Unit { get; set; }
        public string? Error { get; set; }
    }
}
