using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sentra.API.Models
{
    public class Alert
    {
        [Key]
        public int AlertId { get; set; }

        [ForeignKey("Incident")]
        public int IncidentId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Channel { get; set; } = "FCM"; // "FCM", "SignalR", "Email"

        [Required]
        [MaxLength(20)]
        public string DeliveryStatus { get; set; } = "Pending"; // "Pending", "Sent", "Delivered", "Failed"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveredAt { get; set; }

        // Navigation properties
        public Incident Incident { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}