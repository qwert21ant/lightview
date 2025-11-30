using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Models;
using SharedContracts.Auth;
using WebService.Services;

namespace WebService.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UserInfo?> GetUserInfoAsync(Guid userId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public AuthService(AppDbContext context, IPasswordService passwordService, IJwtService jwtService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        
        return new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24), // Match JWT expiration
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username
            }
        };
    }

    public async Task<UserInfo?> GetUserInfoAsync(Guid userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
            return null;

        return new UserInfo
        {
            Id = user.Id,
            Username = user.Username
        };
    }
}