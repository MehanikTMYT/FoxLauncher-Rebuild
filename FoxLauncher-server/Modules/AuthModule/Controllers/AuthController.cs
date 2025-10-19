using FoxLauncher.Modules.AuthModule.Models;
using FoxLauncher.Modules.AuthModule.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.AuthModule.Controllers
{
    /// <summary>
    /// Контроллер для аутентификации пользователей, включая регистрацию, вход и подтверждение email.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Базовый маршрут для API аутентификации
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtTokenService _jwtTokenService; 
        private readonly IEmailConfirmationService _emailConfirmationService; 
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IJwtTokenService jwtTokenService, IEmailConfirmationService emailConfirmationService, ILogger<AuthController> logger) // Изменён конструктор
        {
            _authService = authService;
            _jwtTokenService = jwtTokenService; 
            _emailConfirmationService = emailConfirmationService; 
            _logger = logger;
        }

        /// <summary>
        /// Регистрация нового пользователя.
        /// </summary>
        /// <param name="model">Данные для регистрации, включая имя пользователя, email и пароль.</param>
        /// <returns>Результат операции: успешная регистрация или ошибки валидации.</returns>
        [HttpPost("register")]
        [SwaggerOperation(
            Summary = "Регистрация нового пользователя",
            Description = "Создает нового пользователя в системе. Требуется подтверждение email."
        )]
        [ProducesResponseType(typeof(object), 200)] 
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody][SwaggerRequestBody("Данные для регистрации пользователя.")] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Registration attempt with invalid model state.");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogDebug("Attempting to register user {Username} with email {Email}.", model.Username, model.Email);
                var success = await _authService.CreateUserAsync(model.Username, model.Email, model.Password);
                if (success)
                {
                    // Опционально: автоматически отправить письмо подтверждения
                    var user = await _authService.FindUserAsync(model.Username, model.Password); // Нужно получить ID
                    if (user != null)
                    {
                        await _emailConfirmationService.GenerateConfirmationTokenAsync(user.Id.ToString());
                        // Здесь можно вызвать сервис отправки email, передав ему токен и email пользователя
                        // await _emailService.SendConfirmationEmailAsync(user.Email, token);
                    }

                    _logger.LogInformation("Successfully registered user {Username}.", model.Username);
                    return Ok(new { Message = "User registered successfully. Please check your email to confirm." });
                }
                else
                {
                    _logger.LogWarning("Registration failed for user {Username}.", model.Username);
                    return BadRequest(new { Message = "Registration failed. User might already exist." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during registration for user {Username}.", model.Username);
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        /// <summary>
        /// Аутентификация пользователя и получение JWT-токена.
        /// </summary>
        /// <param name="model">Данные для входа, включая имя пользователя или email и пароль.</param>
        /// <returns>JWT-токен при успешной аутентификации или ошибку.</returns>
        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Аутентификация пользователя",
            Description = "Проверяет учетные данные пользователя и, при успехе, возвращает JWT-токен для доступа к защищенным ресурсам."
        )]
        [ProducesResponseType(typeof(object), 200)] // Уточните тип возврата (например, { token = "...", expires = ... })
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody][SwaggerRequestBody("Данные для аутентификации пользователя.")] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login attempt with invalid model state.");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogDebug("Attempting to log in user {UsernameOrEmail}.", model.UsernameOrEmail);
                var user = await _authService.FindUserAsync(model.UsernameOrEmail, model.Password);
                if (user != null && user.EmailConfirmed) // Проверяем подтверждение email
                {
                    var token = await _jwtTokenService.GenerateTokenAsync(user); // Вызов нового сервиса
                    _logger.LogInformation("Successfully logged in user {Username}.", user.Username);
                    // Возвращаем токен и основные данные пользователя
                    return Ok(new { Token = token, User = new { user.Id, user.UserName, user.Email } });
                }
                else
                {
                    _logger.LogWarning("Login failed for user {UsernameOrEmail}: Invalid credentials or unconfirmed email.", model.UsernameOrEmail);
                    return Unauthorized(new { Message = "Invalid username/email or password, or email not confirmed." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for user {UsernameOrEmail}.", model.UsernameOrEmail);
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        /// <summary>
        /// Подтверждение адреса электронной почты с использованием кода.
        /// </summary>
        /// <param name="model">Модель, содержащая ID пользователя и код подтверждения.</param>
        /// <returns>Результат подтверждения.</returns>
        [HttpPost("confirm-email")]
        [SwaggerOperation(
            Summary = "Подтверждение email",
            Description = "Подтверждает адрес электронной почты пользователя с помощью кода, отправленного на этот адрес."
        )]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ConfirmEmail([FromBody][SwaggerRequestBody("Модель с ID пользователя и кодом подтверждения.")] ConfirmEmailModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Email confirmation attempt with invalid model state.");
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogDebug("Attempting to confirm email for user {UserId} with token {Token}.", model.UserId, model.Token);
                var success = await _emailConfirmationService.ConfirmEmailAsync(model.UserId, model.Token); // Вызов нового сервиса
                if (success)
                {
                    _logger.LogInformation("Successfully confirmed email for user {UserId}.", model.UserId);
                    return Ok(new { Message = "Email confirmed successfully." });
                }
                else
                {
                    _logger.LogWarning("Failed to confirm email for user {UserId} with token {Token}.", model.UserId, model.Token);
                    return BadRequest(new { Message = "Failed to confirm email. Token might be invalid or expired." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during email confirmation for user {UserId}.", model.UserId);
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        // --- Модели для передачи данных ---
        // (Модели RegisterModel, LoginModel, ConfirmEmailModel остаются без изменений)
        /// <summary>
        /// Модель для передачи данных при регистрации пользователя.
        /// </summary>
        public class RegisterModel
        {
            /// <summary>
            /// Имя пользователя.
            /// </summary>
            [Required]
            [SwaggerSchema("Имя пользователя.")]
            public string Username { get; set; } = string.Empty;

            /// <summary>
            /// Адрес электронной почты.
            /// </summary>
            [Required]
            [EmailAddress]
            [SwaggerSchema("Адрес электронной почты.")]
            public string Email { get; set; } = string.Empty;

            /// <summary>
            /// Пароль.
            /// </summary>
            [Required]
            [SwaggerSchema("Пароль пользователя.")]
            public string Password { get; set; } = string.Empty;
        }

        /// <summary>
        /// Модель для передачи данных при аутентификации пользователя.
        /// </summary>
        public class LoginModel
        {
            /// <summary>
            /// Имя пользователя или email.
            /// </summary>
            [Required]
            [SwaggerSchema("Имя пользователя или email.")]
            public string UsernameOrEmail { get; set; } = string.Empty;

            /// <summary>
            /// Пароль.
            /// </summary>
            [Required]
            [SwaggerSchema("Пароль пользователя.")]
            public string Password { get; set; } = string.Empty;
        }

        /// <summary>
        /// Модель для передачи данных при подтверждении email.
        /// </summary>
        public class ConfirmEmailModel
        {
            /// <summary>
            /// Идентификатор пользователя.
            /// </summary>
            [Required]
            [SwaggerSchema("Идентификатор пользователя.")]
            public string UserId { get; set; } = string.Empty;

            /// <summary>
            /// Код подтверждения.
            /// </summary>
            [Required]
            [SwaggerSchema("Код подтверждения.")]
            public string Token { get; set; } = string.Empty;
        }
    }
}