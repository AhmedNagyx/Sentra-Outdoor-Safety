using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentra.API.Data;
using Sentra.API.Models.DTOs;
using System.Security.Claims;

namespace Sentra.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly SentraDbContext _db;

        public UserController(SentraDbContext db)
        {
            _db = db;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ==============================================
        // GET /api/user/profile
        // ==============================================
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            var cameraCount = await _db.Cameras
                .CountAsync(c => c.UserId == userId);

            var incidentCount = await _db.Incidents
                .CountAsync(i => i.Camera.UserId == userId);

            return Ok(new
            {
                user.UserId,
                user.Name,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.LastLoginAt,
                totalCameras = cameraCount,
                totalIncidents = incidentCount
            });
        }

        // ==============================================
        // PATCH /api/user/profile
        // ==============================================
        [HttpPatch("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (dto.Name != null) user.Name = dto.Name;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully",
                user.UserId,
                user.Name,
                user.Email
            });
        }

        // ==============================================
        // POST /api/user/change-password
        // ==============================================
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHashed))
                return BadRequest(new { message = "Current password is incorrect" });

            // Hash and save new password
            user.PasswordHashed = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        // ==============================================
        // POST /api/user/logout
        // ==============================================
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            // Revoke the refresh token
            var token = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken
                                       && r.UserId == userId
                                       && !r.IsRevoked);

            if (token != null)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully" });
        }
    }
}