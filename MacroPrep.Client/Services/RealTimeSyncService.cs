using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace MacroPrep.Client.Services
{
    public class RealTimeSyncService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly NavigationManager _nav;

        // Events that the UI (Pantry.razor) will subscribe to
        public event Action<string>? OnListUpdated;     // Triggered when a list is renamed/deleted
        public event Action<string>? OnItemsChanged;    // Triggered when items are added/checked
        public event Action<string>? OnNotification;  // Triggered when an invite is accepted
        public event Action<string>? OnListAccessRemoved; // Triggered when user is removed from a list

        public RealTimeSyncService(NavigationManager nav)
        {
            _nav = nav;
        }

        public async Task InitializeAsync(string jwtToken)
        {
            if (_hubConnection is not null) return;

            // 1. Build the connection to the Server Hub
            _hubConnection = new HubConnectionBuilder()
                // Switch from "localhost" to the actual deployed URL when in production. Since in Dev the API is on a different port
                //.WithUrl(_nav.ToAbsoluteUri("/api/hubs/shopping-hub"),
                .WithUrl("https://localhost:7273/api/hubs/shopping-hub",
                options =>
                {
                    // Pass the JWT token so the Hub knows who we are
                    options.AccessTokenProvider = () => Task.FromResult((string?)jwtToken);
                })
                .WithAutomaticReconnect() // Auto-retry if internet drops
                .Build();

            // Matches: await hubContext.Clients.Group(...).SendAsync("ListUpdated", listId);
            _hubConnection.On<string>("ListUpdated", (listId) =>
            {
                OnListUpdated?.Invoke(listId);
                OnItemsChanged?.Invoke(listId); // Usually implies items changed too
            });

            // Matches: await hubContext.Clients.Users(...).SendAsync("InviteAccepted", message);
            _hubConnection.On<string>("InviteAccepted", (message) =>
            {
                OnNotification?.Invoke(message);
            });

            // Matches: await hubContext.Clients.Users(...).SendAsync("ListAccessRemoved", message);
            _hubConnection.On<string>("ListAccessRemoved", (message) =>
            {
                OnListAccessRemoved?.Invoke(message);
            });



            // 3. Start the connection
            await _hubConnection.StartAsync();
        }

        public async Task JoinListGroup(string listId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                Console.WriteLine($"Joined list: {listId}");
                await _hubConnection.SendAsync("JoinList", listId);
            }
            else
            {
                Console.WriteLine($"Failed to join list: {listId}");
            }
        }

        public async Task LeaveListGroup(string listId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                Console.WriteLine($"Left list: {listId}");
                await _hubConnection.SendAsync("LeaveList", listId);
            }
            else
            {
                Console.WriteLine($"Failed to leave list: {listId}");
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