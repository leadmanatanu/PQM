using System.Threading;
using System.Threading.Tasks;
using PQM.Core.Entities;

namespace PQM.Core.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int> AddAsync(User user, CancellationToken cancellationToken = default);
    }
}
