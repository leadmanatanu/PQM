using System;
using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class DeviceProfileSyncState
    {
        [Key]
        public int Id { get; set; }
        public DateTime? LastReadTimestamp { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public int? LastEntriesInUse { get; set; }
        public int DeviceId { get; set; }
        public Device? Device { get; set; }
        public int ProfileId { get; set; }
        public Profile? Profile { get; set; }
    }
}
