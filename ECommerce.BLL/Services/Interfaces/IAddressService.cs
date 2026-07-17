using ECommerce.DAL.DTOs.Address;

namespace ECommerce.BLL.Services.Interfaces;

public interface IAddressService
{
    Task<List<GetAddressDto>> GetUserAddressesAsync(Guid userId);

    Task<GetAddressDto?> GetByIdAsync(Guid id);

    Task<GetAddressDto> CreateAsync(Guid userId, CreateAddressDto dto);

    Task UpdateAsync(Guid userId, Guid addressId, UpdateAddressDto dto);

    Task DeleteAsync(Guid id);

    Task SetDefaultAsync(Guid userId, Guid addressId);
}
