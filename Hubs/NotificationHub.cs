using Microsoft.AspNetCore.SignalR;

namespace MakauTech.Hubs
{
    /// <summary>
    /// SignalR Hub for realtime notifications across MakauTech.
    /// Connected clients receive instant updates when events happen.
    /// </summary>
    public class NotificationHub : Hub
    {
        /// <summary>Broadcast a notification to all connected users.</summary>
        public async Task SendNotification(string message, string type)
        {
            await Clients.All.SendAsync("ReceiveNotification", message, type);
        }

        /// <summary>Broadcast leaderboard score change to all users.</summary>
        public async Task LeaderboardChanged(string userName, int newScore)
        {
            await Clients.All.SendAsync("LeaderboardUpdate", userName, newScore);
        }

        /// <summary>Broadcast when someone earns a new badge.</summary>
        public async Task BadgeEarned(string userName, string badgeName)
        {
            await Clients.All.SendAsync("BadgeEarned", userName, badgeName);
        }
    }
}
