using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.SignalR;
using Sentra.API.Hubs;

namespace Sentra.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<AlertHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<AlertHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        // ==============================================
        // FIREBASE — sends push to mobile
        // ==============================================
        public async Task SendFirebaseNotificationAsync(
            string fcmToken,
            string title,
            string body,
            object data)
        {
            try
            {
                var message = new Message
                {
                    Token = fcmToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data as Dictionary<string, string>
                        ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High, // wake device even in standby
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "sentra_alerts"
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance
                    .SendAsync(message);

                _logger.LogInformation(
                    "Firebase notification sent. MessageId: {MessageId}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Firebase notification failed: {Error}", ex.Message);
                // Don't throw — notification failure should not fail the incident report
            }
        }

        // ==============================================
        // SIGNALR — sends live alert to web app
        // ==============================================
        public async Task SendSignalRNotificationAsync(
            int userId,
            string title,
            string body,
            object data)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"user_{userId}")
                    .SendAsync("ReceiveAlert", new
                    {
                        title,
                        body,
                        data,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation(
                    "SignalR alert sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "SignalR notification failed: {Error}", ex.Message);
            }
        }
    }
}