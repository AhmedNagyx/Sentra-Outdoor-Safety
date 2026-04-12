using System.ComponentModel.DataAnnotations;

namespace Sentra.API.Models.DTOs
{
    public class IncidentDto
    {
        [Required]
        public int CameraId { get; set; }

        [Required]
        [RegularExpression("^(fire|violence|accident)$",
            ErrorMessage = "Type must be fire, violence, or accident")]
        public string Type { get; set; } = default!;

        [Required]
        [Range(0f, 1f)]
        public float ConfidenceScore { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public string? SnapshotBase64 { get; set; }   // renamed from ScreenshotB64

        [MaxLength(50)]
        public string? DetectedBy { get; set; }

        // AllScores is optional context sent by AI — all three task scores
        public Dictionary<string, float>? AllScores { get; set; }
    }
}