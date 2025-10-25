using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using FoxLauncher.Modules.CabinetModule.Models;
using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Services.Authlib; 

namespace FoxLauncher.Modules.AuthModule.Services
{
    public class AuthlibService : IAuthlibService
    {
        private readonly AuthDbContext _context;
        private readonly ITextureService _textureService;
        private readonly IAuthlibKeyService _keyService; 
        private readonly ILogger<AuthlibService> _logger;

        public AuthlibService(AuthDbContext context, ITextureService textureService, IAuthlibKeyService keyService, ILogger<AuthlibService> logger)
        {
            _context = context;
            _textureService = textureService;
            _keyService = keyService;
            _logger = logger;
        }

        public async Task<User?> GetUserByUuidAsync(string uuid)
        {
            _logger.LogDebug("Fetching user profile by UUID: {UUID}", uuid);
            var user = await _context.Users
                .Include(u => u.CurrentSkin)
                .Include(u => u.CurrentCape)
                .FirstOrDefaultAsync(u => u.UUID == uuid); 

            if (user == null)
            {
                _logger.LogDebug("Profile not found for UUID: {UUID}", uuid);
            }
            return user;
        }

        // Меняем тип возвращаемого значения и параметра username на CabinetModule.Models.User
        public async Task<object?> ValidateHasJoinedAsync(string username, string? serverId, string? selectedProfile)
        {
            _logger.LogDebug("Validating hasJoined for user {Username}, serverId {ServerId}, selectedProfile {SelectedProfile}", username, serverId, selectedProfile);

            // _context.Users возвращает CabinetModule.Models.User
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                _logger.LogDebug("User {Username} not found for hasJoined.", username);
                return null; // Индикатор неудачи
            }

            // Проверить selectedProfile, если он предоставлен
            if (!string.IsNullOrEmpty(selectedProfile) && user.UUID != selectedProfile) // Используем UUID из CabinetModule.Models.User
            {
                _logger.LogWarning("User {Username} ({UUID}) attempted to join with mismatched selectedProfile {SelectedProfile}.", username, user.UUID, selectedProfile);
                return null; // Индикатор неудачи
            }

            // Получить текстуры
            var texturesResult = await _textureService.GetUserTexturesAsync(user.UUID);
            var profileResponse = new
            {
                id = user.UUID, // Используем UUID из CabinetModule.Models.User
                name = user.Username, // Используем Username из CabinetModule.Models.User
                properties = new List<object>()
            };

            if (!string.IsNullOrEmpty(texturesResult?.SkinUrl) || !string.IsNullOrEmpty(texturesResult?.CapeUrl))
            {
                var textures = new
                {
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    profileId = user.UUID, // Используем UUID из CabinetModule.Models.User
                    profileName = user.Username, // Используем Username из CabinetModule.Models.User
                    isPublic = true,
                    textures = new Dictionary<string, object>()
                };

                if (!string.IsNullOrEmpty(texturesResult.Value.SkinUrl))
                {
                    textures.textures.Add("SKIN", new { url = texturesResult.Value.SkinUrl }); // URL будет добавлен на уровне контроллера
                }
                if (!string.IsNullOrEmpty(texturesResult.Value.CapeUrl))
                {
                    textures.textures.Add("CAPE", new { url = texturesResult.Value.CapeUrl });
                }

                var texturesJson = JsonSerializer.Serialize(textures);
                var texturesValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(texturesJson));

                var textureProperty = new
                {
                    name = "textures",
                    value = texturesValue
                };

                // Если serverId предоставлен, добавить подпись
                if (!string.IsNullOrEmpty(serverId))
                {
                    var verificationString = serverId + user.UUID; // Используем UUID из CabinetModule.Models.User
                    var signature = _keyService.SignData(Encoding.UTF8.GetBytes(verificationString));
                    profileResponse.properties.Add(new { name = textureProperty.name, value = textureProperty.value, signature });
                }
                else
                {
                    profileResponse.properties.Add(new { name = textureProperty.name, value = textureProperty.value });
                }
            }

            return profileResponse;
        }

        // Меняем логику подтверждения, если она зависит от User
        public async Task<bool> ConfirmJoinAsync(string accessToken, string selectedProfile, string serverId)
        {
            _logger.LogDebug("Confirming join for serverId {ServerId} and profile {SelectedProfile}", serverId, selectedProfile);

            // Логика проверки accessToken должна быть на уровне аутентификации/авторизации
            // Предположим, что UUID пользователя извлечен в контроллере или через кастомный обработчик

            // В реальной системе здесь может быть проверка сессии, хранение serverId и т.д.
            // Пока простая проверка соответствия selectedProfile

            // (Проверка JWT токена и извлечение UUID уже выполнена до вызова этого метода)
            // var userIdFromToken = ... // Получено из Claims

            // var user = await _context.Users.FindAsync(userIdFromToken);
            // if (user != null && user.UUID == selectedProfile) // Используем UUID из CabinetModule.Models.User
            // {
            //     // Записать факт присоединения (например, в сессию или отдельную таблицу)
            //     // await _sessionService.RecordJoinAsync(userIdFromToken, serverId);
            //     return true;
            // }

            // Для упрощения, просто проверим, что selectedProfile не пустой
            // Реализация будет зависеть от того, как вы будете извлекать userId из JWT в этом сервисе
            // Пока возвращаем true, если selectedProfile совпадает с каким-то ожидаемым значением (например, из JWT)
            // Это место требует интеграции с системой аутентификации
            return !string.IsNullOrEmpty(selectedProfile) && !string.IsNullOrEmpty(serverId);
            // Возвращаем true, если логика проверки будет реализована и пройдена
        }
    }
}