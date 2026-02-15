namespace Sentra.API.Models.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // optional — mobile app can send it
        public string? FCMToken { get; set; }
    }
}
