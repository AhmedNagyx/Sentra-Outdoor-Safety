using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentra.API.Data;
using Sentra.API.Models;
using Sentra.API.Models.DTOs;
using Sentra.API.Services;
using System.Security.Claims;

namespace Sentra.API.Controllers
{
    [ApiController]
    [Route("api/incidents")]
    public class IncidentsController : ControllerBase
    {
        private readonly SentraDbContext _db;
        private readonly INotificationService _notifications;

        public IncidentsController(
            SentraDbContext db,
            INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ==============================================
        // POST /api/incidents
        // ==============================================
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ReportIncident(IncidentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Load camera + owner in one query
            var camera = await _db.Cameras
                .Include(c => c.User) // need FCMToken + UserId
                .FirstOrDefaultAsync(c => c.CameraId == dto.CameraId);

            if (camera == null)
                return NotFound(new { message = $"Camera {dto.CameraId} not found" });

            // Save incident
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

            // Save alert record
            var alert = new Alert
            {
                IncidentId = incident.IncidentId,
                UserId = camera.UserId,
                Message = $"{dto.Type} detected at {camera.Name} " +
                          $"with {dto.ConfidenceScore:P0} confidence",
                Channel = AlertChannel.FCM,
                DeliveryStatus = AlertDeliveryStatus.Pending
            };

            _db.Alerts.Add(alert);
            await _db.SaveChangesAsync();

            // Notification payload
            var title = $"Sentra Alert — {dto.Type} Detected";
            var body = $"{dto.Type} detected at {camera.Name} " +
                       $"({dto.ConfidenceScore:P0} confidence)";

            var data = new Dictionary<string, string>
            {
                { "incidentId", incident.IncidentId.ToString() },
                { "type",       dto.Type },
                { "cameraId",   dto.CameraId.ToString() },
                { "cameraName", camera.Name },
                { "snapshot",   dto.SnapshotPath ?? "" }
            };

            // 1. Firebase → mobile push
            if (!string.IsNullOrEmpty(camera.User.FCMToken))
            {
                await _notifications.SendFirebaseNotificationAsync(
                    camera.User.FCMToken, title, body, data);

                // Update alert delivery status
                alert.DeliveryStatus = AlertDeliveryStatus.Sent;
                alert.DeliveredAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // 2. SignalR → web app live alert
            await _notifications.SendSignalRNotificationAsync(
                camera.UserId, title, body, data);

            return Ok(new
            {
                message = "Incident reported successfully",
                incidentId = incident.IncidentId
            });
        }

        // GET and PATCH endpoints unchanged from before...
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetIncidents()
        {
            var userId = GetUserId();

            var incidents = await _db.Incidents
                .Include(i => i.Camera)
                .Where(i => i.Camera.UserId == userId)
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

        [HttpPatch("{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus(
            int id, [FromBody] UpdateIncidentStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();

            var incident = await _db.Incidents
                .Include(i => i.Camera)
                .FirstOrDefaultAsync(i => i.IncidentId == id);

            if (incident == null)
                return NotFound(new { message = "Incident not found" });

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