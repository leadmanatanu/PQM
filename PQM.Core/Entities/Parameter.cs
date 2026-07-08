    using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    public class Parameter
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? ObisCode { get; set; }
        public string? ObjectType { get; set; }
        public string? Attribute3 { get; set; }
        public int? Scaler { get; set; }
        public string? Unit { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedId { get; set; }
        [NotMapped]
        public bool IsSelected { get; set; }
    }
}
