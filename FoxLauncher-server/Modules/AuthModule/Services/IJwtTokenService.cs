using FoxLauncher.Modules.AuthModule.Models;

namespace FoxLauncher.Modules.AuthModule.Services
{
    public interface IJwtTokenService
    {
        /// <summary>
        /// Генерирует JWT токен для указанного пользователя.
        /// </summary>
        /// <param name="user">Пользователь, для которого генерируется токен.</param>
        /// <returns>Строка JWT токена.</returns>
        Task<string> GenerateTokenAsync(User user);
    }
}