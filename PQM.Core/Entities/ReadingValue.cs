namespace PQM.Core.Entities
{
    public class ReadingValue
    {
        public long Id { get; set; }
        public long? SessionId { get; set; }
        public int? ParameterId { get; set; }
        public string? Value { get; set; }
        public string? RawValue { get; set; }
        public double? ValueNumeric { get; set; }

        public virtual ReadingSession? Session { get; set; }
        public virtual Parameter? Parameter { get; set; }
    }
}
