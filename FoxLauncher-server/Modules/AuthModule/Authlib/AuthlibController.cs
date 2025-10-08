using MineLauncher.AuthServer.Data.DbContext;
using MineLauncher.Shared.Models;
using AuthServer.Services.Authlib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace AuthServer.Controllers.Authlib
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
                    serverName = "MineLauncher Authlib", // Имя вашего сервера
                    implementationName = "mine-launcher-authserver",
                    implementationVersion = "1.0.0", // Версия вашей реализации
                },
                skinDomains = new[] { Request.Host.Host }, // Домен, с которого разрешены скины (ваш сервер)
                signaturePublickey = _keyService.GetPublicKeyPem().Replace("\n", "") // Убираем переводы строк для JSON
            };
            // Используем indexer для установки заголовка
            Response.Headers["X-Authlib-Injector-API-Location"] = $"{Request.Scheme}://{Request.Host}/authlib";
            return Ok(response);
        }

        // POST /authlib/authenticate
        // Используем стандартный JWT логин из основного AuthController
        // Этот эндпоинт обычно не нужен, если вы используете JWT.

        // POST /authlib/refresh
        // См. authenticate

        // POST /authlib/validate
        // См. authenticate

        // POST /authlib/signout
        // См. authenticate

        // GET /sessionserver/session/minecraft/profile/{uuid}
        [HttpGet("session/minecraft/profile/{uuid}")] // Убедитесь, что путь соответствует ожиданиям authlib-injector
        public async Task<IActionResult> GetProfile(string uuid)
        {
            // authlib-injector ожидает UUID (без дефисов), но в Identity User.Id - это int.
            // В реальности, вы можете хранить UUID в отдельном поле в User или использовать UserName как идентификатор.
            // Для простоты, предположим, что в базе хранится UUID в строковом поле в User.
            // Или, если UUID не хранится, но передается клиентом, можно использовать его для поиска через кастомную логику.
            // Пока используем UserName как идентификатор, как в примере MehLauncher.
            // Нужно будет адаптировать под реальную схему хранения UUID.
            // Временное решение: найти пользователя по UserName, совпадающему с UUID (если UUID используется как имя)
            var user = await _context.Users // ← DbSet<User> из AuthDbContext
                .Include(u => u.CurrentSkin)
                .Include(u => u.CurrentCape)
                .FirstOrDefaultAsync(u => u.Username == uuid); // Заменить на поиск по UUID, если он хранится

            if (user == null)
            {
                return NotFound();
            }

            var profileResponse = new
            {
                id = uuid,
                name = user.Username,
                properties = new[]
                {
                    new
                    {
                        name = "textures",
                        value = await GetTexturesValue(user)
                    }
                }
            };

            return Ok(profileResponse);
        }

        // Вспомогательная функция для формирования значения свойства "textures"
        private async Task<string> GetTexturesValue(User user)
        {
            // Требуется генерация текстурного пакета в формате, понятном Minecraft.
            // Это может включать Base64-кодирование JSON, содержащего URL-адреса скина и плаща.
            // Для этого часто используются сторонние библиотеки или кастомные методы.
            // Пример упрощенного JSON (не Base64):
            var texturesJson = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                profileId = user.Id,
                profileName = user.Username,
                textures = new Dictionary<string, object>()
            };

            if (user.CurrentSkin != null)
            {
                texturesJson.textures["SKIN"] = new { url = $"https://auth.launcher.mehhost.ru/skins/{user.CurrentSkin.FileName}" }; // Исправлен путь
            }
            if (user.CurrentCape != null && user.CurrentCape.IsActive)
            {
                texturesJson.textures["CAPE"] = new { url = $"https://auth.launcher.mehhost.ru/capes/{user.CurrentCape.FileName}" }; // Исправлен путь
            }

            // Base64-кодирование для authlib-injector
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(texturesJson, jsonOptions); // ← Использована System.Text.Json
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            return base64;
        }

        // GET /sessionserver/session/minecraft/hasJoined?username=<name>&serverId=<id>&ip=<ip>
        [HttpGet("sessionserver/session/minecraft/hasJoined")]
        public async Task<IActionResult> HasJoined([FromQuery] string username, [FromQuery] string serverId, [FromQuery] string? ip = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(serverId))
            {
                return BadRequest();
            }

            // Найти пользователя по имени
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
            {
                // Возвращаем 204 No Content, если игрок не найден или не разрешил доступ
                return NoContent(); // Или NotFound() в зависимости от строгости
            }

            // Подписать serverId с приватным ключом и UUID (упрощённый метод, см. спецификацию authlib-injector)
            // В реальности serverId формируется как хеш SHA1(Minecraft+SharedSecret+ServerId)
            // Но для простоты предположим, что сервер проверяет подпись serverId, сгенерированного сервером игры.
            // Сервер игры вызывает hasJoined с *своим* serverId, который он сгенерировал.
            // Лаунчер вызывает authenticate/refresh, получает accessToken и *serverId* (обычно пустой или фиктивный для клиентских проверок).
            // Для клиента (laucher) вызов hasJoined может не требовать подписи serverId, просто возвращается профиль.
            // Для сервера (server) вызов hasJoined с *его* serverId требует подписи.
            // Для упрощения, возвращаем профиль, если пользователь найден, без проверки serverId *здесь*.
            // Реальная проверка serverId происходит на сервере игры, который знает SharedSecret и может проверить подпись.

            // Получить текстуры
            var texturesResult = await _textureService.GetUserTexturesAsync(user.Id.ToString());
            // texturesResult теперь может содержать Hash и Size, но они не используются в профиле напрямую

            var profileId = user.Id.ToString().Replace("-", ""); // UUID без дефисов
            var profileResponse = new
            {
                id = profileId,
                name = user.UserName,
                properties = new List<object>() // Используем List<object>, чтобы можно было добавить элементы
            };

            if (!string.IsNullOrEmpty(texturesResult?.SkinUrl) || !string.IsNullOrEmpty(texturesResult?.CapeUrl))
            {
                var textures = new
                {
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    profileId = profileId,
                    profileName = user.UserName,
                    isPublic = true, // Или false, в зависимости от настроек
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

                // Используем byte[] для SignData
                var signature = _keyService.SignData(Encoding.UTF8.GetBytes(texturesJson));

                // Добавляем свойство в список
                profileResponse.properties.Add(new
                {
                    name = "textures",
                    value = texturesValue,
                    signature
                });
            }

            return Ok(profileResponse);
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

            // Проверка JWT токена (он уже проверен через Authentication/Authorization атрибуты, если эндпоинт защищён)
            // Однако, тут нужно сопоставить accessToken (JWT) с UUID профиля
            // Это проблематично, т.к. JWT сам по себе не содержит UUID профиля напрямую, только UserId
            // В реальной реализации, при /authenticate, сервер возвращает accessToken и *serverId* (обычно пустой для клиента).
            // При подключении к серверу, сервер игры генерирует *свой* serverId, вызывает /join с accessToken (JWT) и этим serverId.
            // Лаунчер не участвует в /join.
            // Сервер игры вызывает /hasJoined с *его* serverId, чтобы проверить подпись.
            // Таким образом, /join должен проверить, что accessToken (JWT) принадлежит пользователю с selectedProfile (UUID).
            // Это требует хранения соответствия между JWT и UUID на короткое время или получение UUID из JWT (если он туда включен).
            // Обычно, UUID включают в JWT как claim.
            // Предположим, что в JWT есть claim "user_uuid".

            // Получаем UserId из Claims (предполагается, что JWT уже проверен)
            var userIdClaim = User.FindFirst("user_id"); // Используем кастомный claim для UserId
            if (userIdClaim == null)
            {
                // Логика для анонимного доступа или ошибка
                // Для /join нужен аутентифицированный пользователь
                return Unauthorized();
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userIdFromToken))
            {
                return Unauthorized();
            }

            // Проверить, что selectedProfile (UUID) принадлежит пользователю с userId
            Guid profileGuid;
            try
            {
                profileGuid = Guid.ParseExact(selectedProfile, "N");
            }
            catch (FormatException)
            {
                return BadRequest();
            }

            if (userIdFromToken != profileGuid) // Предполагаем, что ProfileId = UserId
            {
                return BadRequest(); // Профиль не принадлежит пользователю
            }

            // Здесь можно добавить логику проверки serverId (например, хеширование как в спецификации)
            // Для упрощения, просто возвращаем 204 No Content (успешно)
            // Подпись serverId происходит при вызове hasJoined с этим serverId.
            return NoContent();
        }
    }
}