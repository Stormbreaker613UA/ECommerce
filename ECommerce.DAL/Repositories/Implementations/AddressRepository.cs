using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;
using ECommerce.DAL.DbContexts;
using Microsoft.EntityFrameworkCore;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.DAL.Repositories.Implementations
{
    public class AddressRepository : IAddressRepository
    {
        private readonly ECommerceDbContext _dbContext;

        public AddressRepository(ECommerceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Address>> GetAllAsync()
        {
            return await _dbContext.Addresses.ToListAsync();
        }

        public async Task<Address?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Addresses.FindAsync(id);
        }

        public async Task<List<Address>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.Addresses.Where(a => a.UserId == userId).ToListAsync();
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
            var address = await _dbContext.Addresses.FindAsync(id);
            if (address != null)
            {
                _dbContext.Addresses.Remove(address);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
