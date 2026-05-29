using ECommerce.DAL.DTOs.User;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Implementations;
using System.Data;
namespace ECommerce.BLL.Services;

public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    public async Task<List<GetUserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return users.Select(user => new GetUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            PhoneNumber = user.PhoneNumber
        }).ToList();
    }

    public async Task<GetUserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            return null;
        }
        return new GetUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            PhoneNumber = user.PhoneNumber
        };
    }

    public async Task<GetUserDto> AddUserAsync(AddUserDto addUserDto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = addUserDto.Email,
            PasswordHash = addUserDto.PasswordHash, // add proper password hashing in a real application
            FirstName = addUserDto.FirstName,
            LastName = addUserDto.LastName,
            DateOfBirth = addUserDto.DateOfBirth,
            PhoneNumber = addUserDto.PhoneNumber,
            UserRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001") // default role, should be set properly in a real application
        };

        await _userRepository.AddUserAsync(user);

        var getUserDto = new GetUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            PhoneNumber = user.PhoneNumber
        };

        return getUserDto;
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
    {
        var existingUser = await _userRepository.GetUserByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found");

        existingUser.FirstName = updateUserDto?.FirstName;
        existingUser.LastName = updateUserDto?.LastName;
        existingUser.DateOfBirth = updateUserDto?.DateOfBirth;
        existingUser.PhoneNumber = updateUserDto?.PhoneNumber;

        await _userRepository.UpdateUserAsync(existingUser);
    }

    public async Task UpdateUserEmailAsync(Guid id, ChangeUserEmailDto changeUserEmailDto)
    {
        var existingUser = await _userRepository.GetUserByIdAsync(id)
        ?? throw new KeyNotFoundException("User not found");

        existingUser.Email = changeUserEmailDto.NewEmail;
        await _userRepository.UpdateUserAsync(existingUser);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user != null)
            await _userRepository.DeleteUserAsync(id);
    }
}
