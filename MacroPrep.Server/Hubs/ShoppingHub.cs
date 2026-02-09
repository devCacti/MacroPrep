using Microsoft.AspNetCore.SignalR;

namespace MacroPrep.Server.Hubs
{
    public class ShoppingHub : Hub
    {
        public async Task JoinList(Guid listId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, listId.ToString());
        }

        public async Task LeaveList(Guid listId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, listId.ToString());
        }

        public async Task NotifyListUpdated(Guid listId)
        {
            await Clients.Group(listId.ToString()).SendAsync("ListUpdated", listId);
        }

        public async Task NotifyUserAccepted(Guid listId, Guid userId)
        {
            await Clients.Group(listId.ToString()).SendAsync("InviteAccepted", listId, userId);
        }
    }
}
