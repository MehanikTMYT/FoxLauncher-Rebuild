using FoxLauncher.Modules.ProfileModule.Data;
using FoxLauncher.Modules.ProfileModule.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations; 

namespace FoxLauncher.Modules.ProfileModule.Controllers
{
    /// <summary>
    /// Контроллер для получения информации о публичных профилях и версиях.
    /// </summary>
    [ApiController]
    [Route("api/profiles")] // Базовый путь для API профилей
    public class ProfileController : ControllerBase
    {
        private readonly ProfileDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ProfileDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Получить список публичных профилей.
        /// </summary>
        /// <returns>Список публичных профилей с их версиями.</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Получить список публичных профилей",
            Description = "Возвращает список всех профилей, у которых установлен флаг IsPublic. Включает информацию о версиях для каждого профиля."
        )]
        [ProducesResponseType(typeof(IEnumerable<Profile>), 200)]
        public async Task<ActionResult<IEnumerable<Profile>>> GetProfiles()
        {
            _logger.LogInformation("Request for public profiles received.");
            var profiles = await _context.Profiles
                .Where(p => p.IsPublic) // Фильтруем только публичные профили
                .Include(p => p.Versions) // Включаем версии
                .ToListAsync();
            return Ok(profiles);
        }

        /// <summary>
        /// Получить профиль по ID.
        /// </summary>
        /// <param name="id">ID профиля.</param>
        /// <returns>Данные профиля, включая версии, файлы версий и версию по умолчанию (если она установлена).</returns>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Получить профиль по ID",
            Description = "Возвращает данные конкретного профиля по его ID, если он является публичным."
        )]
        [ProducesResponseType(typeof(Profile), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Profile>> GetProfile(int id)
        {
            _logger.LogInformation("Request for profile with ID {ProfileId} received.", id);
            var profile = await _context.Profiles
                .Where(p => p.Id == id && p.IsPublic) // Проверяем, что профиль публичный
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Files) // Включаем версии и файлы версий
                .Include(p => p.DefaultVersion) // Включаем версию по умолчанию
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                _logger.LogWarning("Request for non-existent or non-public profile with ID {ProfileId} received.", id);
                return NotFound();
            }

            return Ok(profile);
        }

        /// <summary>
        /// Получить версию по ID.
        /// </summary>
        /// <param name="id">ID версии.</param>
        /// <returns>Данные версии, включая профиль, к которому она принадлежит, и связанные файлы.</returns>
        [HttpGet("versions/{id}")]
        [SwaggerOperation(
            Summary = "Получить версию по ID",
            Description = "Возвращает данные конкретной версии по её ID, если профиль, к которому она принадлежит, является публичным."
        )]
        [ProducesResponseType(typeof(Models.Version), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Models.Version>> GetVersion(int id)
        {
            _logger.LogInformation("Request for version with ID {VersionId} received.", id);
            var version = await _context.Versions
                .Where(v => v.Id == id)
                .Include(v => v.Profile).ThenInclude(p => p!.Versions.Where(vv => vv.Id == id)) // Включаем только запрашиваемую версию в профиле (опционально)
                .Include(v => v.Files) // Включаем файлы версии
                .FirstOrDefaultAsync();

            if (version == null)
            {
                _logger.LogWarning("Request for non-existent version with ID {VersionId} received.", id);
                return NotFound();
            }

            // Убедимся, что профиль, к которому принадлежит версия, публичный
            if (version.Profile != null && !version.Profile.IsPublic)
            {
                _logger.LogWarning("Request for version {VersionId} from non-public profile {ProfileId} received.", id, version.Profile.Id);
                return NotFound(); // Или Forbid(), если нужна проверка аутентификации
            }

            return Ok(version);
        }
    }
}