using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.FileModule.Data;
using FoxLauncher.Modules.FileModule.Models;

namespace FoxLauncher.Modules.FileModule.Services
{
    public class FileService : IFileService
    {
        private readonly FileDbContext _context;

        public FileService(FileDbContext context)
        {
            _context = context;
        }

        public async Task<bool> LogDownloadAsync(string filePath, int? userId, int? versionId)
        {
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
                return true;
            }
            catch (Exception ex)
            {
                // Логировать ошибку
                return false;
            }
        }
    }
}