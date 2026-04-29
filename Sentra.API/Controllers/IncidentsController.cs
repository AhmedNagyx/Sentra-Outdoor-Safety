using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentra.API.Data;
using Sentra.API.Models;
using Sentra.API.Models.DTOs;
using Sentra.API.Models.YourNamespace.Models;
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
        // Called by AI service — one POST per detected type
        // ==============================================
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ReportIncident(IncidentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var camera = await _db.Cameras
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CameraId == dto.CameraId);

            if (camera == null)
                return NotFound(new { message = $"Camera {dto.CameraId} not found" });

            var incident = new Incident
            {
                CameraId = dto.CameraId,
                Timestamp = dto.Timestamp,
                DetectedBy = dto.DetectedBy,
                Status = IncidentStatus.Pending
            };

            incident.Detections.Add(new IncidentDetection
            {
                Type = dto.Type,
                ConfidenceScore = dto.ConfidenceScore
            });

            _db.Incidents.Add(incident);
            await _db.SaveChangesAsync();

            // Save screenshot to disk if provided
            if (!string.IsNullOrEmpty(dto.SnapshotBase64))
            {
                try
                {
                    var folder = Path.Combine("wwwroot", "snapshots");
                    Directory.CreateDirectory(folder);

                    var fileName = $"{incident.IncidentId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                    var filePath = Path.Combine(folder, fileName);
                    var imageBytes = Convert.FromBase64String(dto.SnapshotBase64);

                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                    incident.SnapshotPath = $"/snapshots/{fileName}";
                    await _db.SaveChangesAsync();
                }
                catch (FormatException)
                {
                    // Invalid base64 — skip screenshot, don't fail the whole request
                }
            }

            // Notification content
            var title = $"Sentra Alert — {dto.Type} Detected";
            var body = $"{dto.Type} detected at {camera.Name} " +
                        $"({dto.ConfidenceScore:P0} confidence)";

            // Save alert record
            var alert = new Alert
            {
                IncidentId = incident.IncidentId,
                UserId = camera.UserId,
                Message = body,
                Channel = AlertChannel.FCM,
                DeliveryStatus = AlertDeliveryStatus.Pending
            };

            _db.Alerts.Add(alert);
            await _db.SaveChangesAsync();

            var data = new Dictionary<string, string>
            {
                { "incidentId", incident.IncidentId.ToString() },
                { "type",       dto.Type },
                { "cameraId",   dto.CameraId.ToString() },
                { "cameraName", camera.Name },
                { "snapshot",   incident.SnapshotPath ?? "" }
            };

            // 1. Firebase → mobile push
            bool isNotificationSent = false;
            if (!string.IsNullOrEmpty(camera.User.FCMToken))
            {
                await _notifications.SendFirebaseNotificationAsync(
                    camera.User.FCMToken, title, body, data);

                alert.DeliveryStatus = AlertDeliveryStatus.Sent;
                alert.DeliveredAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                isNotificationSent = true;
            }

            // 2. SignalR → web app live alert
            await _notifications.SendSignalRNotificationAsync(
                camera.UserId, title, body, data);

            return Ok(new
            {
                message = "Incident reported successfully",
                incidentId = incident.IncidentId,
                firebaseStatus = isNotificationSent
                    ? "Sent"
                    : "Failed: FCM Token is NULL or Empty for this user!"
            });
        }

        // ==============================================
        // GET /api/incidents
        // ==============================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetIncidents()
        {
            var userId = GetUserId();

            var incidents = await _db.Incidents
                .Include(i => i.Camera)
                .Include(i => i.Detections)
                .Where(i => i.Camera.UserId == userId)
                .OrderByDescending(i => i.Timestamp)
                .Select(i => new
                {
                    i.IncidentId,
                    i.Timestamp,
                    i.Status,
                    i.SnapshotPath,
                    i.VideoClipPath,
                    i.DetectedBy,
                    type = i.Detections.Select(d => d.Type).FirstOrDefault(),
                    confidenceScore = i.Detections.Select(d => d.ConfidenceScore).FirstOrDefault(),
                    Camera = new { i.Camera.CameraId, i.Camera.Name, i.Camera.Location }
                })
                .ToListAsync();

            return Ok(incidents);
        }

        // ==============================================
        // GET /api/incidents/{id}/snapshot
        // ==============================================
        [HttpGet("{id}/snapshot")]
        [Authorize]
        public async Task<IActionResult> GetSnapshot(int id)
        {
            var userId = GetUserId();

            var incident = await _db.Incidents
                .Include(i => i.Camera)
                .FirstOrDefaultAsync(i => i.IncidentId == id);

            if (incident == null)
                return NotFound(new { message = "Incident not found" });

            if (incident.Camera.UserId != userId)
                return Forbid();

            if (string.IsNullOrEmpty(incident.SnapshotPath))
                return NotFound(new { message = "No snapshot available for this incident" });

            var filePath = Path.Combine("wwwroot", incident.SnapshotPath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Snapshot file not found on disk" });

            var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(imageBytes, "image/jpeg");
        }

        // ==============================================
        // PATCH /api/incidents/{id}/status
        // ==============================================
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