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
        [RegularExpression("^(FCM|SignalR|Email)$",
            ErrorMessage = "Channel must be FCM, SignalR, or Email")]
        public string Channel { get; set; } = AlertChannel.FCM;

        [Required]
        [MaxLength(20)]
        [RegularExpression("^(Pending|Sent|Delivered|Failed)$",
            ErrorMessage = "Invalid delivery status")]
        public string DeliveryStatus { get; set; } = AlertDeliveryStatus.Pending;

        public DateTime CreatedAt { get; set; } // set by DB
        public DateTime? DeliveredAt { get; set; }

        // Navigation
        public Incident Incident { get; set; } = null!;
        public User User { get; set; } = null!;
    }

    public static class AlertChannel
    {
        public const string FCM = "FCM";
        public const string SignalR = "SignalR";
        public const string Email = "Email";
    }

    public static class AlertDeliveryStatus
    {
        public const string Pending = "Pending";
        public const string Sent = "Sent";
        public const string Delivered = "Delivered";
        public const string Failed = "Failed";
    }
}