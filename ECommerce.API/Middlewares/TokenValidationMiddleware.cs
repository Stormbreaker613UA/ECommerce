using ECommerce.DAL.Caches.Interface;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ECommerce.API.Middlewares;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    public TokenValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenCacheBucket tokenCache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out var userId))
            {
                var presentedToken = context.Request
                    .Headers["Authorization"].ToString()
                    .Replace("Bearer ", string.Empty);
                var cachedToken = tokenCache.GetToken(userId);
                if (cachedToken != presentedToken)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Token is no longer valid.");
                    return;
                }
            }
        }
        await _next(context);
    }
}
