using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.FileModule.Models
{
    public class GameFile
    {
        public int Id { get; set; }

        [Required]
        public int VersionId { get; set; }

        [MaxLength(1000)]
        [Required]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(64)]
        [Required]
        public string Hash { get; set; } = string.Empty;

        public long Size { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Version Version { get; set; } = null!;
    }
}