using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Core.Entities
{
    [Table("DeviceParameterConfig")]
    public class DeviceParameterConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        public int ParameterId { get; set; }

        [Required]
        public bool IsSelected { get; set; }

        [Required]
        public DateTime LastModifiedDate { get; set; }
    }
}
