using System.ComponentModel.DataAnnotations;
namespace Sentra.API.Models.DTOs
{
    public class CameraDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;
        [Required]
        [MaxLength(500)]
        [RegularExpression(@"^(rtsp|rtsps|http|https)://.+",
            ErrorMessage = "StreamURL must start with rtsp://, rtsps://, http://, or https://")]
        public string StreamURL { get; set; } = string.Empty;
    }
    public class UpdateCameraDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        [MaxLength(255)]
        public string? Location { get; set; }
        [MaxLength(500)]
        [RegularExpression(@"^(rtsp|rtsps|http|https)://.+",
            ErrorMessage = "StreamURL must start with rtsp://, rtsps://, http://, or https://")]
        public string? StreamURL { get; set; }
        [RegularExpression("^(Active|Inactive|Offline)$",
            ErrorMessage = "Status must be Active, Inactive, or Offline")]
        public string? Status { get; set; }
    }
}