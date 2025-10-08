using FoxLauncher.Modules.AuthModule.Models;
using FoxLauncher.Modules.AuthModule.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.AuthModule.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Базовый маршрут для API аутентификации
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _authService.CreateUserAsync(model.Username, model.Email, model.Password);
            if (success)
            {
                return Ok(new { Message = "User registered successfully. Please check your email to confirm." });
            }

            return BadRequest(new { Message = "Registration failed." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _authService.FindUserAsync(model.UsernameOrEmail, model.Password);
            if (user != null && user.EmailConfirmed) // Проверяем подтверждение email
            {
                var token = await _authService.GenerateJwtTokenAsync(user);
                return Ok(new { Token = token, User = new { user.Id, user.UserName, user.Email } });
            }

            return Unauthorized(new { Message = "Invalid username/email or password, or email not confirmed." });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
        {
            var success = await _authService.ConfirmEmailAsync(model.UserId, model.Token);
            if (success)
            {
                return Ok(new { Message = "Email confirmed successfully." });
            }
            return BadRequest(new { Message = "Failed to confirm email." });
        }

        // Модели для передачи данных
        public class RegisterModel
        {
            [Required] public string Username { get; set; } = string.Empty;
            [Required][EmailAddress] public string Email { get; set; } = string.Empty;
            [Required] public string Password { get; set; } = string.Empty;
        }

        public class LoginModel
        {
            [Required] public string UsernameOrEmail { get; set; } = string.Empty;
            [Required] public string Password { get; set; } = string.Empty;
        }

        public class ConfirmEmailModel
        {
            [Required] public string UserId { get; set; } = string.Empty;
            [Required] public string Token { get; set; } = string.Empty;
        }
    }
}