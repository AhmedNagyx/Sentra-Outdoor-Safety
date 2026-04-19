using System.ComponentModel.DataAnnotations;

namespace Sentra.API.Models.DTOs
{
    public class UpdateProfileDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class LogoutDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}