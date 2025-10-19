using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.AuthModule.Models;
using FoxLauncher.Modules.AuthModule.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using System.Text;
using System.Text.Json;

namespace FoxLauncher.Modules.AuthModule.Controllers
{
    [ApiController]
    [Route("authlib")] // Базовый путь для authlib
    public class AuthlibController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IAuthlibKeyService _keyService;
        private readonly ITextureService _textureService;

        public AuthlibController(AuthDbContext context, IAuthlibKeyService keyService, ITextureService textureService)
        {
            _context = context;
            _keyService = keyService;
            _textureService = textureService;
        }

        // GET /authlib/
        [HttpGet]
        public IActionResult GetAuthlibInfo()
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

        // GET /sessionserver/session/minecraft/profile/{UUID}
        [HttpGet("session/minecraft/profile/{UUID}")]
        public async Task<IActionResult> GetProfile(string UUID) // UUID теперь ожидается как UUID
        {
            // Ищем пользователя по его UUID (исправлено с UUID на UUID)
            var user = await _context.Users
                .Include(u => u.CurrentSkin)
                .Include(u => u.CurrentCape)
                .FirstOrDefaultAsync(u => u.UUID == UUID); 

            if (user == null)
            {
                return NotFound();
            }

            // Получить текстуры через TextureService
            var texturesResult = await _textureService.GetUserTexturesAsync(user.UUID);

            var profileResponse = new
            {
                id = user.UUID, 
                name = user.Username,
                properties = new List<object>()
            };

            if (!string.IsNullOrEmpty(texturesResult?.SkinUrl) || !string.IsNullOrEmpty(texturesResult?.CapeUrl))
            {
                var textures = new
                {
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    profileId = user.UUID,
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

        // GET /sessionserver/session/minecraft/hasJoined?username=<name>&serverId=<id>&ip=<ip>
        [HttpGet("sessionserver/session/minecraft/hasJoined")]
        public async Task<IActionResult> HasJoined([FromQuery] string username, [FromQuery] string? serverId = null, [FromQuery] string? ip = null)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest();
            }

            // Найти пользователя по имени (для клиента, который может не знать UUID)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                // Возвращаем 204 No Content, если игрок не найден
                return NoContent();
            }

            // Если serverId не предоставлен, возвращаем профиль для клиента
            if (string.IsNullOrEmpty(serverId))
            {
                // Получить текстуры через TextureService
                var texturesResult = await _textureService.GetUserTexturesAsync(user.UUID); // Использование правильного свойства UUID

                var profileResponse = new
                {
                    id = user.UUID, // Использование правильного свойства UUID для ответа клиента
                    name = user.Username,
                    properties = new List<object>()
                };

                if (!string.IsNullOrEmpty(texturesResult?.SkinUrl) || !string.IsNullOrEmpty(texturesResult?.CapeUrl))
                {
                    var textures = new
                    {
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        profileId = user.UUID, // Использование правильного свойства UUID
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
            else
            {
                // Если serverId предоставлен, это запрос от сервера игры
                // Требуется подпись serverId с приватным ключом и UUID игрока
                // В реальности serverId формируется как хеш SHA1(Minecraft+SharedSecret+ServerId)
                // Сервер игры проверяет подпись, используя наш публичный ключ
                // Для этого нужно сформировать строку "serverId + UUID" и подписать её
                var verificationString = serverId + user.UUID; // Использование правильного свойства UUID
                var signature = _keyService.SignData(Encoding.UTF8.GetBytes(verificationString));

                // Возвращаем профиль с подписью serverId - это стандарт authlib-injector
                var texturesResult = await _textureService.GetUserTexturesAsync(user.UUID); // Использование правильного свойства UUID

                var profileResponse = new
                {
                    id = user.UUID, // Использование правильного свойства UUID для ответа сервера
                    name = user.Username,
                    properties = new List<object>()
                };

                if (!string.IsNullOrEmpty(texturesResult?.SkinUrl) || !string.IsNullOrEmpty(texturesResult?.CapeUrl))
                {
                    var textures = new
                    {
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        profileId = user.UUID, // Использование правильного свойства UUID
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

        // POST /sessionserver/session/minecraft/join
        // Используется сервером игры для подтверждения соединения игрока
        // Требует accessToken (JWT), selectedProfile (UUID) и serverId
        // Проверяет JWT и сопоставляет UUID с пользователем
        // Подписывает serverId с приватным ключом
        [HttpPost("sessionserver/session/minecraft/join")]
        public async Task<IActionResult> JoinServer([FromBody] JsonElement body)
        {
            string? accessToken = body.GetProperty("accessToken").GetString();
            string? selectedProfile = body.GetProperty("selectedProfile").GetString();
            string? serverId = body.GetProperty("serverId").GetString();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(selectedProfile) || string.IsNullOrEmpty(serverId))
            {
                return BadRequest();
            }

            // Проверка JWT токена (предполагается, что JWT уже проверен через Authentication/Authorization атрибуты или кастомную логику)
            // Получаем UUID пользователя из Claims (предполагается, что JWT содержит claim "user_UUID")
            var userIdClaim = User.FindFirst("user_UUID"); // Используем кастомный claim для UUID
            if (userIdClaim == null)
            {
                // Логика для анонимного доступа или ошибка
                // Для /join нужен аутентифицированный пользователь с JWT, содержащим UUID
                return Unauthorized();
            }

            var userIdFromToken = userIdClaim.Value; // Это строка UUID

            // Проверить, что selectedProfile (UUID) принадлежит пользователю с userId
            // selectedProfile - это UUID, который передал сервер игры
            // userIdFromToken - это UUID, извлеченный из JWT
            if (userIdFromToken != selectedProfile)
            {
                return BadRequest("Selected profile does not belong to the authenticated user.");
            }

            // Логика проверки serverId (например, хеширование как в спецификации)
            // В реальности, сервер игры использует serverId для вызова hasJoined и проверки подписи.
            // Этот эндпоинт (/join) фиксирует факт, что пользователь с accessToken пытается присоединиться к серверу с serverId.
            // Подпись serverId происходит при вызове hasJoined с *его* serverId, но *после* вызова /join.
            // В простейшем случае, если все проверки выше прошли, возвращаем 204 No Content (успешно).
            // Более сложные реализации могут хранить сессии, отслеживать serverId и т.д.

            // Для совместимости с authlib-injector и Minecraft, сервер игры ожидает 204 No Content при успехе.
            return NoContent();
        }
    }
}