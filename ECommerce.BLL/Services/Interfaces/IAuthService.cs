using ECommerce.DAL.DTOs.Auth;

namespace ECommerce.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginUserDto dto);
    Task LogoutAsync(Guid userId);
}