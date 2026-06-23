using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.DAL.Caches.Interface
{
    public interface ITokenCacheBucket
    {
        void SetToken(Guid userId, string token, TimeSpan lifetime);
        string? GetToken(Guid userId);
        void RemoveToken(Guid userId);
    }
}
