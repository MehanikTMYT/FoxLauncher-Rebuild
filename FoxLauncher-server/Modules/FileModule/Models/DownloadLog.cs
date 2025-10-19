using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.FileModule.Models
{
    public class DownloadLog
    {
        public int Id { get; set; }

        [MaxLength(1000)]
        [Required]
        public string FilePath { get; set; } = string.Empty; // Фактический путь к файлу на диске или URL

        // Внешние ключи (опционально, могут быть NULL в базе данных)
        public int? UserId { get; set; } // ID пользователя из auth_db (может быть сложно реализовать напрямую)
        public int? VersionId { get; set; } // ID версии из admin_db

        public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства (опционально, требуют дополнительных настроек и связей между DbContext)
        // public virtual User? User { get; set; }
        // public virtual ProfileModule.Models.Version? Version { get; set; }
    }
}