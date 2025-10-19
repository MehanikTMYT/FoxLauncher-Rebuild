using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.FileModule.Data;
using FoxLauncher.Modules.FileModule.Models;
using FoxLauncher.Modules.ProfileModule.Data; // Для проверки прав доступа к версии
using System.Security.Claims;

namespace FoxLauncher.Modules.FileModule.Controllers
{
    [ApiController]
    [Route("api/file")] // Базовый путь для файл-сервера
    [Authorize] // Защищаем эндпоинты скачивания (пользователь должен быть аутентифицирован)
    public class FileController : ControllerBase
    {
        private readonly FileDbContext _fileContext; // Для логирования (опционально)
        private readonly ProfileDbContext _profileContext; // Для проверки прав доступа к версии/профилю
        private readonly ILogger<FileController> _logger;

        public FileController(FileDbContext fileContext, ProfileDbContext profileContext, ILogger<FileController> logger)
        {
            _fileContext = fileContext;
            _profileContext = profileContext;
            _logger = logger;
        }

        // GET /api/file/download/{fileName}
        // Предполагаем, что fileName - это имя файла, как оно хранится в GameFile.FilePath
        // В реальности, возможно, лучше использовать ID файла или хэш, а не путь.
        [HttpGet("download/{*fileName}")] // *fileName захватывает весь оставшийся путь
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name is required.");
            }

            // Получить UserId из токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            // Проверить, существует ли файл в GameFile (в admin_db)
            // Для этого нужно получить ProfileDbContext
            // Найдем GameFile по FilePath
            var gameFile = await _profileContext.GameFiles
                .Include(f => f.Version) // Включаем версию
                    .ThenInclude(v => v.Profile) // Включаем профиль версии
                .FirstOrDefaultAsync(f => f.FilePath == fileName); // Сравниваем с FilePath

            if (gameFile == null)
            {
                _logger.LogWarning("File not found in admin DB: {FileName} requested by User ID: {UserId}", fileName, userId);
                return NotFound("File not found in system.");
            }

            // Проверить права доступа:
            // 1. Принадлежит ли версия публичному профилю?
            // 2. Или имеет ли пользователь доступ к приватному профилю (например, через членство в группе, подписку и т.д.)?
            // Пока проверим только публичность профиля.
            if (gameFile.Version.Profile == null || !gameFile.Version.Profile.IsPublic)
            {
                _logger.LogWarning("Access denied to file: {FileName} (Profile is not public) for User ID: {UserId}", fileName, userId);
                // Возвращаем 404, чтобы не раскрывать существование файла в приватном профиле
                return NotFound("File not found in system.");
            }

            // Формируем путь к файлу на диске
            // ВАЖНО: Убедитесь, что fileName не содержит "../" или других потенциально опасных последовательностей.
            // Лучше всего использовать хэш файла или ID как имя файла на диске.
            var safeFileName = Path.GetFileName(fileName); // Базовое экранирование
            if (string.IsNullOrEmpty(safeFileName) || fileName != safeFileName)
            {
                _logger.LogWarning("Invalid file path: {FileName} requested by User ID: {UserId}", fileName, userId);
                return BadRequest("Invalid file path.");
            }

            var filePath = Path.Combine("wwwroot", "game_files", fileName); // Папка для файлов игры
            // Альтернатива: использовать хэш из gameFile.Hash как имя файла на диске
            // var filePath = Path.Combine("wwwroot", "game_files", gameFile.Hash);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File not found on disk: {FilePath} (DB Path: {DBPath}) for User ID: {UserId}", filePath, fileName, userId);
                return NotFound("File not found on disk.");
            }

            // Опционально: Залогировать загрузку
            var downloadLog = new DownloadLog
            {
                FilePath = fileName, // Логируем путь из запроса или DB
                UserId = userId, // ID пользователя из auth_db
                VersionId = gameFile.VersionId // ID версии из admin_db
            };
            _fileContext.DownloadLogs.Add(downloadLog); // Если DbSet есть
            await _fileContext.SaveChangesAsync(); // Если DbSet есть

            // Отдать файл
            _logger.LogInformation("File served: {FilePath} to User ID: {UserId}", fileName, userId);
            return PhysicalFile(filePath, "application/octet-stream", safeFileName, true); // true для inline (браузер предложит сохранить)
        }

        // GET /api/file/check/{fileName}
        // Опциональный эндпоинт для проверки существования файла и прав доступа без его скачивания
        [HttpGet("check/{*fileName}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CheckFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name is required.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var gameFile = await _profileContext.GameFiles
                .Include(f => f.Version)
                    .ThenInclude(v => v.Profile)
                .FirstOrDefaultAsync(f => f.FilePath == fileName);

            if (gameFile == null || (gameFile.Version.Profile != null && !gameFile.Version.Profile.IsPublic))
            {
                // Возвращаем 404, чтобы не раскрывать существование файла в приватном профиле
                return NotFound();
            }

            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(safeFileName) || fileName != safeFileName)
            {
                return BadRequest("Invalid file path.");
            }

            var filePath = Path.Combine("wwwroot", "game_files", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Файл существует и доступен
            return Ok(new { message = "File is available for download." });
        }
    }
}