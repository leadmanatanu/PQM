namespace PQM.Server.Options
{
    public class ServerOptions
    {
        public const string SectionName = "DlmsSettings";

        public int MeterCooldownSeconds { get; set; } = 8;
    }
}
