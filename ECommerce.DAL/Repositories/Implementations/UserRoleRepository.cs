using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly ECommerceDbContext _dbContext;

    public UserRoleRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<UserRole>> GetAllAsync()
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<UserRole?> GetByIdAsync(Guid id)
    {
        return await _dbContext.UserRoles
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<UserRole?> GetByNameAsync(string name)
    {
        return await _dbContext.UserRoles
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task AddAsync(UserRole role)
    {
        await _dbContext.UserRoles.AddAsync(role);
    }

    public Task UpdateAsync(UserRole role)
    {
        _dbContext.UserRoles.Update(role);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _dbContext.UserRoles
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            throw new KeyNotFoundException("Role not found");

        _dbContext.UserRoles.Remove(role);
    }
}
