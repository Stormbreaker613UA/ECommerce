using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class AddressRepository : IAddressRepository
{
    private readonly ECommerceDbContext _dbContext;

    public AddressRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Address>> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.Addresses.Where(a => a.UserId == userId).OrderByDescending(a => a.IsDefault).ToListAsync();
    }

    public async Task<Address?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Addresses.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Address?> GetDefaultAsync(Guid userId)
    {
        return await _dbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
    }

    public async Task AddAsync(Address address)
    {
        await _dbContext.Addresses.AddAsync(address);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Address address)
    {
        _dbContext.Addresses.Update(address);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var address = await GetByIdAsync(id);

        if (address == null)
            throw new KeyNotFoundException("Address not found.");

        _dbContext.Addresses.Remove(address);

        await _dbContext.SaveChangesAsync();
    }
}
