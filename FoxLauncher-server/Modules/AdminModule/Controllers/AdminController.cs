using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.ProfileModule.Data; // Используем ProfileDbContext
using FoxLauncher.Modules.ProfileModule.Models; // Используем модели из ProfileModule

namespace FoxLauncher.Modules.AdminModule.Controllers
{
    [ApiController]
    [Route("api/admin")] // Базовый путь для админ-панели
    [Authorize] // Защищаем все эндпоинты контроллера
    public class AdminController : ControllerBase
    {
        private readonly ProfileDbContext _context; // Используем ProfileDbContext
        private readonly ILogger<AdminController> _logger;

        public AdminController(ProfileDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Проверка администратора (псевдо-логика, т.к. IdentityUser не имеет ролей напрямую в этом примере)
        private bool IsAdmin()
        {
            // В реальности, используйте систему ролей ASP.NET Core Identity
            // var role = User.FindFirst(ClaimTypes.Role)?.Value;
            // return role == "Administrator";
            // Или проверяйте кастомный claim, установленный в AuthService при логине админа
            var isAdminClaim = User.FindFirst("IsAdmin")?.Value; // Пример кастомного claim
            return string.Equals(isAdminClaim, "true", StringComparison.OrdinalIgnoreCase);
        }

        // GET /api/admin/profiles
        [HttpGet("profiles")]
        public async Task<ActionResult<IEnumerable<Profile>>> GetProfiles()
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var profiles = await _context.Profiles.ToListAsync();
            return Ok(profiles);
        }

        // GET /api/admin/profiles/{id}
        [HttpGet("profiles/{id}")]
        public async Task<ActionResult<Profile>> GetProfile(int id)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var profile = await _context.Profiles
                .Include(p => p.Versions) // Включаем связанные версии
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        // POST /api/admin/profiles
        [HttpPost("profiles")]
        public async Task<ActionResult<Profile>> CreateProfile(Profile profile)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, profile);
        }

        // PUT /api/admin/profiles/{id}
        [HttpPut("profiles/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, Profile profile)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != profile.Id)
            {
                return BadRequest();
            }

            _context.Entry(profile).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProfileExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE /api/admin/profiles/{id}
        [HttpDelete("profiles/{id}")]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var profile = await _context.Profiles.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }

            _context.Profiles.Remove(profile);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProfileExists(int id)
        {
            return _context.Profiles.Any(e => e.Id == id);
        }

        // GET /api/admin/versions
        [HttpGet("versions")]
        public async Task<ActionResult<IEnumerable<Version>>> GetVersions()
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var versions = await _context.Versions
                .Include(v => v.Profile) // Включаем связанный профиль
                .ToListAsync();
            return Ok(versions);
        }

        // GET /api/admin/versions/{id}
        [HttpGet("versions/{id}")]
        public async Task<ActionResult<Version>> GetVersion(int id)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var version = await _context.Versions
                .Include(v => v.Profile) // Включаем связанный профиль
                .Include(v => v.Files)   // Включаем связанные файлы
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null)
            {
                return NotFound();
            }

            return Ok(version);
        }

        // POST /api/admin/versions
        [HttpPost("versions")]
        public async Task<ActionResult<ProfileModule.Models.Version>> CreateVersion(ProfileModule.Models.Version version)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверить, существует ли профиль, к которому привязывается версия
            var profileExists = await _context.Profiles.AnyAsync(p => p.Id == version.ProfileId);
            if (!profileExists)
            {
                return BadRequest("Profile not found.");
            }

            _context.Versions.Add(version);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVersion), new { id = version.Id }, version);
        }

        // PUT /api/admin/versions/{id}
        [HttpPut("versions/{id}")]
        public async Task<IActionResult> UpdateVersion(int id, ProfileModule.Models.Version version)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != version.Id)
            {
                return BadRequest();
            }

            // Проверить, существует ли профиль, к которому привязывается версия
            var profileExists = await _context.Profiles.AnyAsync(p => p.Id == version.ProfileId);
            if (!profileExists)
            {
                return BadRequest("Profile not found.");
            }

            _context.Entry(version).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VersionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE /api/admin/versions/{id}
        [HttpDelete("versions/{id}")]
        public async Task<IActionResult> DeleteVersion(int id)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var version = await _context.Versions.FindAsync(id);
            if (version == null)
            {
                return NotFound();
            }

            _context.Versions.Remove(version);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VersionExists(int id)
        {
            return _context.Versions.Any(e => e.Id == id);
        }

        // GET /api/admin/files
        [HttpGet("files")]
        public async Task<ActionResult<IEnumerable<GameFile>>> GetFiles()
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var files = await _context.GameFiles
                .Include(f => f.Version) // Включаем связанную версию
                .ToListAsync();
            return Ok(files);
        }

        // GET /api/admin/files/{id}
        [HttpGet("files/{id}")]
        public async Task<ActionResult<GameFile>> GetFile(int id)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var file = await _context.GameFiles
                .Include(f => f.Version) // Включаем связанную версию
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
            {
                return NotFound();
            }

            return Ok(file);
        }

        // POST /api/admin/files
        [HttpPost("files")]
        public async Task<ActionResult<GameFile>> CreateFile(GameFile file)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверить, существует ли версия, к которой привязывается файл
            var versionExists = await _context.Versions.AnyAsync(v => v.Id == file.VersionId);
            if (!versionExists)
            {
                return BadRequest("Version not found.");
            }

            _context.GameFiles.Add(file);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFile), new { id = file.Id }, file);
        }

        // PUT /api/admin/files/{id}
        [HttpPut("files/{id}")]
        public async Task<IActionResult> UpdateFile(int id, GameFile file)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != file.Id)
            {
                return BadRequest();
            }

            // Проверить, существует ли версия, к которой привязывается файл
            var versionExists = await _context.Versions.AnyAsync(v => v.Id == file.VersionId);
            if (!versionExists)
            {
                return BadRequest("Version not found.");
            }

            _context.Entry(file).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FileExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE /api/admin/files/{id}
        [HttpDelete("files/{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required.");
            }

            var file = await _context.GameFiles.FindAsync(id);
            if (file == null)
            {
                return NotFound();
            }

            _context.GameFiles.Remove(file);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FileExists(int id)
        {
            return _context.GameFiles.Any(e => e.Id == id);
        }
    }
}