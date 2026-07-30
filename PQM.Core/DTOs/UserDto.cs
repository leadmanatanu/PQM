namespace PQM.Core.DTOs
{
    public class SignUpRequestDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class SignInRequestDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class UserMeResponseDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public string LastName { get; set; } = string.Empty;
    }
}
