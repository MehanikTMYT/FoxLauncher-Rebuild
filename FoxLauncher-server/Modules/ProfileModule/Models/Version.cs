using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.ProfileModule.Models
{
    public class Version
    {
        public int Id { get; set; }

        [Required]
        public int ProfileId { get; set; }

        [MaxLength(255)]
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? JarPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Profile Profile { get; set; } = null!;
        public virtual ICollection<GameFile> Files { get; set; } = new List<GameFile>();
    }
}