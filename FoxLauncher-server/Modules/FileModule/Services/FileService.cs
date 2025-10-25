using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.FileModule.Data;
using FoxLauncher.Modules.FileModule.Models;
using Microsoft.Extensions.Logging;

namespace FoxLauncher.Modules.FileModule.Services
{
    public class FileService : IFileService
    {
        private readonly FileDbContext _context;
        private readonly ILogger<FileService> _logger; // Добавлен логгер

        public FileService(FileDbContext context, ILogger<FileService> logger) // Изменен конструктор для внедрения логгера
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> LogDownloadAsync(string filePath, int? userId, int? versionId)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Attempt to log download with empty file path.");
                return false; // или throw new ArgumentException(...)
            }

            var logEntry = new DownloadLog
            {
                FilePath = filePath,
                UserId = userId,
                VersionId = versionId,
                DownloadedAt = DateTime.UtcNow
            };

            _context.DownloadLogs.Add(logEntry);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogDebug("Download log entry added for file: {FilePath}, UserId: {UserId}, VersionId: {VersionId}", filePath, userId, versionId);
                return true;
            }
            catch (DbUpdateException ex) // Конкретная ошибка EF
            {
                _logger.LogError(ex, "Database error while logging download for file: {FilePath}, UserId: {UserId}, VersionId: {VersionId}", filePath, userId, versionId);
                return false;
            }
            catch (Exception ex) // Общая ошибка
            {
                _logger.LogError(ex, "Unexpected error while logging download for file: {FilePath}, UserId: {UserId}, VersionId: {VersionId}", filePath, userId, versionId);
                return false;
            }
        }
    }
}