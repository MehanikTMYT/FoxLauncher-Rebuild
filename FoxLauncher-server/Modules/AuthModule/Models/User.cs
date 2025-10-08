using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.AuthModule.Models
{
    public class User : IdentityUser<int> 
    {
        [MaxLength(255)]
        public string? Username { get; set; }

        [MaxLength(256)] 
        public override string? Email { get; set; }

        public override bool EmailConfirmed { get; set; }

        [MaxLength(255)]
        public string? EmailConfirmationToken { get; set; }

        public DateTime? EmailTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        
        [MaxLength(36)] 
        public string Uuid { get; set; } = string.Empty; 

        // Внешние ключи, ссылающиеся на записи в БД CabinetModule
        public int? CurrentSkinId { get; set; }
        public int? CurrentCapeId { get; set; }

        public bool IsUser { get; set; } = true;

        // Навигационные свойства
        public virtual object? CurrentSkin { get; set; }
        public virtual object? CurrentCape { get; set; }
        public virtual ICollection<object> Skins { get; set; } = new List<object>();
        public virtual ICollection<object> Capes { get; set; } = new List<object>();
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}