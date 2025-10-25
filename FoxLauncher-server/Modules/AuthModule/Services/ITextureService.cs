using System.Threading.Tasks;

namespace FoxLauncher.Modules.AuthModule.Services
{
    public interface ITextureService
    {
        /// <summary>
        /// Получает URL-адреса скина и плаща для пользователя по его UUID.
        /// </summary>
        /// <param name="userUuid">UUID пользователя.</param>
        /// <returns>Кортеж (SkinUrl, CapeUrl) или null, если пользователь или текстуры не найдены.</returns>
        Task<(string? SkinUrl, string? CapeUrl)?> GetUserTexturesAsync(string userUuid);
    }
}