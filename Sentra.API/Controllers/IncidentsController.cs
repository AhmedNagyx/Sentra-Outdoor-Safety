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
    [Route("api/incidents")]
    public class IncidentsController : ControllerBase
    {
        private readonly SentraDbContext _db;

        public IncidentsController(SentraDbContext db)
        {
            _db = db;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ==============================================
        // POST /api/incidents
        // Called by AI service when incident detected
        // Protected by API Key middleware
        // ==============================================
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ReportIncident(IncidentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var camera = await _db.Cameras
                .FirstOrDefaultAsync(c => c.CameraId == dto.CameraId);

            if (camera == null)
                return NotFound(new { message = $"Camera {dto.CameraId} not found" });

            var incident = new Incident
            {
                CameraId = dto.CameraId,
                Type = dto.Type,
                ConfidenceScore = dto.ConfidenceScore,
                Timestamp = dto.Timestamp,
                SnapshotPath = dto.SnapshotPath,
                VideoClipPath = dto.VideoClipPath,
                DetectedBy = dto.DetectedBy,
                Status = IncidentStatus.Pending
            };

            _db.Incidents.Add(incident);
            await _db.SaveChangesAsync();

            // Create alert for camera owner
            var alert = new Alert
            {
                IncidentId = incident.IncidentId,
                UserId = camera.UserId,
                Message = $"{dto.Type} detected at {camera.Name} with {dto.ConfidenceScore:P0} confidence",
                Channel = AlertChannel.FCM,
                DeliveryStatus = AlertDeliveryStatus.Pending
            };

            _db.Alerts.Add(alert);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Incident reported successfully",
                incidentId = incident.IncidentId
            });
        }

        // ==============================================
        // GET /api/incidents
        // Resident sees only their own incidents
        // ==============================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetIncidents()
        {
            var userId = GetUserId(); // ← removed role check, always filter by owner

            var incidents = await _db.Incidents
                .Include(i => i.Camera)
                .Where(i => i.Camera.UserId == userId) // ← always applied now
                .OrderByDescending(i => i.Timestamp)
                .Select(i => new
                {
                    i.IncidentId,
                    i.Type,
                    i.ConfidenceScore,
                    i.Timestamp,
                    i.Status,
                    i.SnapshotPath,
                    i.VideoClipPath,
                    i.DetectedBy,
                    Camera = new { i.Camera.CameraId, i.Camera.Name, i.Camera.Location }
                })
                .ToListAsync();

            return Ok(incidents);
        }

        // ==============================================
        // PATCH /api/incidents/{id}/status
        // Resident marks their own incident
        // ==============================================
        [HttpPatch("{id}/status")]
        [Authorize] // ← removed Admin role requirement
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateIncidentStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            var incident = await _db.Incidents
                .Include(i => i.Camera)
                .FirstOrDefaultAsync(i => i.IncidentId == id);

            if (incident == null)
                return NotFound(new { message = "Incident not found" });

            // Only owner can update status ← added ownership check
            if (incident.Camera.UserId != userId)
                return Forbid();

            incident.Status = dto.Status;

            if (dto.Status == IncidentStatus.Resolved)
            {
                incident.ResolvedAt = DateTime.UtcNow;
                incident.ResolvedByUserId = userId;
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Status updated", incident.Status });
        }
    }
}