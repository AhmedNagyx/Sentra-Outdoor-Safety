using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentra.API.Data;
using Sentra.API.Models;
using Sentra.API.Models.DTOs;
using System.Security.Claims;

namespace Sentra.API.Controllers
{
    [ApiController]
    [Route("api/cameras")]
    [Authorize]
    public class CamerasController : ControllerBase
    {
        private readonly SentraDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public CamerasController(
            SentraDbContext db,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ==============================================
        // GET /api/cameras
        // ==============================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();

            var cameras = await _db.Cameras
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CameraId,
                    c.Name,
                    c.Location,
                    c.StreamURL,
                    c.Status,
                    c.CreatedAt,
                    c.LastActiveAt
                })
                .ToListAsync();

            return Ok(cameras);
        }

        // ==============================================
        // GET /api/cameras/{id}
        // ==============================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();

            var camera = await _db.Cameras
                .FirstOrDefaultAsync(c => c.CameraId == id);

            if (camera == null)
                return NotFound(new { message = "Camera not found" });

            if (camera.UserId != userId)
                return Forbid();

            return Ok(new
            {
                camera.CameraId,
                camera.Name,
                camera.Location,
                camera.StreamURL,
                camera.Status,
                camera.CreatedAt,
                camera.LastActiveAt
            });
        }

        // ==============================================
        // POST /api/cameras
        // ==============================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CameraDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            if (await _db.Cameras.AnyAsync(c => c.StreamURL == dto.StreamURL))
                return BadRequest(new { message = "A camera with this stream URL already exists" });

            var camera = new Camera
            {
                UserId = userId,
                Name = dto.Name,
                Location = dto.Location,
                StreamURL = dto.StreamURL,
                Status = CameraStatus.Active
            };

            _db.Cameras.Add(camera);
            await _db.SaveChangesAsync();

            // Register camera with AI service
            // Fire-and-forget — if AI is offline, camera is still saved in DB
            _ = RegisterWithAiServiceAsync(camera);

            return CreatedAtAction(nameof(GetById),
                new { id = camera.CameraId },
                new
                {
                    message = "Camera created successfully",
                    camera.CameraId,
                    camera.Name,
                    camera.Location,
                    camera.StreamURL,
                    camera.Status
                });
        }

        private async Task RegisterWithAiServiceAsync(Camera camera)
        {
            try
            {
                var aiBaseUrl = _config["AiService:BaseUrl"];
                if (string.IsNullOrEmpty(aiBaseUrl))
                    return; // AI service not configured — skip silently

                var client = _httpClientFactory.CreateClient();

                var payload = new
                {
                    camera_id = $"cam_{camera.CameraId}",
                    rtsp_url = camera.StreamURL,
                    backend_camera_id = camera.CameraId,  // integer from DB — required
                    cooldown_seconds = 60
                };

                await client.PostAsJsonAsync($"{aiBaseUrl}/streams", payload);
            }
            catch
            {
                // AI service unreachable — log in production, ignore here
                // Camera is already saved in DB so this is non-fatal
            }
        }

        // ==============================================
        // PATCH /api/cameras/{id}
        // ==============================================
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCameraDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            var camera = await _db.Cameras
                .FirstOrDefaultAsync(c => c.CameraId == id);

            if (camera == null)
                return NotFound(new { message = "Camera not found" });

            if (camera.UserId != userId)
                return Forbid();

            if (dto.Name != null) camera.Name = dto.Name;
            if (dto.Location != null) camera.Location = dto.Location;
            if (dto.StreamURL != null) camera.StreamURL = dto.StreamURL;
            if (dto.Status != null) camera.Status = dto.Status;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Camera updated successfully",
                camera.CameraId,
                camera.Name,
                camera.Location,
                camera.StreamURL,
                camera.Status
            });
        }

        // ==============================================
        // DELETE /api/cameras/{id}
        // ==============================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();

            var camera = await _db.Cameras
                .FirstOrDefaultAsync(c => c.CameraId == id);

            if (camera == null)
                return NotFound(new { message = "Camera not found" });

            if (camera.UserId != userId)
                return Forbid();

            camera.IsDeleted = true;
            camera.Status = CameraStatus.Offline;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Camera deleted successfully" });
        }

        // ==============================================
        // PATCH /api/cameras/{id}/heartbeat
        // Called by AI service
        // ==============================================
        [HttpPatch("{id}/heartbeat")]
        [AllowAnonymous]
        public async Task<IActionResult> Heartbeat(int id)
        {
            var camera = await _db.Cameras.FindAsync(id);

            if (camera == null)
                return NotFound(new { message = "Camera not found" });

            camera.LastActiveAt = DateTime.UtcNow;
            camera.Status = CameraStatus.Active;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Heartbeat received" });
        }
    }
}