using ECommerce.DAL.DTOs.User;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<GetUserDto>> GetAllUsersAsync();
        Task<GetUserDto?> GetUserByIdAsync(Guid id);
        Task<GetUserDto?> GetCurrentUserAsync(Guid userId);
        Task UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
        Task UpdateUserEmailAsync(Guid id, ChangeUserEmailDto changeUserEmailDto);
        Task DeleteUserAsync(Guid id);
    }
}
