using System.ComponentModel.DataAnnotations;

namespace Sentra.API.Models.DTOs
{
    public class UpdateIncidentStatusDto
    {
        [Required]
        [RegularExpression("^(Pending|Verified|FalseAlarm|Resolved)$",
            ErrorMessage = "Invalid status value")]
        public string Status { get; set; } = string.Empty;
    }
}