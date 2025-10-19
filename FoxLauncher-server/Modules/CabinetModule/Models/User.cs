using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using FoxLauncher.Modules.AuthModule.Models;

namespace FoxLauncher.Modules.CabinetModule.Models
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
        
        [MaxLength(36)]
        public string UUID { get; set; } = string.Empty;

        public int? CurrentSkinId { get; set; }
        public int? CurrentCapeId { get; set; }

        public bool IsUser { get; set; } = true;

        public virtual Skin? CurrentSkin { get; set; }
        public virtual Cape? CurrentCape { get; set; }
        public virtual ICollection<Skin> Skins { get; set; } = new List<Skin>();
        public virtual ICollection<Cape> Capes { get; set; } = new List<Cape>();
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}