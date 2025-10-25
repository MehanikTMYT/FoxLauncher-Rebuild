using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoxLauncher.Modules.CabinetModule.Models
{
    public class Cape
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(500)]
        [Required]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Required]
        public string OriginalName { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? Hash { get; set; }

        public long? Size { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = false;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}