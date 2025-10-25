using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Models;

namespace FoxLauncher.Modules.AuthModule.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly AuthDbContext _context; // Возможно, больше не нужен в этом сервисе, если все операции через UserManager
        private readonly IConfiguration _configuration; // Возможно, больше не нужен в этом сервисе, если генерация токена вынесена

        public AuthService(UserManager<User> userManager, SignInManager<User> signInManager, AuthDbContext context, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
        }

        public async Task<User?> FindUserAsync(string usernameOrEmail, string password)
        {
            var user = await _userManager.FindByNameAsync(usernameOrEmail) ?? await _userManager.FindByEmailAsync(usernameOrEmail);
            if (user != null && await _userManager.CheckPasswordAsync(user, password))
            {
                return user;
            }
            return null;
        }

        public async Task<bool> CreateUserAsync(string username, string email, string password)
        {
            var user = new User { UserName = username, Email = email, Username = username, Uuid = Guid.NewGuid().ToString() }; // Генерируем UUID при создании
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Логика отправки письма подтверждения теперь будет в контроллере или отдельном сервисе
                // await GenerateEmailConfirmationTokenAsync(user.Id.ToString()); // Удалено из сервиса
            }
            return result.Succeeded;
        }

    }
}