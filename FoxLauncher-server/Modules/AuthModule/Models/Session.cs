using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.AuthModule.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(512)]
        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [MaxLength(512)]
        [Required]
        public string ClientToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        public virtual User User { get; set; } = null!;
    }
}