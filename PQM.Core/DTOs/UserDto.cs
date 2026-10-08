namespace PQM.Core.DTOs
{
    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public int RoleId { get; set; }
    }
    public class UpdateUserDto
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int RoleId { get; set; }
    }
    public class UserResponseDto
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public string? RoleName { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
