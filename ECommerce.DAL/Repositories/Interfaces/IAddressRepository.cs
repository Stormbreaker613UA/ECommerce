using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IAddressRepository
{
    Task<List<Address>> GetByUserIdAsync(Guid userId);

    Task<Address?> GetByIdAsync(Guid id);

    Task<Address?> GetDefaultAsync(Guid userId);

    Task AddAsync(Address address);

    Task UpdateAsync(Address address);

    Task DeleteAsync(Guid id);
}
