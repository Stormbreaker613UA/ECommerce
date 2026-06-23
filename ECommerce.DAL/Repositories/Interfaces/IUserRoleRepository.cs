using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IUserRoleRepository
{
    Task<List<UserRole>> GetAllAsync();
    Task<UserRole?> GetByIdAsync(Guid id);
    Task<UserRole?> GetByNameAsync(string name);
    Task AddAsync(UserRole role);
    Task UpdateAsync(UserRole role);
    Task DeleteAsync(Guid id);
}
