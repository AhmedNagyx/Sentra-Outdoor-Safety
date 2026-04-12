using Sentra.API.Models.YourNamespace.Models;
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

        public DateTime Timestamp { get; set; } // set by DB

        [MaxLength(500)]
        public string? SnapshotPath { get; set; }

        [MaxLength(500)]
        public string? VideoClipPath { get; set; }

        [Required]
        [MaxLength(20)]
        [RegularExpression("^(Pending|Verified|FalseAlarm|Resolved)$",
            ErrorMessage = "Invalid status value")]
        public string Status { get; set; } = IncidentStatus.Pending;

        [MaxLength(50)]
        public string? DetectedBy { get; set; } // "FireModel", "ViolenceModel", "AccidentModel"

        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }

        // Navigation
        public Camera Camera { get; set; } = null!;

        [ForeignKey("ResolvedByUserId")]
        public User? ResolvedByUser { get; set; }

        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
        public ICollection<IncidentDetection> Detections { get; set; } = new List<IncidentDetection>();
    }

    public static class IncidentStatus
    {
        public const string Pending = "Pending";
        public const string Verified = "Verified";
        public const string FalseAlarm = "FalseAlarm";
        public const string Resolved = "Resolved";
    }

    public static class IncidentType
    {
        public const string Fire = "Fire";
        public const string Violence = "Violence";
        public const string Accident = "Accident";
    }
}