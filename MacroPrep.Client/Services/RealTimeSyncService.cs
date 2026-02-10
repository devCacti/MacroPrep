using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace MacroPrep.Client.Services
{
    public class RealTimeSyncService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly NavigationManager _nav;

        // Events that the UI (Pantry.razor) will subscribe to
        public event Action<Guid>? OnListUpdated;     // Triggered when a list is renamed/deleted
        public event Action<Guid>? OnItemsChanged;    // Triggered when items are added/checked
        public event Action<string>? OnNotification;  // Triggered when an invite is accepted

        public RealTimeSyncService(NavigationManager nav)
        {
            _nav = nav;
        }

        public async Task InitializeAsync(string jwtToken)
        {
            if (_hubConnection is not null) return;

            // 1. Build the connection to the Server Hub
            _hubConnection = new HubConnectionBuilder()
                //.WithUrl(_nav.ToAbsoluteUri("/api/hubs/shopping-hub"),
                .WithUrl("https://localhost:7273/api/hubs/shopping-hub",
                options =>
                {
                    // Pass the JWT token so the Hub knows who we are
                    options.AccessTokenProvider = () => Task.FromResult((string?)jwtToken);
                })
                .WithAutomaticReconnect() // Auto-retry if internet drops
                .Build();

            // 2. Register Listeners (Must match the strings used in your API)

            // Matches: await hubContext.Clients.Group(...).SendAsync("ListUpdated");
            _hubConnection.On("ListUpdated", () =>
            {
                // We could pass the GUID here if the server sends it, 
                // but for now we might just refresh the specific list if known
                // or just trigger a generic refresh. 
                // Let's assume your API sends "ListUpdated" with the listId? 
                // If not, we just refresh.
            });

            // Matches: await hubContext.Clients.Group(...).SendAsync("ListUpdated", listId);
            // (Wait, your API code used "ListUpdated" for items too. Let's standardize.)
            _hubConnection.On<Guid>("ListUpdated", (listId) =>
            {
                OnListUpdated?.Invoke(listId);
                OnItemsChanged?.Invoke(listId); // Usually implies items changed too
            });

            // Matches: await hubContext.Clients.Users(...).SendAsync("InviteAccepted", message);
            _hubConnection.On<string>("InviteAccepted", (message) =>
            {
                OnNotification?.Invoke(message);
            });

            // 3. Start the connection
            await _hubConnection.StartAsync();
        }

        public async Task JoinListGroup(Guid listId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("JoinList", listId.ToString());
            }
        }

        public async Task LeaveListGroup(Guid listId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("LeaveList", listId.ToString());
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}