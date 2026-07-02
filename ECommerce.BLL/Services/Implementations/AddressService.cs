using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _addressRepository;

        public AddressService(IAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }

        public async Task<List<Address>> GetAllAddressesAsync()
        {
            return await _addressRepository.GetAllAsync();
        }

        public async Task<Address?> GetAddressByIdAsync(Guid id)
        {
            return await _addressRepository.GetByIdAsync(id);
        }

        public async Task<List<Address>> GetAddressesByUserIdAsync(Guid userId)
        {
            return await _addressRepository.GetByUserIdAsync(userId);
        }

        public async Task<Address> AddAddressAsync(Address address)
        {
            await _addressRepository.AddAsync(address);
            return address;
        }

        public async Task UpdateAddressAsync(Guid id, Address address)
        {
            var existing = await _addressRepository.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("Address not found");

            existing.Street = address.Street;
            existing.City = address.City;
            existing.State = address.State;
            existing.PostalCode = address.PostalCode;
            existing.Country = address.Country;
            existing.IsDefault = address.IsDefault;

            await _addressRepository.UpdateAsync(existing);
        }

        public async Task DeleteAddressAsync(Guid id)
        {
            await _addressRepository.DeleteAsync(id);
        }
    }
}
