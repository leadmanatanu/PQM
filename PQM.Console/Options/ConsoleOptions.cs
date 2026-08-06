namespace PQM.Console.Options
{
    public class ConsoleOptions
    {
        public const string SectionName = "DlmsSettings";

        public string DefaultConnection { get; set; } = string.Empty;
        public string ServerHubUrl { get; set; } = "http://localhost:5135/hubs/device";
        public int MeterCooldownSeconds { get; set; } = 8;
    }
}
