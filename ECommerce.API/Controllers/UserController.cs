using ECommerce.BLL.Services;
using ECommerce.DAL.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<GetUserDto>>> GetAllUsersAsync()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<GetUserDto>> GetUserByIdAsync(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }
    [HttpPost("add")]
    public async Task<ActionResult<GetUserDto>> AddUserAsync(AddUserDto addUserDto)
    {
        var addedUser = await _userService.AddUserAsync(addUserDto);
        return Ok(addedUser);
    }

    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
    {
        await _userService.UpdateUserAsync(id, updateUserDto);
        return Ok();
    }

    [HttpPut("update/{id}/{email}")]
    public async Task<IActionResult> UpdateUserEmailAsync(Guid id, ChangeUserEmailDto changeUserEmailDto)
    {
        await _userService.UpdateUserEmailAsync(id, changeUserEmailDto);
        return Ok();
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteUserAsync(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }
}