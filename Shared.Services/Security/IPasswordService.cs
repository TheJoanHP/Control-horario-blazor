namespace Shared.Services.Security
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        string GenerateRandomPassword(int length = 12);
        bool IsPasswordStrong(string password);
        (bool IsValid, List<string> Errors) ValidatePasswordStrength(string password);
    }
}