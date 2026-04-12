namespace Sentra.API.Services
{
    public interface INotificationService
    {
        Task SendFirebaseNotificationAsync(string fcmToken, string title, string body, object data);
        Task SendSignalRNotificationAsync(int userId, string title, string body, object data);
        
    }
}