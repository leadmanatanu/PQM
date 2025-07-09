using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class Parameter
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedId { get; set; }
    }
}
