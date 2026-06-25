using System;

namespace PQM.Core.Entities
{
    public class Ip4Setup
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public string? Name { get; set; }
        public string? ObjectType { get; set; }
        public string? Value { get; set; }
        public DateTime DateEntered { get; set; }
    }
}
