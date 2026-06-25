using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("ConnectedHeader")]
    public class ConnectedHeader
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string Name { get; set; } = null!;
    }
}
