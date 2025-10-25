using FoxLauncher.Modules.ProfileModule.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoxLauncher.Modules.AdminModule.Services
{
    /// <summary>
    /// Интерфейс сервиса для административных операций.
    /// </summary>
    public interface IAdminService
    {
        #region Profiles
        /// <summary>
        /// Получает список всех профилей.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <returns>Список профилей.</returns>
        Task<IEnumerable<Profile>> GetProfilesAsync(ILogger logger, string? userId);

        /// <summary>
        /// Получает профиль по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID профиля.</param>
        /// <returns>Результат IActionResult (Ok(Profile), NotFound).</returns>
        // Task<IActionResult> GetProfileAsync(ILogger logger, string? userId, int id); // <-- Закомментировано
        Task<ActionResult<Profile>> GetProfileAsync(ILogger logger, string? userId, int id); // <-- Новая сигнатура

        /// <summary>
        /// Создает новый профиль.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="profile">Данные профиля для создания.</param>
        /// <returns>Результат IActionResult (CreatedAtAction, BadRequest).</returns>
        // Task<IActionResult> CreateProfileAsync(ILogger logger, string? userId, Profile profile); // <-- Закомментировано
        Task<ActionResult<Profile>> CreateProfileAsync(ILogger logger, string? userId, Profile profile); // <-- Новая сигнатура

        /// <summary>
        /// Обновляет профиль по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID профиля.</param>
        /// <param name="profile">Новые данные профиля.</param>
        /// <returns>Результат IActionResult (NoContent, BadRequest, NotFound).</returns>
        // Task<IActionResult> UpdateProfileAsync(ILogger logger, string? userId, int id, Profile profile); // <-- Закомментировано
        Task<IActionResult> UpdateProfileAsync(ILogger logger, string? userId, int id, Profile profile); // <-- Остается IActionResult для NoContent/NotFound/BadRequest

        /// <summary>
        /// Удаляет профиль по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID профиля.</param>
        /// <returns>Результат IActionResult (NoContent, NotFound).</returns>
        Task<IActionResult> DeleteProfileAsync(ILogger logger, string? userId, int id); // <-- Остается IActionResult
        #endregion

        #region Versions
        /// <summary>
        /// Получает список всех версий.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <returns>Список версий.</returns>
        Task<IEnumerable<ProfileModule.Models.Version>> GetVersionsAsync(ILogger logger, string? userId);

        /// <summary>
        /// Получает версию по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID версии.</param>
        /// <returns>Результат IActionResult (Ok(Version), NotFound).</returns>
        // Task<IActionResult> GetVersionAsync(ILogger logger, string? userId, int id); // <-- Закомментировано
        Task<ActionResult<ProfileModule.Models.Version>> GetVersionAsync(ILogger logger, string? userId, int id); // <-- Новая сигнатура

        /// <summary>
        /// Создает новую версию.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="version">Данные версии для создания.</param>
        /// <returns>Результат IActionResult (CreatedAtAction, BadRequest).</returns>
        // Task<IActionResult> CreateVersionAsync(ILogger logger, string? userId, ProfileModule.Models.Version version); // <-- Закомментировано
        Task<ActionResult<ProfileModule.Models.Version>> CreateVersionAsync(ILogger logger, string? userId, ProfileModule.Models.Version version); // <-- Новая сигнатура

        /// <summary>
        /// Обновляет версию по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID версии.</param>
        /// <param name="version">Новые данные версии.</param>
        /// <returns>Результат IActionResult (NoContent, BadRequest, NotFound).</returns>
        Task<IActionResult> UpdateVersionAsync(ILogger logger, string? userId, int id, ProfileModule.Models.Version version); // <-- Остается IActionResult

        /// <summary>
        /// Удаляет версию по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID версии.</param>
        /// <returns>Результат IActionResult (NoContent, NotFound).</returns>
        Task<IActionResult> DeleteVersionAsync(ILogger logger, string? userId, int id); // <-- Остается IActionResult
        #endregion

        #region Files
        /// <summary>
        /// Получает список всех файлов.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <returns>Список файлов.</returns>
        Task<IEnumerable<GameFile>> GetFilesAsync(ILogger logger, string? userId);

        /// <summary>
        /// Получает файл по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID файла.</param>
        /// <returns>Результат IActionResult (Ok(GameFile), NotFound).</returns>
        // Task<IActionResult> GetFileAsync(ILogger logger, string? userId, int id); // <-- Закомментировано
        Task<ActionResult<GameFile>> GetFileAsync(ILogger logger, string? userId, int id); // <-- Новая сигнатура

        /// <summary>
        /// Создает новый файл.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="file">Данные файла для создания.</param>
        /// <returns>Результат IActionResult (CreatedAtAction, BadRequest).</returns>
        // Task<IActionResult> CreateFileAsync(ILogger logger, string? userId, GameFile file); // <-- Закомментировано
        Task<ActionResult<GameFile>> CreateFileAsync(ILogger logger, string? userId, GameFile file); // <-- Новая сигнатура

        /// <summary>
        /// Обновляет файл по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID файла.</param>
        /// <param name="file">Новые данные файла.</param>
        /// <returns>Результат IActionResult (NoContent, BadRequest, NotFound).</returns>
        Task<IActionResult> UpdateFileAsync(ILogger logger, string? userId, int id, GameFile file); // <-- Остается IActionResult

        /// <summary>
        /// Удаляет файл по ID.
        /// </summary>
        /// <param name="logger">Логгер.</param>
        /// <param name="userId">ID текущего пользователя (администратора).</param>
        /// <param name="id">ID файла.</param>
        /// <returns>Результат IActionResult (NoContent, NotFound).</returns>
        Task<IActionResult> DeleteFileAsync(ILogger logger, string? userId, int id); // <-- Остается IActionResult
        #endregion
    }
}