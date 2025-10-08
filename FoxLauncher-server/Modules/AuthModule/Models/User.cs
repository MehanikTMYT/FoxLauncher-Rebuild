using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.AuthModule.Models
{
    public class User : IdentityUser<int>
    {
        [MaxLength(255)]
        public string? Username { get; set; }
        public string? Email { get; set; }

        public bool EmailConfirmed { get; set; } = false;

        [MaxLength(255)]
        public string? EmailConfirmationToken { get; set; }

        public DateTime? EmailTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        // Внешние ключи, ссылающиеся на записи в БД CabinetModule
        public int? CurrentSkinId { get; set; }
        public int? CurrentCapeId { get; set; }

        public bool IsUser { get; set; } = true;

        // Навигационные свойства (требуют загрузки через Include или отдельный запрос)
        // Эти сущности управляются в CabinetModule
        public virtual object? CurrentSkin { get; set; } // Тип object, так как сущность из другого модуля
        public virtual object? CurrentCape { get; set; } // Тип object, так как сущность из другого модуля
        public virtual ICollection<object> Skins { get; set; } = new List<object>(); // Тип object
        public virtual ICollection<object> Capes { get; set; } = new List<object>(); // Тип object
        public virtual ICollection<object> Sessions { get; set; } = new List<object>(); // Или создайте Session в AuthModule, если она только там нужна
    }
}