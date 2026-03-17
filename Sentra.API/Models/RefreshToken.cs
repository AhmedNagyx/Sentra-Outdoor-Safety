using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sentra.API.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty; // stored as hash

        [ForeignKey("User")]
        public int UserId { get; set; }

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } // set by DB

        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}