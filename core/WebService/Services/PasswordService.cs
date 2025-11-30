using System.Security.Cryptography;
using System.Text;

namespace WebService.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = password + GetSalt();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }

    private static string GetSalt()
    {
        // Simple salt for this implementation - in production, use per-user salts
        return "lightview_salt_2024";
    }
}