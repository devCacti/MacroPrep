using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace MacroPrep.Client.Services
{
    public class RealTimeSyncService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly NavigationManager _nav;

        // Events that the UI will subscribe to

        /// <summary>                                       ///
        /// START OF    -> PANTRY.RAZOR PAGE RELATED EVENTS ///
        /// </summary>                                      ///
        // List Specific
        public event Action<string>? ListUpdated;           // ListId
        public event Action<string>? ListDeleted;           // ListId
        public event Action<string>? ListAccessRemoved;     // ListId

        // Items (To be implemented)
        public event Action<string>? ItemsChanged;          // ...

        // Invites
        public event Action<string>? InviteReceived;        // ListId
        public event Action<string, string>? InviteAccepted;// ListId, Member UserName
        public event Action<string, string>? InviteRejected;// ListId, Member UserName
        public event Action<string, string>? MemberLeft;    // ListId, Member UserName

        /// <summary>                                       ///
        /// END OF      -> PANTRY.RAZOR PAGE RELATED EVENTS ///
        /// </summary>                                      ///
        
        public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;



        public RealTimeSyncService(NavigationManager nav)
        {
            _nav = nav;
        }

        public async Task InitializeAsync(string jwtToken)
        {
            if (_hubConnection is not null)
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    Console.WriteLine("Hub connection exists but is not connected. Attempting to reconnect...");
                    await _hubConnection.StartAsync();
                }
                return;
            }

            // 1. Build the connection to the Server Hub
            _hubConnection = new HubConnectionBuilder()
                // Switch from "localhost" to the actual deployed URL when in production. Since in Dev the API is on a different port
                .WithUrl(_nav.ToAbsoluteUri("/api/hubs/shopping-hub"), options => options.AccessTokenProvider = () => Task.FromResult((string?)jwtToken))
                //.WithUrl("https://localhost:7273/api/hubs/shopping-hub", options => options.AccessTokenProvider = () => Task.FromResult((string?)jwtToken))
                .WithAutomaticReconnect() // Auto-retry if internet drops
                .Build();

            /// <summary>                                       ///
            /// START OF    -> PANTRY.RAZOR PAGE RELATED EVENTS ///
            /// </summary>                                      ///
            // List Specific
            _hubConnection.On<string>("ListUpdated", (listId) => ListUpdated?.Invoke(listId));
            _hubConnection.On<string>("ListDeleted", (listId) => ListDeleted?.Invoke(listId));
            _hubConnection.On<string>("ListAccessRemoved", (listId) => ListAccessRemoved?.Invoke(listId));

            // Items (To be implemented)
            _hubConnection.On<string>("ItemsChanged", (itemId) => ItemsChanged?.Invoke(itemId));

            // Invites & Members
            _hubConnection.On<string>("InviteReceived", (listId) => InviteReceived?.Invoke(listId));
            _hubConnection.On<string, string>("InviteAccepted", (listId, memberId) => InviteAccepted?.Invoke(listId, memberId));
            _hubConnection.On<string, string>("InviteRejected", (listId, memberId) => InviteRejected?.Invoke(listId, memberId));
            _hubConnection.On<string, string>("MemberLeftList", (listId, memberId) => MemberLeft?.Invoke(listId, memberId));
            /// <summary>                                       ///
            /// END OF      -> PANTRY.RAZOR PAGE RELATED EVENTS ///
            /// </summary>                                      ///

            // 3. Start the connection
            await _hubConnection.StartAsync();
        }

        public async Task EnsureConnectionAsync()
        {
            // The connection doesn't exist
            if (_hubConnection is null) return;

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                Console.WriteLine("Connection was dead. Restarting...");

                try
                {
                    await _hubConnection.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to restart connection: {ex.Message}");
                }
            }
        }

        public async Task JoinListGroup(string listId)
        {
            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
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
            if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Connected)
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