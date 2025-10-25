namespace FoxLauncher.Modules.AuthModule.Services
{
    public interface IEmailConfirmationService
    {
        /// <summary>
        /// Генерирует токен подтверждения email для пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <returns>True, если токен успешно сгенерирован и сохранен, иначе False.</returns>
        Task<bool> GenerateConfirmationTokenAsync(string userId);

        /// <summary>
        /// Подтверждает email пользователя по токену.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <param name="token">Токен подтверждения.</param>
        /// <returns>True, если подтверждение успешно, иначе False.</returns>
        Task<bool> ConfirmEmailAsync(string userId, string token);
    }
}