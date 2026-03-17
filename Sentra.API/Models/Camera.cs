using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sentra.API.Models
{
    public class Camera
    {
        [Key]
        public int CameraId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string StreamURL { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [RegularExpression("^(Active|Inactive|Offline)$",
            ErrorMessage = "Status must be Active, Inactive, or Offline")]
        public string Status { get; set; } = CameraStatus.Active;

        public bool IsDeleted { get; set; } = false; // soft delete

        public DateTime CreatedAt { get; set; } // set by DB
        public DateTime? LastActiveAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    }

    public static class CameraStatus
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string Offline = "Offline";
    }
}