using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Address;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations;

public class AddressService : IAddressService
{
    private readonly IAddressRepository _addressRepository;
    private readonly IUserRepository _userRepository;

    public AddressService(
        IAddressRepository addressRepository,
        IUserRepository userRepository)
    {
        _addressRepository = addressRepository;
        _userRepository = userRepository;
    }

    public async Task<List<GetAddressDto>> GetUserAddressesAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var addresses = await _addressRepository.GetByUserIdAsync(userId);

        return addresses.Select(MapToDto).ToList();
    }

    public async Task<GetAddressDto?> GetByIdAsync(Guid id)
    {
        var address = await _addressRepository.GetByIdAsync(id);

        return address == null
            ? null
            : MapToDto(address);
    }

    public async Task<GetAddressDto> CreateAsync(Guid userId, CreateAddressDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        if (dto.IsDefault)
        {
            var currentDefault = await _addressRepository.GetDefaultAsync(userId);

            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
                await _addressRepository.UpdateAsync(currentDefault);
            }
        }

        var address = new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Street = dto.Street,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            IsDefault = dto.IsDefault
        };

        await _addressRepository.AddAsync(address);

        return MapToDto(address);
    }

    public async Task UpdateAsync(Guid userId, Guid addressId, UpdateAddressDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var address = await _addressRepository.GetByIdAsync(addressId);

        if (address == null)
            throw new KeyNotFoundException("Address not found.");

        if (address.UserId != userId)
            throw new InvalidOperationException("Address does not belong to the user.");

        if (dto.IsDefault == true)
        {
            var currentDefault = await _addressRepository.GetDefaultAsync(userId);

            if (currentDefault != null && currentDefault.Id != address.Id)
            {
                currentDefault.IsDefault = false;
                await _addressRepository.UpdateAsync(currentDefault);
            }

            address.IsDefault = true;
        }

        if (!string.IsNullOrWhiteSpace(dto.Street))
            address.Street = dto.Street;

        if (!string.IsNullOrWhiteSpace(dto.City))
            address.City = dto.City;

        if (dto.State != null)
            address.State = dto.State;

        if (!string.IsNullOrWhiteSpace(dto.PostalCode))
            address.PostalCode = dto.PostalCode;

        if (!string.IsNullOrWhiteSpace(dto.Country))
            address.Country = dto.Country;

        await _addressRepository.UpdateAsync(address);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _addressRepository.DeleteAsync(id);
    }

    public async Task SetDefaultAsync(Guid userId, Guid addressId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var addresses = await _addressRepository.GetByUserIdAsync(userId);

        var address = addresses.FirstOrDefault(a => a.Id == addressId);

        if (address == null)
            throw new KeyNotFoundException("Address not found.");

        foreach (var item in addresses)
        {
            item.IsDefault = item.Id == addressId;
            await _addressRepository.UpdateAsync(item);
        }
    }

    private static GetAddressDto MapToDto(Address address)
    {
        return new GetAddressDto
        {
            Id = address.Id,
            Street = address.Street,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault
        };
    }
}
