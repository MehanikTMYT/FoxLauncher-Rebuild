using FoxLauncher.Modules.AuthModule.Models;

namespace FoxLauncher.Modules.AuthModule.Services
{
    public interface IAuthService
    {
        Task<User?> FindUserAsync(string usernameOrEmail, string password);
        Task<bool> CreateUserAsync(string username, string email, string password);
    }
}