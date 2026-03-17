using System.ComponentModel.DataAnnotations;

namespace Sentra.API.Models.DTOs
{
    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}