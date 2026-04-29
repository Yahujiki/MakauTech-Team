using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MakauTech.Hubs
{
    public class NotificationHub : Hub
    {
        // Send message to ALL connected clients
        public async Task SendNotification(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }

        // Optional: send to specific user
        public async Task SendToUser(string user, string message)
        {
            await Clients.User(user).SendAsync("ReceiveNotification", message);
        }
    }
}
