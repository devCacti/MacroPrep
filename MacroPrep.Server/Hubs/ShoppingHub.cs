using Microsoft.AspNetCore.SignalR;

namespace MacroPrep.Server.Hubs
{
    public class ShoppingHub : Hub
    {
        public async Task JoinList(string listId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, listId.ToString());
            Console.WriteLine($"Client {Context.ConnectionId} joined group {listId}.");
        }

        public async Task LeaveList(string listId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, listId.ToString());
            Console.WriteLine($"Client {Context.ConnectionId} left group {listId}.");
        }

        public async Task NotifyListUpdated(string listId)
        {
            await Clients.Group(listId.ToString()).SendAsync("ListUpdated", listId);
        }

        public async Task NotifyUserAccepted(string listId, string userId)
        {
            await Clients.Group(listId.ToString()).SendAsync("InviteAccepted", listId, userId);
        }
    }
}
