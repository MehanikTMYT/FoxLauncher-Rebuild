
namespace FoxLauncher.Modules.AuthModule.Services
{
    public interface IAuthlibService
    {
        /// <summary>
        /// Получает профиль пользователя по его UUID.
        /// </summary>
        /// <param name="uuid">UUID пользователя.</param>
        /// <returns>Объект User или null, если не найден.</returns>
        // Изменяем возвращаемый тип на CabinetModule.Models.User
        Task<CabinetModule.Models.User?> GetUserByUuidAsync(string uuid);

        /// <summary>
        /// Проверяет, вошёл ли пользователь на сервер игры (hasJoined).
        /// </summary>
        /// <param name="username">Имя пользователя.</param>
        /// <param name="serverId">ID сервера (опционально).</param>
        /// <param name="selectedProfile">UUID выбранного профиля (опционально).</param>
        /// <returns>Объект результата hasJoined (включая профиль, текстуры, подпись) или null, если проверка не пройдена.</returns>
        Task<object?> ValidateHasJoinedAsync(string username, string? serverId, string? selectedProfile);

        /// <summary>
        /// Подтверждает соединение игрока с сервером игры (join).
        /// </summary>
        /// <param name="accessToken">JWT токен пользователя.</param>
        /// <param name="selectedProfile">UUID выбранного профиля.</param>
        /// <param name="serverId">ID сервера игры.</param>
        /// <returns>True, если подтверждение успешно, иначе False.</returns>
        Task<bool> ConfirmJoinAsync(string accessToken, string selectedProfile, string serverId);

        // Другие методы, если потребуется
    }
}