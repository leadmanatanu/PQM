using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class Device
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
        public int PORT { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedId { get; set; }
    }

    public class MyTest
    {
        public string Name { get; set; }
    }
}
