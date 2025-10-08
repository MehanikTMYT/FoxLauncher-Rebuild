using FoxLauncher.Modules.AuthModule.Models;

namespace FoxLauncher.Modules.AuthModule.Services
{
    public interface IAuthService
    {
        Task<User?> FindUserAsync(string usernameOrEmail, string password);
        Task<bool> CreateUserAsync(string username, string email, string password);
        Task<string> GenerateJwtTokenAsync(User user);
        Task<bool> ConfirmEmailAsync(string userId, string token);
        Task<bool> GenerateEmailConfirmationTokenAsync(string userId);
        // Другие методы аутентификации/авторизации
    }
}