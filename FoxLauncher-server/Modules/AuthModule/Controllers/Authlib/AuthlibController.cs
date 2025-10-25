using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Models;
using FoxLauncher.Modules.AuthModule.Services;
using FoxLauncher.Modules.AuthModule.Services.Authlib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations; 
using System.Text;
using System.Text.Json;

namespace FoxLauncher.Modules.AuthModule.Controllers.Authlib
{
    /// <summary>
    /// Контроллер, реализующий протокол authlib-injector для совместимости с Minecraft.
    /// </summary>
    [ApiController]
    [Route("authlib")] // Базовый путь для authlib
    [Authorize(Policy = "RequireUserRole")]
    public class AuthlibController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IAuthlibKeyService _keyService;
        private readonly ITextureService _textureService;
        private readonly ILogger<AuthlibController> _logger; // Добавляем логгер

        public AuthlibController(AuthDbContext context, IAuthlibKeyService keyService, ITextureService textureService, ILogger<AuthlibController> logger)
        {
            _context = context;
            _keyService = keyService;
            _textureService = textureService;
            _logger = logger; // Инициализируем логгер
        }

        /// <summary>
        /// Возвращает информацию о сервере authlib, необходимую для authlib-injector.
        /// </summary>
        /// <returns>Объект с метаданными сервера, доменами скинов и публичным ключом.</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Получить информацию о сервере authlib",
            Description = "Этот эндпоинт используется клиентом (authlib-injector) для получения информации о сервере аутентификации, включая публичный ключ для проверки подписей."
        )]
        [ProducesResponseType(typeof(object), 200)] // Уточните тип возврата, если создадите класс
        [ProducesResponseType(500)]
        public IActionResult GetAuthlibInfo()
        {
            try
            {
                var response = new
                {
                    meta = new
                    {
                        serverName = "FoxLauncher Authlib", // Имя вашего сервера
                        implementationName = "fox-launcher-authserver",
                        implementationVersion = "1.0.0",
                    },
                    skinDomains = new[] { Request.Host.Host }, // Домен, с которого разрешены скины (ваш сервер)
                    signaturePublickey = _keyService.GetPublicKeyPem().Replace("\n", "").Replace("\r", "")
                };
                // Используем indexer для установки заголовка
                Response.Headers["X-Authlib-Injector-API-Location"] = $"{Request.Scheme}://{Request.Host}/authlib";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting authlib info.");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Получает профиль пользователя Minecraft по его UUID.
        /// </summary>
        /// <param name="uuid">UUID пользователя.</param>
        /// <returns>Информация о профиле пользователя, включая имя и текстуры (если есть).</returns>
        [HttpGet("session/minecraft/profile/{uuid}")]
        [SwaggerOperation(
            Summary = "Получить профиль Minecraft по UUID",
            Description = "Возвращает информацию о профиле пользователя Minecraft, включая имя и данные о скине/плаще, если они установлены."
        )]
        [ProducesResponseType(typeof(object), 200)] // Уточните тип возврата, если создадите класс
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProfile(string uuid) // Используем lowercase 'uuid' для соответствия стандарту
        {
            try
            {
                // Ищем пользователя по его UUID
                var user = await _context.Users
                    .Include(u => u.CurrentSkin)
                    .Include(u => u.CurrentCape)
                    .FirstOrDefaultAsync(u => u.UUID == uuid); // Используем UUID, а не Id

                if (user == null)
                {
                    _logger.LogDebug("Profile not found for UUID: {UUID}", uuid);
                    return NotFound();
                }

                // Получить текстуры через TextureService
                var texturesResult = await _textureService.GetUserTexturesAsync(user.UUID);

                var profileResponse = new
                {
                    id = user.UUID, // Используем UUID, а не Id
                    name = user.Username,
                    properties = new List<object>()
                };

                if (!string.IsNullOrEmpty(texturesResult?.SkinUrl) || !string.IsNullOrEmpty(texturesResult?.CapeUrl))
                {
                    var textures = new
                    {
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        profileId = user.UUID, // Используем UUID, а не Id
                        profileName = user.Username,
                        isPublic = true,
                        textures = new Dictionary<string, object>()
                    };

                    if (!string.IsNullOrEmpty(texturesResult.Value.SkinUrl))
                    {
                        textures.textures.Add("SKIN", new { url = $"{Request.Scheme}://{Request.Host}{texturesResult.Value.SkinUrl}" });
                    }
                    if (!string.IsNullOrEmpty(texturesResult.Value.CapeUrl))
                    {
                        textures.textures.Add("CAPE", new { url = $"{Request.Scheme}://{Request.Host}{texturesResult.Value.CapeUrl}" });
                    }

                    var texturesJson = JsonSerializer.Serialize(textures);
                    var texturesValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(texturesJson));

                    // Подпись текстур (опционально для этого эндпоинта, но часто добавляется)
                    // var signature = _keyService.SignData(Encoding.UTF8.GetBytes(texturesJson));

                    // Добавляем свойство в список
                    profileResponse.properties.Add(new
                    {
                        name = "textures",
                        value = texturesValue
                        // signature // Подпись добавляется, если требуется
                    });
                }

                return Ok(profileResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching profile for UUID: {UUID}", uuid);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Проверяет, вошёл ли пользователь на сервер игры.
        /// </summary>
        /// <param name="username">Имя пользователя.</param>
        /// <param name="serverId">ID сервера (опционально, если проверка со стороны клиента).</param>
        /// <param name="selectedProfile">UUID выбранного профиля (опционально).</param>
        /// <param name="ip">IP-адрес клиента (опционально).</param>
        /// <returns>Профиль пользователя и текстуры, если проверка успешна, иначе 204 No Content.</returns>
        [HttpGet("sessionserver/session/minecraft/hasJoined")]
        [SwaggerOperation(
            Summary = "Проверить вход на сервер игры",
            Description = "Используется как клиентом (без serverId), так и сервером игры (с serverId) для проверки аутентификации пользователя. Возвращает профиль и текстуры при успехе или 204 No Content при неудаче."
        )]
        [ProducesResponseType(typeof(object), 200)] // Уточните тип возврата, если создадите класс
        [ProducesResponseType(204)] // No Content при неудаче
        [ProducesResponseType(400)] // Bad Request при отсутствии username
        [ProducesResponseType(500)]
        public async Task<IActionResult> HasJoined(
            [FromQuery][SwaggerParameter("Имя пользователя.")] string username,
            [FromQuery][SwaggerParameter("ID сервера игры (для проверки со стороны сервера).")] string? serverId = null,
            [FromQuery][SwaggerParameter("UUID выбранного профиля (опционально).")] string? selectedProfile = null, // Добавляем selectedProfile как параметр
            [FromQuery][SwaggerParameter("IP-адрес клиента (опционально).")] string? ip = null)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("hasJoined called without username.");
                    return BadRequest(new { error = "Username is required" });
                }

                // Найти пользователя по имени (для клиента, который может не знать UUID)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    _logger.LogDebug("User {Username} not found for hasJoined.", username);
                    // Возвращаем 204 No Content, если игрок не найден
                    return NoContent();
                }

                // Проверить, соответствует ли переданный selectedProfile UUID пользователя
                // Это важно, если /join проверял конкретный профиль
                if (!string.IsNullOrEmpty(selectedProfile))
                {
                    // Сравниваем переданный selectedProfile с UUID пользователя
                    // В простой схеме (один профиль = один пользователь) это должно совпадать
                    if (user.UUID != selectedProfile)
                    {
                        _logger.LogWarning("User {Username} ({UUID}) attempted to join with mismatched selectedProfile {SelectedProfile}.", username, user.UUID, selectedProfile);
                        // Если переданный selectedProfile не совпадает с UUID пользователя,
                        // сервер игры может не принять это как успешную аутентификацию.
                        // Возвращаем NoContent, как если бы пользователь не был найден.
                        // Альтернатива - BadRequest, если это ошибка клиента/сервера.
                        // Стандарт authlib часто возвращает NoContent в случае несоответствия.
                        return NoContent(); // Или BadRequest() для большей ясности ошибки
                    }
                }


                // Если serverId не предоставлен, возвращаем профиль для клиента
                if (string.IsNullOrEmpty(serverId))
                {
                    _logger.LogDebug("Client-side hasJoined request for user {Username} ({UUID}).", username, user.UUID);
                    // Получить текстуры через TextureService
                    var texturesResult = await _textureService.GetUserTexturesAsync(user.UUID);

                    var profileResponse = new
                    {
                        id = user.UUID, // Используем UUID, а не Id
                        name = user.Username,
                        properties = new List<object>()
                    };

                    if (!string.IsNullOrEmpty(texturesResult?.SkinUrl) || !string.IsNullOrEmpty(texturesResult?.CapeUrl))
                    {
                        var textures = new
                        {
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            profileId = user.UUID, // Используем UUID, а не Id
                            profileName = user.Username,
                            isPublic = true,
                            textures = new Dictionary<string, object>()
                        };

                        if (!string.IsNullOrEmpty(texturesResult.Value.SkinUrl))
                        {
                            textures.textures.Add("SKIN", new { url = $"{Request.Scheme}://{Request.Host}{texturesResult.Value.SkinUrl}" });
                        }
                        if (!string.IsNullOrEmpty(texturesResult.Value.CapeUrl))
                        {
                            textures.textures.Add("CAPE", new { url = $"{Request.Scheme}://{Request.Host}{texturesResult.Value.CapeUrl}" });
                        }

                        var texturesJson = JsonSerializer.Serialize(textures);
                        var texturesValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(texturesJson));

                        profileResponse.properties.Add(new
                        {
                            name = "textures",
                            value = texturesValue
                            // signature (опционально для клиента)
                        });
                    }

                    return Ok(profileResponse);
                }
                else // serverId предоставлен - запрос от сервера игры
                {
                    _logger.LogDebug("Server-side hasJoined request for user {Username} ({UUID}) with serverId {ServerId}.", username, user.UUID, serverId);
                    // Требуется подпись serverId с приватным ключом и UUID игрока
                    // Важно: используем UUID пользователя (который, в идеале, совпадает с selectedProfile из /join)
                    // или, если selectedProfile был проверен выше, используем его (в простой схеме это тот же UUID).
                    // Так как мы уже проверили соответствие в начале метода, можно использовать user.UUID.
                    var verificationString = serverId + user.UUID; // Используем UUID пользователя
                    var signature = _keyService.SignData(Encoding.UTF8.GetBytes(verificationString));

                    // Возвращаем профиль с подписью serverId
                    var texturesResult = await _textureService.GetUserTexturesAsync(user.UUID);

                    var profileResponse = new
                    {
                        id = user.UUID, // Используем UUID, а не Id
                        name = user.Username,
                        properties = new List<object>()
                    };

                    if (!string.IsNullOrEmpty(texturesResult?.SkinUrl) || !string.IsNullOrEmpty(texturesResult?.CapeUrl))
                    {
                        var textures = new
                        {
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            profileId = user.UUID, // Используем UUID, а не Id
                            profileName = user.Username,
                            isPublic = true,
                            textures = new Dictionary<string, object>()
                        };

                        if (!string.IsNullOrEmpty(texturesResult.Value.SkinUrl))
                        {
                            textures.textures.Add("SKIN", new { url = $"{Request.Scheme}://{Request.Host}{texturesResult.Value.SkinUrl}" });
                        }
                        if (!string.IsNullOrEmpty(texturesResult.Value.CapeUrl))
                        {
                            textures.textures.Add("CAPE", new { url = $"{Request.Scheme}://{Request.Host}{texturesResult.Value.CapeUrl}" });
                        }

                        var texturesJson = JsonSerializer.Serialize(textures);
                        var texturesValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(texturesJson));

                        profileResponse.properties.Add(new
                        {
                            name = "textures",
                            value = texturesValue,
                            signature // Подпись добавляется для serverId
                        });
                    }

                    return Ok(profileResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during hasJoined request for user {Username} and serverId {ServerId}.", username, serverId ?? "null");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Подтверждает соединение игрока с сервером игры.
        /// </summary>
        /// <param name="body">Тело запроса, содержащее accessToken, selectedProfile и serverId.</param>
        /// <returns>204 No Content при успешном подтверждении.</returns>
        [HttpPost("sessionserver/session/minecraft/join")]
        [SwaggerOperation(
            Summary = "Подтвердить соединение с сервером игры",
            Description = "Используется сервером игры для подтверждения, что игрок с указанным accessToken и UUID профиля (selectedProfile) пытается присоединиться к серверу с указанным ID (serverId)."
        )]
        [ProducesResponseType(204)] // No Content при успехе
        [ProducesResponseType(400)] // Bad Request при отсутствии полей
        [ProducesResponseType(401)] // Unauthorized при невалидном токене
        [ProducesResponseType(500)]
        public async Task<IActionResult> JoinServer([FromBody][SwaggerRequestBody("Тело запроса с accessToken, selectedProfile и serverId.")] JsonElement body)
        {
            try
            {
                string? accessToken;
                string? selectedProfile;
                string? serverId;

                try
                {
                    accessToken = body.GetProperty("accessToken").GetString();
                    selectedProfile = body.GetProperty("selectedProfile").GetString();
                    serverId = body.GetProperty("serverId").GetString();
                }
                catch (KeyNotFoundException ex)
                {
                    _logger.LogWarning("Malformed join request body: {ExceptionMessage}", ex.Message);
                    return BadRequest(new { error = "Invalid request body" });
                }

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(selectedProfile) || string.IsNullOrEmpty(serverId))
                {
                    _logger.LogWarning("Join request missing required fields. accessToken: {AccessToken}, selectedProfile: {SelectedProfile}, serverId: {ServerId}", accessToken, selectedProfile, serverId);
                    return BadRequest(new { error = "accessToken, selectedProfile, and serverId are required" });
                }

                // Проверка JWT токена (предполагается, что JWT уже проверен через Authentication/Authorization атрибуты или кастомную логику)
                // Получаем UUID пользователя из Claims (предполагается, что JWT содержит claim "user_UUID" или "user_UUID")
                var userIdClaim = User.FindFirst("user_UUID"); // Используем кастомный claim для UUID
                if (userIdClaim == null)
                {
                    // Логика для анонимного доступа или ошибка
                    // Для /join нужен аутентифицированный пользователь с JWT, содержащим UUID
                    _logger.LogWarning("No user_UUID claim found in JWT for join request with serverId {ServerId}.", serverId);
                    return Unauthorized(new { error = "Unauthorized: Invalid token" });
                }

                var userIdFromToken = userIdClaim.Value; // Это строка UUID

                // Проверить, что selectedProfile (UUID) принадлежит пользователю с userId
                // selectedProfile - это UUID, который передал сервер игры
                // userIdFromToken - это UUID, извлеченный из JWT
                if (userIdFromToken != selectedProfile)
                {
                    _logger.LogWarning("Join request failed: selectedProfile {SelectedProfile} does not match user_UUID {UserIdFromToken} for serverId {ServerId}.", selectedProfile, userIdFromToken, serverId);
                    return BadRequest(new { error = "Selected profile does not belong to the authenticated user." });
                }

                // Логика проверки serverId (например, хеширование как в спецификации)
                // В реальности, сервер игры использует serverId для вызова hasJoined и проверки подписи.
                // Этот эндпоинт (/join) фиксирует факт, что пользователь с accessToken пытается присоединиться к серверу с serverId.
                // Подпись serverId происходит при вызове hasJoined с *его* serverId, но *после* вызова /join.
                // В простейшем случае, если все проверки выше прошли, возвращаем 204 No Content (успешно).
                // Более сложные реализации могут хранить сессии, отслеживать serverId и т.д.

                _logger.LogDebug("Successful join request for user_UUID {UserIdFromToken} with serverId {ServerId}.", userIdFromToken, serverId);

                // Для совместимости с authlib-injector и Minecraft, сервер игры ожидает 204 No Content при успехе.
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during join request.");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}