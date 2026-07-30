using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using PQM.Core.Interfaces.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PQM.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _db;

        public UserRepository(DataContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _db.User
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.User
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<int> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user.CreatedDate == default)
            {
                user.CreatedDate = DateTime.UtcNow;
            }

            await _db.User.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
    }
}
