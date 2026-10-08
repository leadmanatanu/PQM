using Microsoft.AspNetCore.Mvc;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;

namespace PQM.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET: api/users
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userRepository.GetAllUsersAsync();

            return Ok(users);
        }

        // GET: api/users/5
        [HttpGet("GetUserById/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            return Ok(user);
        }

        // POST: api/users
        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            var emailExists =
                await _userRepository.EmailExistsAsync(user.Email);

            if (emailExists)
            {
                return Conflict(new
                {
                    message = "Email already exists."
                });
            }

            user.CreatedAt = DateTime.UtcNow;

            var createdUser =
                await _userRepository.AddUserAsync(user);

            return CreatedAtAction(
                nameof(GetUserById),
                new { id = createdUser.Id },
                createdUser);
        }

        // PUT: api/users/5
        [HttpPut("UpdateUser/{id}")]
        public async Task<IActionResult> UpdateUser(
            int id,
            [FromBody] User user)
        {
            if (id != user.Id)
            {
                return BadRequest(new
                {
                    message = "User ID does not match."
                });
            }

            var updatedUser =
                await _userRepository.UpdateUserAsync(user);

            if (updatedUser == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            return Ok(updatedUser);
        }

        // DELETE: api/users/5
        [HttpDelete("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleted =
                await _userRepository.DeleteUserAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            return Ok(new
            {
                message = "User deleted successfully."
            });
        }
    }
}