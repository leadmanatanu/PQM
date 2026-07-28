using System;
using System.Collections.Generic;

namespace PQM.Core.Entities
{
    public class ReadingSession
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public int ProfileId { get; set; }
        public DateTime? ReadTime { get; set; }
        public DateTime? EntryTimestampUtc { get; set; }

        public virtual Device? Device { get; set; }
        public virtual Profile? Profile { get; set; }
        public virtual ICollection<ReadingValue> Values { get; set; } = new List<ReadingValue>();
    }
}
