namespace PQM.Core.DTOs
{
    public class SaveConfigResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SavedCount { get; set; }
    }
}
