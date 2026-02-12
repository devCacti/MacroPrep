using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace MacroPrep.Server.Hubs
{
    public interface IShoppingHubClient
    {
        Task ListUpdated(string listId);
        Task ListDeleted(string listId);
        Task ItemUpdated(string itemId);
        Task ItemDeleted(string itemId);
        Task InviteReceived(string listId);
        Task InviteAccepted(string listId, string userName);
        Task InviteRejected(string listId, string userName);
        Task MemberLeftList(string listId, string userName);
        Task ListAccessRemoved(string listId);
    }

    [Authorize]
    public class ShoppingHub : Hub<IShoppingHubClient>
    {
        // Join invite group
        public async Task JoinInvite(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
            Console.WriteLine($"\n > Client {Context.ConnectionId} joined group {userId}.\n");
        }

        // Leave invite group
        public async Task LeaveInvite(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
            Console.WriteLine($"\n > Client {Context.ConnectionId} left group {userId}.\n");
        }

        // Join a list group
        public async Task JoinList(string listId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, listId.ToString());
            Console.WriteLine($"\n > Client {Context.ConnectionId} joined group {listId}.\n");
        }

        // Leave a list group
        public async Task LeaveList(string listId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, listId.ToString());
            Console.WriteLine($"\n > Client {Context.ConnectionId} left group {listId}.\n");
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
