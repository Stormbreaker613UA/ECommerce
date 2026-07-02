using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IAddressService
    {
        Task<List<Address>> GetAllAddressesAsync();
        Task<Address?> GetAddressByIdAsync(Guid id);
        Task<List<Address>> GetAddressesByUserIdAsync(Guid userId);
        Task<Address> AddAddressAsync(Address address);
        Task UpdateAddressAsync(Guid id, Address address);
        Task DeleteAddressAsync(Guid id);
    }
}
