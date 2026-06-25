using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("DLMSObject")]
    public class DLMSObject
    {
        [Key]
        public int Id { get; set; }
        public int HeaderId { get; set; }
        public string Name { get; set; } = null!;
        public string ObisCode { get; set; } = null!;
        public string ObjectType { get; set; } = null!;
    }
}
