using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.DAL.DTOs.Auth;

public class RegisterUserDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }
}