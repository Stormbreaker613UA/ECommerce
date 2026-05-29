using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.DAL.DTOs.User
{
    public class ChangeUserEmailDto
    {
        public Guid UserId { get; set; }
        public string NewEmail { get; set; } = string.Empty;
    }
}
