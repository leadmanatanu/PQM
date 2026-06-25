using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("ObjectParameter")]
    public class ObjectParameter
    {
        [Key]
        public int Id { get; set; }
        public int ObjectId { get; set; }
        public int AttributeId { get; set; }
        public string Name { get; set; } = null!;
        public string? DataType { get; set; }
        public string? AccessType { get; set; }
    }
}
