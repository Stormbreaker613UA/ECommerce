using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.DAL.DTOs.Auth;

public class LoginUserDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}