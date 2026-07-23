using System;

namespace PQM.Core.Entities
{
    public class DeviceProfileSyncState
    {
        public int DeviceId { get; set; }
        public int ProfileId { get; set; }
        public DateTime? LastReadTimestampUtc { get; set; }
        public int? LastReadEntryIndex { get; set; }
        public DateTime LastSyncedAt { get; set; }

        public virtual Device? Device { get; set; }
        public virtual Profile? Profile { get; set; }
    }
}
