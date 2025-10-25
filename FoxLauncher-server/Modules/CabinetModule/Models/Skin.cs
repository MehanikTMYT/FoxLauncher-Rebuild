using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.CabinetModule.Models
{
    public class Skin
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

        public bool IsDefault { get; set; } = false;

        public virtual User User { get; set; } = null!;
        public virtual ICollection<User>? UsersWithThisSkinAsCurrent { get; set; }
    }
}