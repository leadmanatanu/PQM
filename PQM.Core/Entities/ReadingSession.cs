using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class ReadingSession
    {
        [Key]
        public long Id { get; set; }
        public DateTime? ReadTimeAt { get; set; }
        public DateTime? EntryTimestamp { get; set; }
        public int DeviceId { get; set; }
        public Device? Device { get; set; }

        public int ProfileId { get; set; }
        public Profile? Profile { get; set; }
    }
}
