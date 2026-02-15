using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sentra.API.Models
{
    public class Incident
    {
        [Key]
        public int IncidentId { get; set; }

        [ForeignKey("Camera")]
        public int CameraId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // "Fire", "Violence", "Accident"

        [Required]
        [Range(0.0, 1.0)]
        public double ConfidenceScore { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? SnapshotPath { get; set; }

        [MaxLength(500)]
        public string? VideoClipPath { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Verified", "FalseAlarm", "Resolved"

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedByUserId { get; set; }

        // Navigation properties
        public Camera Camera { get; set; } = null!;

        [ForeignKey("ResolvedByUserId")]
        public User? ResolvedByUser { get; set; }

        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}