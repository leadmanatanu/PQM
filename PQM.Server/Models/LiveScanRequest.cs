namespace PQM.Server.Models
{
    public class LiveScanRequest
    {
        public List<int>? ProfileIds { get; set; }
        public List<int>? ParameterIds { get; set; }
    }
}
