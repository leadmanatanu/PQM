using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;

namespace PQM.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _db;

        public UserRepository(DataContext db)
        {
            _db = db;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _db.User
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _db.User
                .Include(u => u.Role)
                .ToListAsync();
        }

        public async Task<User> AddUserAsync(User user)
        {
            _db.User.Add(user);

            await _db.SaveChangesAsync();

            return user;
        }

        public async Task<User?> UpdateUserAsync(User user)
        {
            var existingUser = await _db.User
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (existingUser == null)
            {
                return null;
            }

            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.Password = user.Password;
            existingUser.RoleId = user.RoleId;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return existingUser;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _db.User
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return false;
            }

            _db.User.Remove(user);

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _db.User
                .AnyAsync(u => u.Email == email);
        }
    }
}