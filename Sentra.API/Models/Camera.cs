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
        public string Status { get; set; } = "Active"; // "Active", "Inactive", "Offline"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastActiveAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    }
}