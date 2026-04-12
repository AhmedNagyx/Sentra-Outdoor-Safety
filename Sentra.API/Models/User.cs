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
        [RegularExpression("^(Resident)$", ErrorMessage = "Invalid role")] // ← ADD THIS
        public string Role { get; set; } = UserRoles.Resident;

        [MaxLength(300)]
        public string? FCMToken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public ICollection<Camera> Cameras { get; set; } = new List<Camera>();
    }

    public static class UserRoles
    {
        public const string Resident = "Resident";
    }
}