using Microsoft.AspNetCore.SignalR;

namespace MacroPrep.Server.Hubs
{
    public class ShoppingHub : Hub
    {
        // Join invite group
        public async Task JoinInvite(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
            Console.WriteLine($"Client {Context.ConnectionId} joined group {userId}.");
        }

        // Leave invite group
        public async Task LeaveInvite(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
            Console.WriteLine($"Client {Context.ConnectionId} left group {userId}.");
        }

        // Join a list group
        public async Task JoinList(string listId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, listId.ToString());
            Console.WriteLine($"Client {Context.ConnectionId} joined group {listId}.");
        }

        // Leave a list group
        public async Task LeaveList(string listId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, listId.ToString());
            Console.WriteLine($"Client {Context.ConnectionId} left group {listId}.");
        }

        // Broadcast List Change Code
        //await Clients.Group(listId.ToString()).SendAsync("ListUpdated", listId);

        // Broadcast List Deletion Code
        //await Clients.Group(listId.ToString()).SendAsync("ListDeleted", listId);

        // Broadcast Item Change Code
        //await Clients.Group(listId.ToString()).SendAsync("ItemUpdated", itemId);

        // Broadcast Item Deletion Code
        //await Clients.Group(listId.ToString()).SendAsync("ItemDeleted", itemId);

        // Broadcast Item New Invite Code
        //await Clients.Group(listId.ToString()).SendAsync("NewInvite", inviteId);
    }
}
