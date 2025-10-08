using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Models;
using System.Threading.Tasks;
using FoxLauncher.Modules.AuthModule.Services;


namespace FoxLauncher.Modules.AuthModule.Services
{
    public class TextureService : ITextureService
    {
        private readonly AuthDbContext _context; 

        public TextureService(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<(string? SkinUrl, string? CapeUrl)?> GetUserTexturesAsync(string userUuid)
        {
            // Найти пользователя по UUID
            var user = await _context.Users
                .Where(u => u.Uuid == userUuid) 
                .Select(u => new { u.CurrentSkinId, u.CurrentCapeId }) 
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return null; 
            }

            string? skinUrl = null;
            string? capeUrl = null;

            // Получить URL скина, если он установлен
            if (user.CurrentSkinId.HasValue)
            {
                // Здесь нужно получить URL файла скина из соответствующего модуля (например, FileModule или CabinetModule)
                // Так как навигационное свойство для Skin в User типа object, мы не можем напрямую получить его.
                // Предположим, что в будущем будет реализован отдельный сервис для получения файлов по ID.
                // Пока что, для примера, возвращаем фиктивные URL.
                // Реализация зависит от структуры хранения файлов.
                // var skinFile = await _fileService.GetByIdAsync(user.CurrentSkinId.Value);
                // skinUrl = skinFile?.FilePath; // или путь к файлу на сервере

                // Временный фиктивный URL для скина
                skinUrl = $"/skins/{user.CurrentSkinId.Value}.png"; // Пример пути
            }

            // Получить URL плаща, если он установлен
            if (user.CurrentCapeId.HasValue)
            {
                // Аналогично скину
                // var capeFile = await _fileService.GetByIdAsync(user.CurrentCapeId.Value);
                // capeUrl = capeFile?.FilePath;

                // Временный фиктивный URL для плаща
                capeUrl = $"/capes/{user.CurrentCapeId.Value}.png"; // Пример пути
            }

            return (skinUrl, capeUrl);
        }
    }
}