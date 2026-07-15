using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Caches.Interface;
using ECommerce.DAL.DTOs.Auth;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ECommerce.BLL.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenCacheBucket _tokenCache;
    private readonly IProductBucketRepository _productBucketRepository;

    public AuthService(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IConfiguration configuration,
        IPasswordHasher<User> passwordHasher,
        ITokenCacheBucket tokenCache,
        IProductBucketRepository productBucketRepository)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _tokenCache = tokenCache;
        _productBucketRepository = productBucketRepository;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var customerRole = await _userRoleRepository.GetByNameAsync("Customer");

        if (customerRole == null)
        {
            throw new InvalidOperationException("Default Customer role was not found.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            PhoneNumber = dto.PhoneNumber,
            UserRoleId = customerRole.Id,
            UserRole = customerRole
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);

        var productBucket = new ProductBucket
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };
        
        await _productBucketRepository.AddAsync(productBucket);

        var token = GenerateJwtToken(user);
        var tokenLifetime = GetTokenLifetime();

        _tokenCache.SetToken(user.Id, token, tokenLifetime);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.UserRole.Name,
            Token = token
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginUserDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user == null)
        {
            return null;
        }

        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password);

        if (passwordVerificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (user.UserRole == null)
        {
            throw new InvalidOperationException("User role was not loaded.");
        }

        var token = GenerateJwtToken(user);
        var tokenLifetime = GetTokenLifetime();

        _tokenCache.SetToken(user.Id, token, tokenLifetime);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.UserRole.Name,
            Token = token
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        _tokenCache.RemoveToken(userId);
        await Task.CompletedTask;
    }

    // Token generation and caching methods
    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT key is not configured.");
        }

        if (user.UserRole == null)
        {
            throw new InvalidOperationException("User role was not loaded.");
        }

        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.UserRole.Name)
    };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key,
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var tokenLifetime = GetTokenLifetime();

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(tokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TimeSpan GetTokenLifetime()
    {
        var expiresInMinutes = _configuration.GetValue<int>("Jwt:ExpiresInMinutes");

        if (expiresInMinutes <= 0)
        {
            expiresInMinutes = 120;
        }

        return TimeSpan.FromMinutes(expiresInMinutes);
    }

}