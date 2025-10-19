using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Models;
using System.Threading.Tasks;

namespace FoxLauncher.Modules.AuthModule.Services
{
    public class TextureService : ITextureService
    {
        private readonly AuthDbContext _context; // Используем AuthDbContext, так как он содержит User, Skin, Cape

        public TextureService(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<(string? SkinUrl, string? CapeUrl)?> GetUserTexturesAsync(string userUuid)
        {
            // Найти пользователя по UUID, включая связанные скин и плащ
            var user = await _context.Users
                .Where(u => u.UUID == userUuid)
                .Include(u => u.CurrentSkin) // Загружаем текущий скин
                .Include(u => u.CurrentCape) // Загружаем текущий плащ
                .Select(u => new { u.CurrentSkin, u.CurrentCape }) // Проекция для оптимизации
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return null; // Пользователь не найден
            }

            string? skinUrl = null;
            string? capeUrl = null;

            // Получить URL скина, если он установлен и файл существует
            if (user.CurrentSkin != null)
            {
                // Формируем URL к файлу скина. Путь зависит от вашей стратегии раздачи файлов.
                // Вариант 1: Статическая папка (например, wwwroot/skins)
                // skinUrl = $"/skins/{user.CurrentSkin.FileName}";

                // Вариант 2: API-эндпоинт для получения скина (более гибко, можно добавить логику проверки прав)
                skinUrl = $"/api/cabinet/skin/{user.CurrentSkin.FileName}"; // Пример API пути
            }

            // Получить URL плаща, если он установлен и файл существует
            if (user.CurrentCape != null && user.CurrentCape.IsActive) // Проверяем IsActive для плаща
            {
                // capeUrl = $"/capes/{user.CurrentCape.FileName}"; // Пример для статической папки
                capeUrl = $"/api/cabinet/cape/{user.CurrentCape.FileName}"; // Пример API пути
            }

            return (skinUrl, capeUrl);
        }
    }
}