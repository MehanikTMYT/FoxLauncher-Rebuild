using System.ComponentModel.DataAnnotations;

namespace FoxLauncher.Modules.ProfileModule.Models
{
    public class Profile
    {
        public int Id { get; set; }

        [MaxLength(255)]
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? IconPath { get; set; }

        public int? DefaultVersionId { get; set; }

        public virtual Version? DefaultVersion { get; set; }

        public bool IsPublic { get; set; } = false;
        public virtual ICollection<Version> Versions { get; set; } = new List<Version>();
    }
}