using System.ComponentModel.DataAnnotations;

namespace Sentra.API.Models.DTOs
{
    public class IncidentDto
    {
        [Required]
        public int CameraId { get; set; }

        [Required]
        [RegularExpression("^(Fire|Violence|Accident)$",
            ErrorMessage = "Type must be Fire, Violence, or Accident")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Range(0.0, 1.0)]
        public double ConfidenceScore { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public string? SnapshotPath { get; set; }
        public string? VideoClipPath { get; set; }

        [MaxLength(50)]
        public string? DetectedBy { get; set; }
    }
}