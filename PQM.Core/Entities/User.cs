using System;
using System.ComponentModel.DataAnnotations;

namespace PQM.Core.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }
        
        [Required]
        [MaxLength(100)]
        public required string Email { get; set; }
        
        [Required]
        [MaxLength(256)]
        public required string PasswordHash { get; set; }
        
        [MaxLength(20)]
        public string Role { get; set; } = "User";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
