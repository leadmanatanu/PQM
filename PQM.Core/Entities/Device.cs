using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class Device
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string IP { get; set; }
        public int PORT { get; set; }
        public string? FtpFolder { get; set; }
        public string? SerialNumber { get; set; }
        public string? ConsumerNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedId { get; set; }
        public DateTime? LastSync { get; set; }
    }
}
