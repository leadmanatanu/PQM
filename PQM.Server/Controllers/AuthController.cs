using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQM.Core.Entities;
using PQM.Infrastructure;
using PQM.Server.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PQM.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly APIResponse _apiResponse = new();
        private readonly ILogger<AuthController> _logger;
        private readonly string _connectionString;

        // Simple thread-safe in-memory session store
        private static readonly ConcurrentDictionary<string, User> Sessions = new();

        public AuthController(ILogger<AuthController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // -------------------- SIGN UP --------------------
        [HttpPost("signup")]
        public IActionResult SignUp([FromBody] SignUpRequest request)
        {
            _apiResponse.Errors.Clear();
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Errors.Add("Invalid sign up request. Email and password are required.");
                return Ok(_apiResponse);
            }

            try
            {
                using var dbContext = new DataContext(_connectionString);

                // Check if user already exists
                var existingUser = dbContext.User.FirstOrDefault(u => u.Email.ToLower() == request.Email.ToLower());
                if (existingUser != null)
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.Conflict;
                    _apiResponse.Errors.Add("A user with this email address already exists.");
                    return Ok(_apiResponse);
                }

                // Create user
                var fullName = $"{request.FirstName} {request.LastName}".Trim();
                if (string.IsNullOrEmpty(fullName))
                {
                    fullName = request.Email.Split('@')[0];
                }

                var user = new User
                {
                    FullName = fullName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    Role = "User",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                dbContext.User.Add(user);
                dbContext.SaveChanges();

                // Log user in automatically after sign up
                var token = Guid.NewGuid().ToString("N");
                Sessions[token] = user;

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new AuthResponseData
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id.ToString(),
                        Name = user.FullName,
                        Email = user.Email,
                        Avatar = "/assets/avatar-1.png"
                    }
                };

                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sign up user {Email}", request.Email);
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                _apiResponse.Errors.Add(ex.Message);
                return Ok(_apiResponse);
            }
        }

        // -------------------- LOGIN --------------------
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            _apiResponse.Errors.Clear();
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _apiResponse.Errors.Add("Email and password are required.");
                return Ok(_apiResponse);
            }

            try
            {
                using var dbContext = new DataContext(_connectionString);

                var user = dbContext.User.FirstOrDefault(u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive);
                if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                {
                    _apiResponse.Status = false;
                    _apiResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;
                    _apiResponse.Errors.Add("Invalid email or password.");
                    return Ok(_apiResponse);
                }

                // Generate session token
                var token = Guid.NewGuid().ToString("N");
                Sessions[token] = user;

                _apiResponse.Status = true;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
                _apiResponse.Data = new AuthResponseData
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id.ToString(),
                        Name = user.FullName,
                        Email = user.Email,
                        Avatar = "/assets/avatar-1.png"
                    }
                };

                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to login user {Email}", request.Email);
                _apiResponse.Status = false;
                _apiResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                _apiResponse.Errors.Add(ex.Message);
                return Ok(_apiResponse);
            }
        }

        // -------------------- LOGOUT --------------------
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            _apiResponse.Errors.Clear();
            string authHeader = Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                string token = authHeader.Substring("Bearer ".Length).Trim();
                Sessions.TryRemove(token, out _);
            }

            _apiResponse.Status = true;
            _apiResponse.StatusCode = System.Net.HttpStatusCode.OK;
            _apiResponse.Data = "Logged out successfully.";
            return Ok(_apiResponse);
        }

        // -------------------- HELPER METHODS --------------------
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }
    }

    public class SignUpRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }

    public class AuthResponseData
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }
}
