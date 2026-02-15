using System.ComponentModel.DataAnnotations;

namespace Sentra.API.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHashed { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Resident"; // "Admin" or "Resident"

        public string? FCMToken { get; set; } // For push notifications

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Navigation property - One User has Many Cameras
        public ICollection<Camera> Cameras { get; set; } = new List<Camera>();
    }
}