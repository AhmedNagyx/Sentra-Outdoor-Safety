using System.ComponentModel.DataAnnotations;

namespace Sentra.API.Models.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        // Optional — mobile app sends this
        public string? FCMToken { get; set; }
    }
}