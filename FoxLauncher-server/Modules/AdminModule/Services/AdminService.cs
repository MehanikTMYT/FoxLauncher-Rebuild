using FoxLauncher.Modules.ProfileModule.Data;
using FoxLauncher.Modules.ProfileModule.Models;
using Microsoft.AspNetCore.Mvc; // <-- Для ActionResult<T>, IActionResult и их реализаций
using Microsoft.EntityFrameworkCore; // <-- Для Include, FirstOrDefaultAsync и т.д.

namespace FoxLauncher.Modules.AdminModule.Services
{
    /// <summary>
    /// Реализация сервиса для административных операций.
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly ProfileDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="AdminService"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных профилей.</param>
        public AdminService(ProfileDbContext context)
        {
            _context = context;
        }

        #region Profiles
        /// <inheritdoc/>
        public async Task<IEnumerable<Profile>> GetProfilesAsync(ILogger logger, string? userId)
        {
            logger.LogInformation("Admin {AdminId} requested all profiles via service.", userId);
            return await _context.Profiles.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<ActionResult<Profile>> GetProfileAsync(ILogger logger, string? userId, int id)
        {
            logger.LogInformation("Admin {AdminId} requested profile with ID {ProfileId} via service.", userId, id);
            var profile = await _context.Profiles
                .Include(p => p.Versions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
            {
                logger.LogWarning("Admin {AdminId} requested non-existent profile with ID {ProfileId} via service.", userId, id);
                // Возвращаем NotFoundObjectResult вместо NotFound(), так как метод не наследует ControllerBase
                return new NotFoundObjectResult($"Profile with ID {id} not found.");
            }

            // Возвращаем сам объект, ActionResult<T> позволяет это
            return profile;
        }

        /// <inheritdoc/>
        public async Task<ActionResult<Profile>> CreateProfileAsync(ILogger logger, string? userId, Profile profile)
        {
            if (profile == null)
            {
                logger.LogWarning("Admin {AdminId} attempted to create a profile with null data via service.", userId);
                // Возвращаем BadRequestObjectResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestObjectResult("Profile data is required.");
            }

            logger.LogInformation("Admin {AdminId} is creating a new profile via service.", userId);
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            logger.LogInformation("Profile {ProfileId} created successfully by admin {AdminId} via service.", profile.Id, userId);
            // Возвращаем сам объект, контроллер решит, как его представить (Ok или Created)
            return profile;
        }

        /// <inheritdoc/>
        public async Task<IActionResult> UpdateProfileAsync(ILogger logger, string? userId, int id, Profile profile)
        {
            if (profile == null || id != profile.Id)
            {
                logger.LogWarning("Admin {AdminId} attempted to update profile {ProfileId} with invalid data (mismatched ID or null) via service.", userId, id);
                // Возвращаем BadRequestResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestResult();
            }

            logger.LogInformation("Admin {AdminId} is updating profile with ID {ProfileId} via service.", userId, id);
            _context.Entry(profile).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                logger.LogInformation("Profile {ProfileId} updated successfully by admin {AdminId} via service.", profile.Id, userId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!ProfileExists(id))
                {
                    logger.LogWarning("Admin {AdminId} attempted to update non-existent profile with ID {ProfileId} via service.", userId, id);
                    // Возвращаем NotFoundResult вместо NotFound(), так как метод не наследует ControllerBase
                    return new NotFoundResult();
                }
                else
                {
                    logger.LogError(ex, "Concurrency error while updating profile {ProfileId} by admin {AdminId} via service.", id, userId);
                    // Re-throw to let global exception handler deal with it, or return StatusCode(500)
                    throw; // Пусть обрабатывает глобальный обработчик
                }
            }

            // Возвращаем NoContentResult вместо NoContent(), так как метод не наследует ControllerBase
            return new NoContentResult();
        }

        /// <inheritdoc/>
        public async Task<IActionResult> DeleteProfileAsync(ILogger logger, string? userId, int id)
        {
            logger.LogInformation("Admin {AdminId} is attempting to delete profile with ID {ProfileId} via service.", userId, id);
            var profile = await _context.Profiles.FindAsync(id);
            if (profile == null)
            {
                logger.LogWarning("Admin {AdminId} attempted to delete non-existent profile with ID {ProfileId} via service.", userId, id);
                // Возвращаем NotFoundResult вместо NotFound(), так как метод не наследует ControllerBase
                return new NotFoundResult();
            }

            _context.Profiles.Remove(profile);
            await _context.SaveChangesAsync();

            logger.LogInformation("Profile {ProfileId} deleted successfully by admin {AdminId} via service.", id, userId);
            // Возвращаем NoContentResult вместо NoContent(), так как метод не наследует ControllerBase
            return new NoContentResult();
        }

        private bool ProfileExists(int id)
        {
            return _context.Profiles.Any(e => e.Id == id);
        }
        #endregion

        #region Versions
        /// <inheritdoc/>
        public async Task<IEnumerable<ProfileModule.Models.Version>> GetVersionsAsync(ILogger logger, string? userId)
        {
            logger.LogInformation("Admin {AdminId} requested all versions via service.", userId);
            return await _context.Versions
                .Include(v => v.Profile)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<ActionResult<ProfileModule.Models.Version>> GetVersionAsync(ILogger logger, string? userId, int id)
        {
            logger.LogInformation("Admin {AdminId} requested version with ID {VersionId} via service.", userId, id);
            var version = await _context.Versions
                .Include(v => v.Profile)
                .Include(v => v.Files)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null)
            {
                logger.LogWarning("Admin {AdminId} requested non-existent version with ID {VersionId} via service.", userId, id);
                // Возвращаем NotFoundObjectResult вместо NotFound(), так как метод не наследует ControllerBase
                return new NotFoundObjectResult($"Version with ID {id} not found.");
            }

            // Возвращаем сам объект
            return version;
        }

        /// <inheritdoc/>
        public async Task<ActionResult<ProfileModule.Models.Version>> CreateVersionAsync(ILogger logger, string? userId, ProfileModule.Models.Version version)
        {
            if (version == null)
            {
                logger.LogWarning("Admin {AdminId} attempted to create a version with null data via service.", userId);
                // Возвращаем BadRequestObjectResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestObjectResult("Version data is required.");
            }

            // Проверить, существует ли профиль, к которому привязывается версия
            var profileExists = await _context.Profiles.AnyAsync(p => p.Id == version.ProfileId);
            if (!profileExists)
            {
                logger.LogWarning("Admin {AdminId} attempted to create a version for a non-existent profile ID {ProfileId} via service.", userId, version.ProfileId);
                // Возвращаем BadRequestObjectResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestObjectResult("Profile not found.");
            }

            logger.LogInformation("Admin {AdminId} is creating a new version for profile {ProfileId} via service.", userId, version.ProfileId);
            _context.Versions.Add(version);
            await _context.SaveChangesAsync();

            logger.LogInformation("Version {VersionId} created successfully by admin {AdminId} via service.", version.Id, userId);
            // Возвращаем сам объект
            return version;
        }

        /// <inheritdoc/>
        public async Task<IActionResult> UpdateVersionAsync(ILogger logger, string? userId, int id, ProfileModule.Models.Version version)
        {
            if (version == null || id != version.Id)
            {
                logger.LogWarning("Admin {AdminId} attempted to update version {VersionId} with invalid data (mismatched ID or null) via service.", userId, id);
                // Возвращаем BadRequestResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestResult();
            }

            // Проверить, существует ли профиль, к которому привязывается версия
            var profileExists = await _context.Profiles.AnyAsync(p => p.Id == version.ProfileId);
            if (!profileExists)
            {
                logger.LogWarning("Admin {AdminId} attempted to update version {VersionId} linked to a non-existent profile ID {ProfileId} via service.", userId, id, version.ProfileId);
                // Возвращаем BadRequestObjectResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestObjectResult("Profile not found.");
            }

            logger.LogInformation("Admin {AdminId} is updating version with ID {VersionId} via service.", userId, id);
            _context.Entry(version).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                logger.LogInformation("Version {VersionId} updated successfully by admin {AdminId} via service.", version.Id, userId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!VersionExists(id))
                {
                    logger.LogWarning("Admin {AdminId} attempted to update non-existent version with ID {VersionId} via service.", userId, id);
                    // Возвращаем NotFoundResult вместо NotFound(), так как метод не наследует ControllerBase
                    return new NotFoundResult();
                }
                else
                {
                    logger.LogError(ex, "Concurrency error while updating version {VersionId} by admin {AdminId} via service.", id, userId);
                    // Re-throw or handle
                    throw;
                }
            }

            // Возвращаем NoContentResult вместо NoContent(), так как метод не наследует ControllerBase
            return new NoContentResult();
        }

        /// <inheritdoc/>
        public async Task<IActionResult> DeleteVersionAsync(ILogger logger, string? userId, int id)
        {
            logger.LogInformation("Admin {AdminId} is attempting to delete version with ID {VersionId} via service.", userId, id);
            var version = await _context.Versions.FindAsync(id);
            if (version == null)
            {
                logger.LogWarning("Admin {AdminId} attempted to delete non-existent version with ID {VersionId} via service.", userId, id);
                // Возвращаем NotFoundResult вместо NotFound(), так как метод не наследует ControllerBase
                return new NotFoundResult();
            }

            _context.Versions.Remove(version);
            await _context.SaveChangesAsync();

            logger.LogInformation("Version {VersionId} deleted successfully by admin {AdminId} via service.", id, userId);
            // Возвращаем NoContentResult вместо NoContent(), так как метод не наследует ControllerBase
            return new NoContentResult();
        }

        private bool VersionExists(int id)
        {
            return _context.Versions.Any(e => e.Id == id);
        }
        #endregion

        #region Files
        /// <inheritdoc/>
        public async Task<IEnumerable<GameFile>> GetFilesAsync(ILogger logger, string? userId)
        {
            logger.LogInformation("Admin {AdminId} requested all files via service.", userId);
            return await _context.GameFiles
                .Include(f => f.Version)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<ActionResult<GameFile>> GetFileAsync(ILogger logger, string? userId, int id)
        {
            logger.LogInformation("Admin {AdminId} requested file with ID {FileId} via service.", userId, id);
            var file = await _context.GameFiles
                .Include(f => f.Version)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
            {
                logger.LogWarning("Admin {AdminId} requested non-existent file with ID {FileId} via service.", userId, id);
                // Возвращаем NotFoundObjectResult вместо NotFound(), так как метод не наследует ControllerBase
                return new NotFoundObjectResult($"File with ID {id} not found.");
            }

            // Возвращаем сам объект
            return file;
        }

        /// <inheritdoc/>
        public async Task<ActionResult<GameFile>> CreateFileAsync(ILogger logger, string? userId, GameFile file)
        {
            if (file == null)
            {
                logger.LogWarning("Admin {AdminId} attempted to create a file with null data via service.", userId);
                // Возвращаем BadRequestObjectResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestObjectResult("File data is required.");
            }

            // Проверить, существует ли версия, к которой привязывается файл
            var versionExists = await _context.Versions.AnyAsync(v => v.Id == file.VersionId);
            if (!versionExists)
            {
                logger.LogWarning("Admin {AdminId} attempted to create a file for a non-existent version ID {VersionId} via service.", userId, file.VersionId);
                // Возвращаем BadRequestObjectResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestObjectResult("Version not found.");
            }

            logger.LogInformation("Admin {AdminId} is creating a new file for version {VersionId} via service.", userId, file.VersionId);
            _context.GameFiles.Add(file);
            await _context.SaveChangesAsync();

            logger.LogInformation("File {FileId} created successfully by admin {AdminId} via service.", file.Id, userId);
            // Возвращаем сам объект
            return file;
        }

        /// <inheritdoc/>
        public async Task<IActionResult> UpdateFileAsync(ILogger logger, string? userId, int id, GameFile file)
        {
            if (file == null || id != file.Id)
            {
                logger.LogWarning("Admin {AdminId} attempted to update file {FileId} with invalid data (mismatched ID or null) via service.", userId, id);
                // Возвращаем BadRequestResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestResult();
            }

            // Проверить, существует ли версия, к которой привязывается файл
            var versionExists = await _context.Versions.AnyAsync(v => v.Id == file.VersionId);
            if (!versionExists)
            {
                logger.LogWarning("Admin {AdminId} attempted to update file {FileId} linked to a non-existent version ID {VersionId} via service.", userId, id, file.VersionId);
                // Возвращаем BadRequestObjectResult вместо BadRequest(), так как метод не наследует ControllerBase
                return new BadRequestObjectResult("Version not found.");
            }

            logger.LogInformation("Admin {AdminId} is updating file with ID {FileId} via service.", userId, id);
            _context.Entry(file).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                logger.LogInformation("File {FileId} updated successfully by admin {AdminId} via service.", file.Id, userId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!FileExists(id))
                {
                    logger.LogWarning("Admin {AdminId} attempted to update non-existent file with ID {FileId} via service.", userId, id);
                    // Возвращаем NotFoundResult вместо NotFound(), так как метод не наследует ControllerBase
                    return new NotFoundResult();
                }
                else
                {
                    logger.LogError(ex, "Concurrency error while updating file {FileId} by admin {AdminId} via service.", id, userId);
                    // Re-throw or handle
                    throw;
                }
            }

            // Возвращаем NoContentResult вместо NoContent(), так как метод не наследует ControllerBase
            return new NoContentResult();
        }

        /// <inheritdoc/>
        public async Task<IActionResult> DeleteFileAsync(ILogger logger, string? userId, int id)
        {
            logger.LogInformation("Admin {AdminId} is attempting to delete file with ID {FileId} via service.", userId, id);
            var file = await _context.GameFiles.FindAsync(id);
            if (file == null)
            {
                logger.LogWarning("Admin {AdminId} attempted to delete non-existent file with ID {FileId} via service.", userId, id);
                // Возвращаем NotFoundResult вместо NotFound(), так как метод не наследует ControllerBase
                return new NotFoundResult();
            }

            _context.GameFiles.Remove(file);
            await _context.SaveChangesAsync();

            logger.LogInformation("File {FileId} deleted successfully by admin {AdminId} via service.", id, userId);
            // Возвращаем NoContentResult вместо NoContent(), так как метод не наследует ControllerBase
            return new NoContentResult();
        }

        private bool FileExists(int id)
        {
            return _context.GameFiles.Any(e => e.Id == id);
        }
        #endregion
    }
}