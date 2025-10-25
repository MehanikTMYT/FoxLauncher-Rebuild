using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FoxLauncher.Modules.AuthModule.Models;

namespace FoxLauncher.Modules.AuthModule.Services
{
    /// <summary>
    /// Сервис для генерации JWT-токенов.
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenService> _logger;
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="JwtTokenService"/>.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <param name="logger">Логгер.</param>
        /// <param name="userManager">Менеджер пользователей ASP.NET Core Identity.</param>
        public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger, UserManager<User> userManager)
        {
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Генерирует JWT-токен для указанного пользователя.
        /// </summary>
        /// <param name="user">Пользователь, для которого генерируется токен.</param>
        /// <returns>Строка JWT-токена.</returns>
        public async Task<string> GenerateTokenAsync(User user)
        {
            _logger.LogDebug("Generating JWT token for user {UserId} ({Username}).", user.Id, user.UserName);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSecret = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json");
            var key = Convert.FromBase64String(jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("user_uuid", user.Uuid),
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogDebug("JWT token generated successfully for user {UserId}.", user.Id);
            return await Task.FromResult(tokenString);
        }
    }
}