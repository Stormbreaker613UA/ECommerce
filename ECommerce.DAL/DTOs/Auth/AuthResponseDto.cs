using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.DAL.DTOs.Auth;

public class AuthResponseDto
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
}