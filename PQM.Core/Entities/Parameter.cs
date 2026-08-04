    using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    public class Parameter
    {
        [Key]
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public required string Name { get; set; }
        public string? ObisCode { get; set; }
        public string? Description { get; set; }
        public string? DataType { get; set; }
        public string? ObjectType { get; set; }
        public int? AttributeIndex { get; set; }
        public bool IsHistorical { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public int? Scaler { get; set; }
        public int? UnitCode { get; set; }
        public string? Unit { get; set; }
        public string? AggregationType { get; set; }
        [NotMapped]
        public bool IsSelected { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Profile? Profile { get; set; }
        public virtual ICollection<ReadingValue> ReadingValues { get; set; } = new List<ReadingValue>();
    }
}
