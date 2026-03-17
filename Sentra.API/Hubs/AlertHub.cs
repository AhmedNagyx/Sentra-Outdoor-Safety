using Microsoft.AspNetCore.SignalR;

namespace Sentra.API.Hubs
{
    public class AlertHub : Hub
    {
        // Called when web client connects
        // Client joins a group named by their userId
        // so alerts are sent only to the right user
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
    }
}