

namespace PQM.Infrastructure.Services
{
    public static class ProfileCatalog
    {
        public static readonly Dictionary<string, string> TimeSeriesProfiles = new()
        {
            { "1.0.99.1.0.255", "Block Load" },
            { "1.0.99.2.0.255", "Daily Load" },
            { "1.0.98.1.0.255", "Billing" },
            { "0.0.99.98.0.255", "Voltage Events" },
            { "0.0.99.98.1.255", "Current Events" },
            { "0.0.99.98.2.255", "Power Events" },
            { "0.0.99.98.3.255", "Transaction Events" },
            { "0.0.99.98.4.255", "Other Tamper Events" }
        };
        public static readonly Dictionary<string, string> StaticOrMetadataProfiles = new()
        {
            { "0.0.94.91.10.255", "Nameplate" },
            { "1.0.94.91.0.255",  "Instantaneous" },
            { "1.0.94.91.3.255",  "Scaler: Instantaneous" }, // Metadata — NOT time-series
            { "1.0.94.91.4.255",  "Scaler: Block Load" },     // Metadata — NOT time-series
            { "1.0.94.91.5.255",  "Scaler: Daily Load" },     // Metadata — NOT time-series
            { "1.0.94.91.6.255",  "Scaler: Billing" },        // Metadata — NOT time-series
            { "1.0.94.91.7.255",  "Scaler: Events" },         // Metadata — NOT time-series
            { "0.128.187.0.128.255", "Manufacturer specific" },
            { "1.0.128.7.90.255",    "Man. specific" }
        };
        public static Dictionary<string, string> AllProfiles =>TimeSeriesProfiles.Concat(StaticOrMetadataProfiles).ToDictionary(x => x.Key, x => x.Value);
    }
}
