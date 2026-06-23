using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.User;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using System.Data;
namespace ECommerce.BLL.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<GetUserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();

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
        var user = await _userRepository.GetByIdAsync(id);

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

    public async Task<GetUserDto?> GetCurrentUserAsync(Guid userId)
    {
        return await GetUserByIdAsync(userId);
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
    {
        var existingUser = await _userRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");

        existingUser.FirstName = updateUserDto.FirstName ?? existingUser.FirstName;
        existingUser.LastName = updateUserDto.LastName ?? existingUser.LastName;
        existingUser.DateOfBirth = updateUserDto.DateOfBirth ?? existingUser.DateOfBirth;
        existingUser.PhoneNumber = updateUserDto.PhoneNumber ?? existingUser.PhoneNumber;

        await _userRepository.UpdateAsync(existingUser);
    }

    public async Task UpdateUserEmailAsync(Guid id, ChangeUserEmailDto changeUserEmailDto)
    {
        var existingUser = await _userRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");

        var userWithSameEmail = await _userRepository.GetByEmailAsync(changeUserEmailDto.NewEmail);

        if (userWithSameEmail != null && userWithSameEmail.Id != id)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        existingUser.Email = changeUserEmailDto.NewEmail;

        await _userRepository.UpdateAsync(existingUser);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");

        await _userRepository.DeleteAsync(user.Id);
    }
}