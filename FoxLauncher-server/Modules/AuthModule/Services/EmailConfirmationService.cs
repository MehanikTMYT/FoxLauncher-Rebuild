using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Models;

namespace FoxLauncher.Modules.AuthModule.Services
{
    public class EmailConfirmationService : IEmailConfirmationService
    {
        private readonly UserManager<User> _userManager;
        private readonly AuthDbContext _context; // Для обновления EmailConfirmed
        private readonly ILogger<EmailConfirmationService> _logger;

        public EmailConfirmationService(UserManager<User> userManager, AuthDbContext context, ILogger<EmailConfirmationService> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<bool> GenerateConfirmationTokenAsync(string userId)
        {
            _logger.LogDebug("Generating email confirmation token for user {UserId}.", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                user.EmailConfirmationToken = token;
                user.EmailTokenExpiry = DateTime.UtcNow.AddHours(24); // Устанавливаем срок действия токена

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Email confirmation token generated and saved for user {UserId}.", userId);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to save email confirmation token for user {UserId}. Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogWarning("Attempted to generate confirmation token for non-existent user {UserId}.", userId);
            }
            return false;
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            _logger.LogDebug("Confirming email for user {UserId} with provided token.", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    // Убедимся, что EmailConfirmed установлен в true
                    // UserManager.ConfirmEmailAsync *должен* сам обновить это поле в БД и в сущности.
                    // Проверим, нужно ли ручное обновление.
                    // В большинстве случаев, если Identity настроена правильно, ручное обновление НЕ требуется.
                    // Однако, если по какой-то причине оно не обновляется, можно добавить:
                    // if (!user.EmailConfirmed) { user.EmailConfirmed = true; await _userManager.UpdateAsync(user); }
                    // Но лучше сначала убедиться, что Identity работает корректно.

                    // Проверим, обновлен ли статус в БД через контекст
                    // var userFromDb = await _context.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(u => u.EmailConfirmed).FirstOrDefaultAsync();
                    // if (!userFromDb) { ... } // Если не обновилось, делаем ручное обновление

                    // В простейшем случае, если Identity работает корректно, просто логгируем успех.
                    _logger.LogInformation("Email confirmed successfully for user {UserId}.", userId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Email confirmation failed for user {UserId} with token. Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogWarning("Attempted to confirm email for non-existent user {UserId}.", userId);
            }
            return false;
        }
    }
}