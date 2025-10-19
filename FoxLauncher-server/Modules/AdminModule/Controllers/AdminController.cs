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
    [Route("api/admin")] // Базовый путь для админ-панели
    [Authorize(Roles = "Admin")] // Защищаем все эндпоинты контроллера с помощью ролей
    public class AdminController : ControllerBase
    {
        private readonly ProfileDbContext _context; // Используем ProfileDbContext
        private readonly ILogger<AdminController> _logger;

        public AdminController(ProfileDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
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
            _logger.LogInformation("Admin {AdminId} requested all profiles.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var profiles = await _context.Profiles.ToListAsync();
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
            _logger.LogInformation("Admin {AdminId} requested profile with ID {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            var profile = await _context.Profiles
                .Include(p => p.Versions) // Включаем связанные версии
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
            {
                _logger.LogWarning("Admin {AdminId} requested non-existent profile with ID {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return NotFound();
            }

            return Ok(profile);
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

            _logger.LogInformation("Admin {AdminId} is creating a new profile.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Profile {ProfileId} created successfully by admin {AdminId}.", profile.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, profile);
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

            _logger.LogInformation("Admin {AdminId} is updating profile with ID {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            _context.Entry(profile).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Profile {ProfileId} updated successfully by admin {AdminId}.", profile.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!ProfileExists(id))
                {
                    _logger.LogWarning("Admin {AdminId} attempted to update non-existent profile with ID {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error while updating profile {ProfileId} by admin {AdminId}.", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    throw; // Перебрасываем исключение, чтобы вызвать глобальный обработчик ошибок, если таковой имеется
                }
            }

            return NoContent();
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
            _logger.LogInformation("Admin {AdminId} is attempting to delete profile with ID {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            var profile = await _context.Profiles.FindAsync(id);
            if (profile == null)
            {
                _logger.LogWarning("Admin {AdminId} attempted to delete non-existent profile with ID {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return NotFound();
            }

            _context.Profiles.Remove(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Profile {ProfileId} deleted successfully by admin {AdminId}.", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return NoContent();
        }

        private bool ProfileExists(int id)
        {
            return _context.Profiles.Any(e => e.Id == id);
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
            _logger.LogInformation("Admin {AdminId} requested all versions.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var versions = await _context.Versions
                .Include(v => v.Profile) // Включаем связанный профиль
                .ToListAsync();
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
            _logger.LogInformation("Admin {AdminId} requested version with ID {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            var version = await _context.Versions
                .Include(v => v.Profile) // Включаем связанный профиль
                .Include(v => v.Files)   // Включаем связанные файлы
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null)
            {
                _logger.LogWarning("Admin {AdminId} requested non-existent version with ID {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return NotFound();
            }

            return Ok(version);
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

            // Проверить, существует ли профиль, к которому привязывается версия
            var profileExists = await _context.Profiles.AnyAsync(p => p.Id == version.ProfileId);
            if (!profileExists)
            {
                _logger.LogWarning("Admin {AdminId} attempted to create a version for a non-existent profile ID {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, version.ProfileId);
                return BadRequest("Profile not found.");
            }

            _logger.LogInformation("Admin {AdminId} is creating a new version for profile {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, version.ProfileId);
            _context.Versions.Add(version);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Version {VersionId} created successfully by admin {AdminId}.", version.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return CreatedAtAction(nameof(GetVersion), new { id = version.Id }, version);
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

            // Проверить, существует ли профиль, к которому привязывается версия
            var profileExists = await _context.Profiles.AnyAsync(p => p.Id == version.ProfileId);
            if (!profileExists)
            {
                _logger.LogWarning("Admin {AdminId} attempted to update version {VersionId} linked to a non-existent profile ID {ProfileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id, version.ProfileId);
                return BadRequest("Profile not found.");
            }

            _logger.LogInformation("Admin {AdminId} is updating version with ID {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            _context.Entry(version).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Version {VersionId} updated successfully by admin {AdminId}.", version.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!VersionExists(id))
                {
                    _logger.LogWarning("Admin {AdminId} attempted to update non-existent version with ID {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error while updating version {VersionId} by admin {AdminId}.", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    throw;
                }
            }

            return NoContent();
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
            _logger.LogInformation("Admin {AdminId} is attempting to delete version with ID {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            var version = await _context.Versions.FindAsync(id);
            if (version == null)
            {
                _logger.LogWarning("Admin {AdminId} attempted to delete non-existent version with ID {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return NotFound();
            }

            _context.Versions.Remove(version);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Version {VersionId} deleted successfully by admin {AdminId}.", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return NoContent();
        }

        private bool VersionExists(int id)
        {
            return _context.Versions.Any(e => e.Id == id);
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
            _logger.LogInformation("Admin {AdminId} requested all files.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var files = await _context.GameFiles
                .Include(f => f.Version) // Включаем связанную версию
                .ToListAsync();
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
            _logger.LogInformation("Admin {AdminId} requested file with ID {FileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            var file = await _context.GameFiles
                .Include(f => f.Version) // Включаем связанную версию
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
            {
                _logger.LogWarning("Admin {AdminId} requested non-existent file with ID {FileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return NotFound();
            }

            return Ok(file);
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

            // Проверить, существует ли версия, к которой привязывается файл
            var versionExists = await _context.Versions.AnyAsync(v => v.Id == file.VersionId);
            if (!versionExists)
            {
                _logger.LogWarning("Admin {AdminId} attempted to create a file for a non-existent version ID {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, file.VersionId);
                return BadRequest("Version not found.");
            }

            _logger.LogInformation("Admin {AdminId} is creating a new file for version {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, file.VersionId);
            _context.GameFiles.Add(file);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File {FileId} created successfully by admin {AdminId}.", file.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return CreatedAtAction(nameof(GetFile), new { id = file.Id }, file);
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

            // Проверить, существует ли версия, к которой привязывается файл
            var versionExists = await _context.Versions.AnyAsync(v => v.Id == file.VersionId);
            if (!versionExists)
            {
                _logger.LogWarning("Admin {AdminId} attempted to update file {FileId} linked to a non-existent version ID {VersionId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id, file.VersionId);
                return BadRequest("Version not found.");
            }

            _logger.LogInformation("Admin {AdminId} is updating file with ID {FileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            _context.Entry(file).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("File {FileId} updated successfully by admin {AdminId}.", file.Id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!FileExists(id))
                {
                    _logger.LogWarning("Admin {AdminId} attempted to update non-existent file with ID {FileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error while updating file {FileId} by admin {AdminId}.", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    throw;
                }
            }

            return NoContent();
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
            _logger.LogInformation("Admin {AdminId} is attempting to delete file with ID {FileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
            var file = await _context.GameFiles.FindAsync(id);
            if (file == null)
            {
                _logger.LogWarning("Admin {AdminId} attempted to delete non-existent file with ID {FileId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
                return NotFound();
            }

            _context.GameFiles.Remove(file);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File {FileId} deleted successfully by admin {AdminId}.", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return NoContent();
        }

        private bool FileExists(int id)
        {
            return _context.GameFiles.Any(e => e.Id == id);
        }
    }
}