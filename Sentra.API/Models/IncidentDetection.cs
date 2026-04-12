using System.ComponentModel.DataAnnotations;

namespace Sentra.API.Models
{
    using System.ComponentModel.DataAnnotations;

    namespace YourNamespace.Models
    {
        public class IncidentDetection
        {
            public int Id { get; set; }
            public int IncidentId { get; set; }

            [Required]
            [MaxLength(20)]
            public string Type { get; set; } = default!;

            [Range(0f, 1f)]
            public float ConfidenceScore { get; set; }

            public Incident Incident { get; set; } = null!;
        }
    }
}
