using FoxLauncher.Modules.FileModule.Data;
using FoxLauncher.Modules.FileModule.Models;
using FoxLauncher.Modules.FileModule.Services;  
using FoxLauncher.Modules.ProfileModule.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations; 
using System.Security.Claims;
using System.Text.RegularExpressions; // Для проверки имени файла

namespace FoxLauncher.Modules.FileModule.Controllers
{
    /// <summary>
    /// Контроллер для управления скачиванием файлов.
    /// </summary>
    [ApiController]
    [Route("api/file")] // Базовый путь для файл-сервера
    [Authorize] // Защищаем эндпоинты скачивания (пользователь должен быть аутентифицирован)
    public class FileController : ControllerBase
    {
        private readonly FileDbContext _fileContext;
        private readonly ProfileDbContext _profileContext; // Переименовано для ясности
        private readonly IFileService _fileService; // Внедряем IFileService
        private readonly ILogger<FileController> _logger;

        public FileController(FileDbContext fileContext, ProfileDbContext profileContext, IFileService fileService, ILogger<FileController> logger) // Изменен конструктор
        {
            _fileContext = fileContext ?? throw new ArgumentNullException(nameof(fileContext));
            _profileContext = profileContext ?? throw new ArgumentNullException(nameof(profileContext));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Скачивание файла по его имени.
        /// </summary>
        /// <param name="fileName">Имя файла для скачивания (включая путь относительно корня файлов).</param>
        /// <returns>Файл для скачивания или соответствующий код ошибки.</returns>
        [HttpGet("download/{*fileName}")]
        [SwaggerOperation(
            Summary = "Скачать файл",
            Description = "Позволяет аутентифицированному пользователю скачать файл, если он существует, доступен (публичный профиль) и находится на диске. Также логирует попытку скачивания."
        )]
        [ProducesResponseType(200)] // FileResult при успехе
        [ProducesResponseType(400)] // Bad Request при пустом имени файла или небезопасном пути
        [ProducesResponseType(401)] // Unauthorized при отсутствии аутентификации
        [ProducesResponseType(404)] // Not Found при отсутствии файла в БД или на диске, или при недоступности (приватный профиль)
        [ProducesResponseType(500)] // Internal Server Error (опционально, если логирование не удалось)
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Download attempt with empty file name from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return BadRequest("File name is required.");
            }

            // Получить UserId из токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogWarning("Download attempt with invalid or missing user ID claim from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized();
            }

            // Проверить имя файла на безопасность (предотвращение directory traversal)
            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(safeFileName) || !IsValidFileName(safeFileName))
            {
                _logger.LogWarning("Download attempt with potentially unsafe file path: {FilePath} by User ID: {UserId} from IP: {IP}", fileName, userId, HttpContext.Connection.RemoteIpAddress);
                return BadRequest("Invalid file path.");
            }

            // Проверить, существует ли файл в GameFile (в ProfileModule DB) И получить информацию о версии/профиле
            // ИСПОЛЬЗУЕТСЯ: _profileContext
            var gameFile = await _profileContext.GameFiles
                .Include(f => f.Version)
                    .ThenInclude(v => v.Profile)
                .FirstOrDefaultAsync(f => f.FilePath == fileName);

            if (gameFile == null)
            {
                _logger.LogWarning("File not found in profile DB: {FileName} requested by User ID: {UserId} from IP: {IP}", fileName, userId, HttpContext.Connection.RemoteIpAddress);
                return NotFound("File not found in system.");
            }

            // Проверить, является ли профиль публичным
            if (gameFile.Version.Profile == null || !gameFile.Version.Profile.IsPublic)
            {
                _logger.LogWarning("Access denied to file: {FileName} (Profile '{ProfileName}' is not public) for User ID: {UserId} from IP: {IP}", fileName, gameFile.Version.Profile?.Name, userId, HttpContext.Connection.RemoteIpAddress);
                // Возвращаем 404, чтобы не раскрывать существование файла в приватном профиле
                return NotFound("File not found in system.");
            }

            // Построить путь к файлу на диске
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "game_files", fileName);

            // Проверить, существует ли файл на диске
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("File not found on disk: {FilePath} (DB Path: {DBPath}) for User ID: {UserId} from IP: {IP}", filePath, fileName, userId, HttpContext.Connection.RemoteIpAddress);
                // Опционально: можно вернуть 500, если файл должен был существовать, но исчез
                return NotFound("File not found on disk.");
            }

            // Попытаться залогировать скачивание
            try
            {
                await _fileService.LogDownloadAsync(fileName, userId, gameFile.VersionId);
                _logger.LogDebug("Download logged for file: {FileName} by User ID: {UserId}", fileName, userId);
            }
            catch (Exception ex)
            {
                // Не прерываем скачивание, если логирование не удалось, но логируем ошибку
                _logger.LogError(ex, "Failed to log download for file: {FileName} by User ID: {UserId} from IP: {IP}", fileName, userId, HttpContext.Connection.RemoteIpAddress);
            }

            _logger.LogInformation("File served: {FilePath} to User ID: {UserId} from IP: {IP}", fileName, userId, HttpContext.Connection.RemoteIpAddress);
            // Используем fileName из БД для Content-Disposition, но safeFileName для физического файла
            return PhysicalFile(filePath, "application/octet-stream", safeFileName, true);
        }

        /// <summary>
        /// Проверить существование файла и доступность для скачивания.
        /// </summary>
        /// <param name="fileName">Имя файла для проверки (включая путь относительно корня файлов).</param>
        /// <returns>Информация о доступности файла или соответствующий код ошибки.</returns>
        [HttpGet("check/{*fileName}")]
        [SwaggerOperation(
            Summary = "Проверить файл",
            Description = "Проверяет, существует ли файл в системе и доступен ли он для скачивания текущим пользователем (публичный профиль, файл на диске)."
        )]
        [ProducesResponseType(200)] // Ok с сообщением при успехе
        [ProducesResponseType(400)] // Bad Request при пустом имени файла или небезопасном пути
        [ProducesResponseType(401)] // Unauthorized при отсутствии аутентификации
        [ProducesResponseType(404)] // Not Found при отсутствии файла в БД или на диске, или при недоступности (приватный профиль)
        public async Task<IActionResult> CheckFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Check attempt with empty file name from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return BadRequest("File name is required.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogWarning("Check attempt with invalid or missing user ID claim from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized();
            }

            // Проверить имя файла на безопасность
            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(safeFileName) || !IsValidFileName(safeFileName))
            {
                _logger.LogWarning("Check attempt with potentially unsafe file path: {FilePath} by User ID: {UserId} from IP: {IP}", fileName, userId, HttpContext.Connection.RemoteIpAddress);
                return BadRequest("Invalid file path.");
            }

            // ИСПОЛЬЗУЕТСЯ: _profileContext
            var gameFile = await _profileContext.GameFiles
                .Include(f => f.Version)
                    .ThenInclude(v => v.Profile)
                .FirstOrDefaultAsync(f => f.FilePath == fileName);

            if (gameFile == null || (gameFile.Version.Profile != null && !gameFile.Version.Profile.IsPublic))
            {
                // Возвращаем 404, чтобы не раскрывать существование файла в приватном профиле
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "game_files", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                // Файл есть в БД, но нет на диске
                return NotFound();
            }

            // Файл существует и доступен
            _logger.LogDebug("File check passed: {FilePath} for User ID: {UserId} from IP: {IP}", fileName, userId, HttpContext.Connection.RemoteIpAddress);
            return Ok(new { message = "File is available for download." });
        }

        // Вспомогательный метод для проверки имени файла
        private static bool IsValidFileName(string fileName)
        {
            // Простая проверка: только буквы, цифры, точки, тире, подчеркивания
            // Регулярное выражение может быть сложнее в зависимости от требований
            var regex = new Regex(@"^[a-zA-Z0-9._-]+$");
            return regex.IsMatch(fileName);
        }
    }
}