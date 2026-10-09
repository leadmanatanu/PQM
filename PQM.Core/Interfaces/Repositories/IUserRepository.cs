using PQM.Core.Entities;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(int id);

        Task<List<User>> GetAllUsersAsync();

        Task<User> AddUserAsync(User user);

        Task<User?> UpdateUserAsync(User user);

        Task<bool> DeleteUserAsync(int id);

        Task<bool> EmailExistsAsync(string email);
    }
}
