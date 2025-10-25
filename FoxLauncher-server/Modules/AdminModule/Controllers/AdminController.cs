using FoxLauncher.Modules.AdminModule.Services;
using FoxLauncher.Modules.ProfileModule.Data;
using FoxLauncher.Modules.ProfileModule.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace FoxLauncher.Modules.AdminModule.Controllers
{
    /// <summary>
    /// Контроллер для административного управления профилями, версиями и файлами.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="AdminController"/>.
        /// </summary>
        /// <param name="adminService">Сервис для выполнения административных операций.</param>
        /// <param name="logger">Логгер для записи событий контроллера.</param>
        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// Получить список всех профилей.
        /// </summary>
        /// <returns>Список профилей.</returns>
        [HttpGet("profiles")]
        [SwaggerOperation(
            Summary = "Получить список всех профилей (только для администраторов)",
            Description = "Возвращает список всех профилей, доступных в системе."
        )]
        [ProducesResponseType(typeof(IEnumerable<Profile>), 200)]
        public async Task<ActionResult<IEnumerable<Profile>>> GetProfiles()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var profiles = await _adminService.GetProfilesAsync(_logger, userId);
            return Ok(profiles);
        }

        /// <summary>
        /// Получить профиль по ID.
        /// </summary>
        /// <param name="id">ID профиля.</param>
        /// <returns>Данные профиля.</returns>
        [HttpGet("profiles/{id}")]
        [SwaggerOperation(
            Summary = "Получить профиль по ID (только для администраторов)",
            Description = "Возвращает данные конкретного профиля по его уникальному идентификатору."
        )]
        [ProducesResponseType(typeof(Profile), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Profile>> GetProfile(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _adminService.GetProfileAsync(_logger, userId, id);
            return result;
        }

        /// <summary>
        /// Создать новый профиль.
        /// </summary>
        /// <param name="profile">Данные профиля для создания.</param>
        /// <returns>Созданный профиль.</returns>
        [HttpPost("profiles")]
        [SwaggerOperation(
            Summary = "Создать новый профиль (только для администраторов)",
            Description = "Создает новый профиль в системе."
        )]
        [ProducesResponseType(typeof(Profile), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<ActionResult<Profile>> CreateProfile([FromBody][SwaggerRequestBody("Данные профиля для создания.")] Profile profile)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Admin {AdminId} attempted to create a profile with invalid data.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return BadRequest(ModelState);
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _adminService.CreateProfileAsync(_logger, userId, profile);
            // Предполагаем, что сервис возвращает ActionResult<Profile> с Created или другим статусом
            return result;
        }

        /// <summary>
        /// Обновить профиль по ID.
        /// </summary>
        /// <param name="id">ID профиля для обновления.</param>
        /// <param name="profile">Новые данные профиля.</param>
        /// <returns>Результат операции.</returns>
        [HttpPut("profiles/{id}")]
        [SwaggerOperation(
            Summary = "Обновить профиль по ID (только для администраторов)",
            Description = "Обновляет данные существующего профиля по его уникальному идентификатору."
        )]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody][SwaggerRequestBody("Новые данные профиля.")] Profile profile)
        {
            if (id != profile.Id)
            {
                _logger.LogWarning("Admin {AdminId} attempted to update profile {ProfileId} with mismatched ID in payload.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Admin {AdminId} attempted to update profile {ProfileId} with invalid data.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return BadRequest(ModelState);
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _adminService.UpdateProfileAsync(_logger, userId, id, profile);
        }

        /// <summary>
        /// Удалить профиль по ID.
        /// </summary>
        /// <param name="id">ID профиля для удаления.</param>
        /// <returns>Результат операции.</returns>
        [HttpDelete("profiles/{id}")]
        [SwaggerOperation(
            Summary = "Удалить профиль по ID (только для администраторов)",
            Description = "Удаляет профиль из системы по его уникальному идентификатору."
        )]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _adminService.DeleteProfileAsync(_logger, userId, id);
        }

        /// <summary>
        /// Получить список всех версий.
        /// </summary>
        /// <returns>Список версий.</returns>
        [HttpGet("versions")]
        [SwaggerOperation(
            Summary = "Получить список всех версий (только для администраторов)",
            Description = "Возвращает список всех версий, доступных в системе."
        )]
        [ProducesResponseType(typeof(IEnumerable<ProfileModule.Models.Version>), 200)]
        public async Task<ActionResult<IEnumerable<ProfileModule.Models.Version>>> GetVersions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var versions = await _adminService.GetVersionsAsync(_logger, userId);
            return Ok(versions);
        }

        /// <summary>
        /// Получить версию по ID.
        /// </summary>
        /// <param name="id">ID версии.</param>
        /// <returns>Данные версии.</returns>
        [HttpGet("versions/{id}")]
        [SwaggerOperation(
            Summary = "Получить версию по ID (только для администраторов)",
            Description = "Возвращает данные конкретной версии по её уникальному идентификатору."
        )]
        [ProducesResponseType(typeof(ProfileModule.Models.Version), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProfileModule.Models.Version>> GetVersion(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _adminService.GetVersionAsync(_logger, userId, id);
            return result;
        }

        /// <summary>
        /// Создать новую версию.
        /// </summary>
        /// <param name="version">Данные версии для создания.</param>
        /// <returns>Созданная версия.</returns>
        [HttpPost("versions")]
        [SwaggerOperation(
            Summary = "Создать новую версию (только для администраторов)",
            Description = "Создает новую версию в системе."
        )]
        [ProducesResponseType(typeof(ProfileModule.Models.Version), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<ActionResult<ProfileModule.Models.Version>> CreateVersion([FromBody][SwaggerRequestBody("Данные версии для создания.")] ProfileModule.Models.Version version)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Admin {AdminId} attempted to create a version with invalid data.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return BadRequest(ModelState);
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _adminService.CreateVersionAsync(_logger, userId, version);
            // Предполагаем, что сервис возвращает ActionResult<Version> с Created или другим статусом
            return result;
        }

        /// <summary>
        /// Обновить версию по ID.
        /// </summary>
        /// <param name="id">ID версии для обновления.</param>
        /// <param name="version">Новые данные версии.</param>
        /// <returns>Результат операции.</returns>
        [HttpPut("versions/{id}")]
        [SwaggerOperation(
            Summary = "Обновить версию по ID (только для администраторов)",
            Description = "Обновляет данные существующей версии по её уникальному идентификатору."
        )]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateVersion(int id, [FromBody][SwaggerRequestBody("Новые данные версии.")] ProfileModule.Models.Version version)
        {
            if (id != version.Id)
            {
                _logger.LogWarning("Admin {AdminId} attempted to update version {VersionId} with mismatched ID in payload.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Admin {AdminId} attempted to update version {VersionId} with invalid data.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return BadRequest(ModelState);
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _adminService.UpdateVersionAsync(_logger, userId, id, version);
        }

        /// <summary>
        /// Удалить версию по ID.
        /// </summary>
        /// <param name="id">ID версии для удаления.</param>
        /// <returns>Результат операции.</returns>
        [HttpDelete("versions/{id}")]
        [SwaggerOperation(
            Summary = "Удалить версию по ID (только для администраторов)",
            Description = "Удаляет версию из системы по её уникальному идентификатору."
        )]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteVersion(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _adminService.DeleteVersionAsync(_logger, userId, id);
        }

        /// <summary>
        /// Получить список всех файлов.
        /// </summary>
        /// <returns>Список файлов.</returns>
        [HttpGet("files")]
        [SwaggerOperation(
            Summary = "Получить список всех файлов (только для администраторов)",
            Description = "Возвращает список всех файлов, доступных в системе."
        )]
        [ProducesResponseType(typeof(IEnumerable<GameFile>), 200)]
        public async Task<ActionResult<IEnumerable<GameFile>>> GetFiles()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var files = await _adminService.GetFilesAsync(_logger, userId);
            return Ok(files);
        }

        /// <summary>
        /// Получить файл по ID.
        /// </summary>
        /// <param name="id">ID файла.</param>
        /// <returns>Данные файла.</returns>
        [HttpGet("files/{id}")]
        [SwaggerOperation(
            Summary = "Получить файл по ID (только для администраторов)",
            Description = "Возвращает данные конкретного файла по его уникальному идентификатору."
        )]
        [ProducesResponseType(typeof(GameFile), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GameFile>> GetFile(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _adminService.GetFileAsync(_logger, userId, id);
            return result;
        }

        /// <summary>
        /// Создать новый файл.
        /// </summary>
        /// <param name="file">Данные файла для создания.</param>
        /// <returns>Созданный файл.</returns>
        [HttpPost("files")]
        [SwaggerOperation(
            Summary = "Создать новый файл (только для администраторов)",
            Description = "Создает новый файл в системе."
        )]
        [ProducesResponseType(typeof(GameFile), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<ActionResult<GameFile>> CreateFile([FromBody][SwaggerRequestBody("Данные файла для создания.")] GameFile file)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Admin {AdminId} attempted to create a file with invalid data.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return BadRequest(ModelState);
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _adminService.CreateFileAsync(_logger, userId, file);
            // Предполагаем, что сервис возвращает ActionResult<GameFile> с Created или другим статусом
            return result;
        }

        /// <summary>
        /// Обновить файл по ID.
        /// </summary>
        /// <param name="id">ID файла для обновления.</param>
        /// <param name="file">Новые данные файла.</param>
        /// <returns>Результат операции.</returns>
        [HttpPut("files/{id}")]
        [SwaggerOperation(
            Summary = "Обновить файл по ID (только для администраторов)",
            Description = "Обновляет данные существующего файла по его уникальному идентификатору."
        )]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateFile(int id, [FromBody][SwaggerRequestBody("Новые данные файла.")] GameFile file)
        {
            if (id != file.Id)
            {
                _logger.LogWarning("Admin {AdminId} attempted to update file {FileId} with mismatched ID in payload.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Admin {AdminId} attempted to update file {FileId} with invalid data.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return BadRequest(ModelState);
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _adminService.UpdateFileAsync(_logger, userId, id, file);
        }

        /// <summary>
        /// Удалить файл по ID.
        /// </summary>
        /// <param name="id">ID файла для удаления.</param>
        /// <returns>Результат операции.</returns>
        [HttpDelete("files/{id}")]
        [SwaggerOperation(
            Summary = "Удалить файл по ID (только для администраторов)",
            Description = "Удаляет файл из системы по его уникальному идентификатору."
        )]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _adminService.DeleteFileAsync(_logger, userId, id);
        }
    }
}