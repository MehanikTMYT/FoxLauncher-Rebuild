using FoxLauncher.Modules.FileModule.Models;

namespace FoxLauncher.Modules.FileModule.Services
{
    public interface IFileService
    {
        Task<bool> LogDownloadAsync(string filePath, int? userId, int? versionId);
    }
}