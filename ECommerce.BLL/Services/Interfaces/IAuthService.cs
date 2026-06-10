using System;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(User user, string password);
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User?> GetUserByIdAsync(Guid id);
    }
}