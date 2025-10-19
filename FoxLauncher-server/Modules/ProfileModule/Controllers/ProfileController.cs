using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.ProfileModule.Data;
using FoxLauncher.Modules.ProfileModule.Models;

namespace FoxLauncher.Modules.ProfileModule.Controllers
{
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

        // GET /api/profiles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Profile>>> GetProfiles()
        {
            // В реальности, возможно, нужно фильтровать только публичные профили (IsPublic = true)
            // или профили, доступные аутентифицированному пользователю.
            // Пока получаем все.
            var profiles = await _context.Profiles
                .Where(p => p.IsPublic) // Фильтруем только публичные профили
                .Include(p => p.Versions) // Включаем версии
                .ToListAsync();
            return Ok(profiles);
        }

        // GET /api/profiles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Profile>> GetProfile(int id)
        {
            // В реальности, возможно, нужно проверять, что профиль публичный или принадлежит пользователю.
            var profile = await _context.Profiles
                .Where(p => p.Id == id && p.IsPublic) // Проверяем, что профиль публичный
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Files) // Включаем версии и файлы версий
                .Include(p => p.DefaultVersion) // Включаем версию по умолчанию
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        // GET /api/versions/{id}
        [HttpGet("versions/{id}")]
        [Route("api/versions/{id}")] // Альтернативный маршрут для получения версии напрямую
        public async Task<ActionResult<Models.Version>> GetVersion(int id)
        {
            // В реальности, возможно, нужно проверять, что версия принадлежит публичному профилю.
            var version = await _context.Versions
                .Where(v => v.Id == id)
                .Include(v => v.Profile).ThenInclude(p => p!.Versions.Where(vv => vv.Id == id)) // Включаем только запрашиваемую версию в профиле (опционально)
                .Include(v => v.Files) // Включаем файлы версии
                .FirstOrDefaultAsync();

            if (version == null)
            {
                return NotFound();
            }

            // Убедимся, что профиль, к которому принадлежит версия, публичный
            if (version.Profile != null && !version.Profile.IsPublic)
            {
                return NotFound(); // Или Forbid(), если нужна проверка аутентификации
            }

            return Ok(version);
        }
    }
}