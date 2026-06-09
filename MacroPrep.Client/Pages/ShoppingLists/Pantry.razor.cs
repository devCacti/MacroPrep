using Blazored.LocalStorage;
using MacroPrep.Client.Services;
using MacroPrep.Client.Services.Offline;
using MacroPrep.Shared.Enums.ShoppingLists;
using MacroPrep.Shared.Models.ShoppingLists;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Claims;

namespace MacroPrep.Client.Pages.ShoppingLists
{
    public partial class Pantry
    {
        // --- Injections ---
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
        [Inject] private ShoppingListsService OfflineService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private RealTimeSyncService SignalR { get; set; } = default!;
        [Inject] private SyncService Sync { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        public class ListViewModel
        {
            public ListDto List { get; set; } = new();
            public List<ListItemDto> Items { get; set; } = [];
            public List<ListMemberDto> Members { get; set; } = [];

            // Helper to check if current user is the owner
            public bool IsOwner(Guid currentUserId) => List.OwnerId == currentUserId;
            public bool IsSyncing { get; set; } = false;

            // Helper to get current user's role
            public MemberType GetUserRole(Guid currentUserId)
            {
                if (IsOwner(currentUserId)) return MemberType.Creator;
                var member = Members.FirstOrDefault(m => m.UserId == currentUserId);
                return member?.Type ?? MemberType.Viewer;
            }
        }

        private readonly ConcurrentDictionary<string, bool> _activeUpdates = new();
        private Guid _currentUserId = Guid.Empty;
        private List<ListInviteDto> _pendingInvites = [];

        private readonly List<ListViewModel> _viewModels = [];
        private List<ListDto> ShoppingLists { get; set; } = [];

        private bool _isOffline = false;
        private bool _hasConnection = true;

        private DotNetObjectReference<Pantry>? _objRef;
        private IJSObjectReference? _jsModule;

        private System.Threading.Timer? _retryTimer;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;


            if (user.Identity?.IsAuthenticated == true)
            {
                var idClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(idClaim, out var parsedId))
                    _currentUserId = parsedId;
            }

            if (_currentUserId == Guid.Empty)
            {
                // Force logout because there is something very wrong with the authentication state if we can't get a valid user ID
                await ForceLogout();
                return;
            }

            var token = await LocalStorage.GetItemAsync<string>("authToken");

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    await SignalR.InitializeAsync(token);

                    // List
                    SignalR.ListUpdated += HandleListUpdate;
                    SignalR.ListDeleted += HandleAccessRemoved;
                    SignalR.ListAccessRemoved += HandleAccessRemoved;

                    // Item
                    SignalR.ItemsChanged += HandleListUpdate;

                    // Invites and Members
                    SignalR.InviteReceived += HandleInviteReceived;
                    SignalR.InviteAccepted += HandleInviteAccepted;
                    SignalR.InviteRejected += HandleInviteRejected_Or_MemberLeft;
                    SignalR.MemberLeft += HandleInviteRejected_Or_MemberLeft;
                }
                catch
                {
                    Console.WriteLine("Please check your internet connection or try again later.");
                }
            }

            Sync.OnSyncStatusChanged += HandleSyncStatusChanged;

            // Initialize the offline service
            await OfflineService.InitializeAsync();

            // Loads all local data, even if it's outdated
            await LoadLocalData();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                bool isOnline = true;

                try
                {
                    // Checks Internet Connection
                    _objRef = DotNetObjectReference.Create(this);

                    _jsModule = await JS.InvokeAsync<IJSObjectReference>("import",
                        $"./js/connectivity.js?v={DateTime.Now.Ticks}");

                    isOnline = await JS.InvokeAsync<bool>("ConnectionManager.register", _objRef);
                }
                catch (JSException ex)
                {
                    Console.WriteLine($"JS Interop failed: {ex.Message}");
                }

                _isOffline = !isOnline;
                _hasConnection = isOnline;

                // If online, sync with server
                if (isOnline)
                    await SyncWithServer();

                await InvokeAsync(StateHasChanged);
            }
        }

        private async void HandleListUpdate(string listId)
        {
            if (!Guid.TryParse(listId, out var listGuid)) return;

            var vm = _viewModels.FirstOrDefault(v => v.List.Id == listGuid);
            if (vm == null) return;

            if (!_activeUpdates.TryAdd(listGuid.ToString(), true)) return;

            try
            {
                vm.IsSyncing = true;
                await InvokeAsync(StateHasChanged);

                await Task.Delay(1500);

                var response = await Http.GetAsync($"api/shopping-lists/{listId}");
                if (response.IsSuccessStatusCode)
                {
                    var freshList = await response.Content.ReadFromJsonAsync<ListDto>();
                    if (freshList != null)
                    {
                        vm.List = await AutoUpdateListInfo(freshList, vm.List);
                        vm.Items = await OfflineService.GetItemsAsync(listGuid);
                    }
                }
            }
            finally
            {
                vm.IsSyncing = false;
                _activeUpdates.TryRemove(listGuid.ToString(), out _);
                await InvokeAsync(StateHasChanged);
            }
        }

        private async void HandleAccessRemoved(string listId)
        {
            var vm = _viewModels.FirstOrDefault(v => v.List.Id.ToString() == listId);
            if (vm != null)
            {
                _viewModels.Remove(vm);
                await OfflineService.DeleteListAndRelatedDataAsync(Guid.Parse(listId));
                await SignalR.LeaveListGroup(listId.ToString());
                await InvokeAsync(StateHasChanged);
            }
        }

        private async void HandleInviteReceived(string listId)
        {
            await FetchLists(getInvites: true, getMyLists: false);
            await InvokeAsync(StateHasChanged);
        }

        private async void HandleInviteAccepted(string listId, string memberUserName)
        {
            Console.WriteLine($" > Member {memberUserName} has accepted the invite to list {listId}.");
            var vm = _viewModels.FirstOrDefault(v => v.List.Id.ToString() == listId);
            if (vm != null)
            {
                var member = vm.Members.FirstOrDefault(m => m.UserName.ToString() == memberUserName);
                if (member != null)
                {
                    member.HasAccepted = true;
                    await OfflineService.AddMemberAsync(member, true);
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private async void HandleInviteRejected_Or_MemberLeft(string listId, string memberUserName)
        {
            var vm = _viewModels.FirstOrDefault(v => v.List.Id.ToString() == listId);
            if (vm != null)
            {
                var member = vm.Members.FirstOrDefault(m => m.UserName.ToString() == memberUserName);
                if (member != null)
                {
                    vm.Members.Remove(member);
                    await OfflineService.DeleteMemberPAsync(member);
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        // Syncing Status Changed Handler
        private async void HandleSyncStatusChanged()
        {
            await InvokeAsync(StateHasChanged);
        }

        // Check list full sync status
        private bool IsListFullySynced(ListViewModel vm)
        {
            return !vm.IsSyncing && !Sync.IsGlobalSyncing && vm.List.IsSynced;
        }

        #region JS Invokable Methods
        [JSInvokable]
        public async Task SetOfflineStatus(bool isOffline)
        {
            _isOffline = isOffline;
            _hasConnection = !isOffline;
            await InvokeAsync(StateHasChanged);

            if (!isOffline)
            {
                // When coming back online, attempt to sync immediately
                await SyncWithServer();
            }
        }

        [JSInvokable]
        public async Task OnAppWakeUp()
        {
            Console.WriteLine("App woke up. Performing connection checks...");

            await SignalR.EnsureConnectionAsync();

            foreach (var vm in _viewModels)
            {
                await SignalR.JoinListGroup(vm.List.Id.ToString());
            }

            await SyncWithServer();
        }
        #endregion JS Invokable Methods

        /// <summary>
        /// Syncs local data with the server. This method is called on initial load if we are online, and also whenever we come back online after being offline.
        /// </summary>
        private async Task SyncWithServer()
        {
            try
            {
                // 5 second timeout
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await Http.GetAsync("api/test-connection", cts.Token);

                if (!response.IsSuccessStatusCode) throw new Exception("Server Unreachable");

                _hasConnection = true;
                _isOffline = false;

                await InvokeAsync(StateHasChanged);


                if (_retryTimer != null)
                {
                    _retryTimer.Dispose();
                    _retryTimer = null;
                    Console.WriteLine("Server is back! Stopping retry timer.");
                }

                // Fetch invites and lists (invites: true, my-lists: true)
                await FetchLists();

                // FetchLists() will update the local database with lists that belong to the user or that the user has been invited to
                // But because the following foreach loop looks through the local database.
                // We still haven't retreived the info of all the lists the user has, we need to fetch the lists in the db
                var localLists = await OfflineService.GetAllListsAsync(_currentUserId);

                // Now local lists has all the online lists and the ones that were already there, however, now we loop through them, the ones
                // marked as synced that weren't found on the server get permanently deleted, 
                _viewModels.Clear();

                foreach (var list in localLists)
                {
                    try
                    {
                        var listResponse = await Http.GetAsync($"api/shopping-lists/{list.Id}");
                        if (listResponse.IsSuccessStatusCode)
                        {
                            var serverList = await listResponse.Content.ReadFromJsonAsync<ListDto>();
                            if (serverList != null)
                            {
                                // If this user is no longer a member, remove the list locally, it exists on the view model but doesn't necesserily mean the user is still a member, causing the list to be removed locally
                                if (!serverList.Members.Any(m => m.UserId == _currentUserId) && serverList.OwnerId != _currentUserId)
                                {
                                    // Completely remove from local db, even items and members
                                    await OfflineService.DeleteListAndRelatedDataAsync(list.Id);
                                    await InvokeAsync(StateHasChanged);
                                    continue;
                                }

                                await AutoUpdateListInfo(serverList, list);

                                // Add list to the view model
                                _viewModels.Add(new ListViewModel
                                {
                                    List = serverList,
                                    Items = await OfflineService.GetItemsAsync(serverList.Id),
                                    Members = await OfflineService.GetMembersAsync(serverList.Id)
                                });

                                await SignalR.LeaveListGroup(list.Id.ToString());
                                await SignalR.JoinListGroup(list.Id.ToString());
                                await InvokeAsync(StateHasChanged);
                                continue;
                            }
                        }

                        // If the previous conditions were not met, it means the list either doesn't exist in the server or is invalid
                        // If any of those are met, we check our list, if it is synced (Which means the local db thinks it matches the server, the list is meant to be deleted everywhere)
                        if (list.IsSynced)
                        {
                            // Deletes all data related to the list, items and members.
                            await OfflineService.DeleteListAndRelatedDataAsync(list.Id);
                            await InvokeAsync(StateHasChanged);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Something went wrong fetching lists. Try again later.");
                        Console.WriteLine(ex);
                    }
                }

                Sync.RequestSync();

            }
            catch (Exception ex)
            {
                if (_retryTimer == null && !_hasConnection)
                {
                    _retryTimer = new System.Threading.Timer(async _ =>
                    {
                        Console.WriteLine("Starting 30s auto-retry timer...");
                        await SyncWithServer();
                    }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
                }

                _isOffline = true;
                _hasConnection = false;
                Console.WriteLine("You are currently offline. Changes will sync when you reconnect.");
                Console.WriteLine(ex);

                await InvokeAsync(StateHasChanged);
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task<ListDto> AutoUpdateListInfo(ListDto? serverList, ListDto localList)
        {
            try
            {
                if (serverList == null) return localList;

                var tolerance = TimeSpan.FromSeconds(1);

                // If the local list doesn't have an UpdatedAt timestamp, we should consider the server list as newer and update it
                if (serverList.UpdatedAt - localList.UpdatedAt > tolerance || localList.OwnerId == default)
                {
                    // If the server list is newer, update the local list info
                    localList = serverList;
                }

                var _serverItems = serverList.Items;
                var _serverMembers = serverList.Members;

                var _localItems = await OfflineService.GetItemsAsync(localList.Id);
                var _localMembers = await OfflineService.GetMembersAsync(localList.Id);

                // Run through the server items
                foreach (var serverItem in _serverItems)
                {
                    // Check if there is at least a single item locally that matches the server item by ID
                    var localItem = _localItems.FirstOrDefault(i => i.Id == serverItem.Id);
                    if (localItem != null)
                    {
                        // If the item exists locally, compare the UpdatedAt timestamps to determine which version is newer
                        if (serverItem.UpdatedAt - localItem.UpdatedAt > tolerance)
                        {
                            // If the server item is newer, update the local item with the server version
                            await OfflineService.AddItemAsync(serverItem!, true);
                        }
                        else if (localItem.UpdatedAt - serverItem.UpdatedAt > tolerance)
                        {
                            // If the local item is newer, update the server item with the local version
                            Sync.RequestSync();
                        }
                        // Nothing happens if the updated at timestamps are within the tolerance window, we assume they are the same version and do not need to update either side
                        // THIS SHOULD BE CHANGED TO ROW VERSIONING IN THE FUTURE TO AVOID ANY POSSIBLE ISSUES WITH CLOCK SYNCHRONIZATION OR SIMULTANEOUS UPDATES
                    }
                    else
                    {
                        // if the local item doesn't exist, add it to the local database
                        await OfflineService.AddItemAsync(serverItem!, true);
                    }
                }

                // Now run through the local items and check if any of them don't exist on the server,
                // if they don't exist and they are marked as synced, it means they were deleted on the server and we should delete them locally as well,
                foreach (var localItem in _localItems)
                {
                    if (Sync.IsItemInFlight(localItem.Id)) continue;

                    // Use explicit string comparison or Ensure Guid types match
                    var existsOnServer = _serverItems.Any(i => i.Id.ToString().Equals(localItem.Id.ToString(), StringComparison.CurrentCultureIgnoreCase));

                    if (!existsOnServer && localItem.IsSynced && !localItem.IsDeleted)
                    {
                        Console.WriteLine($"Deleting {localItem.Name} because ID {localItem.Id} was not found in server list.");
                        await OfflineService.DeleteItemPAsync(localItem.Id);
                    }
                }

                // Run through the server members
                foreach (var serverMember in _serverMembers)
                {
                    // Save all server members to the local database
                    // This function will handle duplicates (even with different Ids)
                    await OfflineService.AddMemberAsync(serverMember, true);
                }

                // Run through the local members and check if any of them don't exist on the server,
                // If they don't exist, delete them locally.
                foreach (var member in _localMembers)
                {
                    var serverMember = _serverMembers.FirstOrDefault(m => m.UserName.Equals(member.UserName, StringComparison.CurrentCultureIgnoreCase));

                    if (serverMember == null && member.IsSynced)
                    {
                        Console.WriteLine("Member doesn't exist on the server.");
                        await OfflineService.DeleteMemberPAsync(member);
                    }

                    if (serverMember == null && !member.IsSynced)
                    {
                        localList.Members.Add(member);
                        Sync.RequestSync();
                    }
                }

                await OfflineService.SaveListAsync(localList, true); // Made to be synced from server and not to it
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Problem Updating List Information: {ex}");
            }
            return localList;
        }

        private async Task ToggleSharedStatus(ListViewModel vm, bool isShared)
        {
            vm.List.IsShared = isShared;
            vm.List.IsSynced = false;
            await OfflineService.SaveListAsync(vm.List);
            Sync.RequestSync();
        }

        #region Member Operations
        private async Task AddMember(ListViewModel vm, string username)
        {
            username = username.ToLower();
            var newMember = new ListMemberDto
            {
                Id = Guid.NewGuid(),
                ListId = vm.List.Id,
                UserName = username,
                IsSynced = false,
                IsDeleted = false,
                HasAccepted = false,

            };

            // Resize array (simplest way for fixed array in DTO)
            var membersList = vm.Members;

            if (!membersList.Any(m => m.UserName == newMember.UserName))
            {
                membersList.Add(newMember);
                vm.Members = membersList;
                await OfflineService.AddMemberAsync(newMember);
            }

            Console.WriteLine("Sending invite to: " + username);
            Sync.RequestSync();
        }

        private async Task RemoveMember(ListViewModel vm, ListMemberDto member)
        {
            vm.Members.Remove(member);
            await OfflineService.RemoveMemberAsync(vm.List.Id, member.UserName.ToLower());
            Sync.RequestSync();
        }
        #endregion Member Operations

        #region List Operations
        private async Task CreateNewList()
        {
            var newList = new ListDto
            {
                Id = Guid.NewGuid(),
                OwnerId = _currentUserId,
                Members = Array.Empty<ListMemberDto>(),
                Name = "New List" + (_viewModels.Count + 1),
                IsSynced = false
            };

            await OfflineService.SaveListAsync(newList);

            _viewModels.Insert(0, new ListViewModel { List = newList });

            // Join SignalR group for this new list
            await SignalR.JoinListGroup(newList.Id.ToString());
            Sync.RequestSync();
        }

        private async Task UpdateListTitle(ListDto list, string newName)
        {
            list.Name = newName;
            list.IsSynced = false;
            await OfflineService.SaveListAsync(list);
            Sync.RequestSync();
        }

        private async Task DeleteListAsync(ListViewModel viewModel)
        {
            _viewModels.Remove(viewModel);
            await OfflineService.DeleteListAsync(viewModel.List.Id);
            Sync.RequestSync();
        }

        private async Task LeaveListAsync(ListViewModel vm)
        {
            // 1. Find my own member record
            var me = vm.Members.FirstOrDefault(m => m.UserId == _currentUserId);
            if (me == null)
            {
                // Fallback: If I can't find my member record, I just delete the list locally
                await DeleteListAsync(vm);
                return;
            }

            // 2. Remove myself from the view model
            vm.Members.Remove(me);
            _viewModels.Remove(vm);

            // 3. Mark myself as deleted in the DB so SyncService tells the server
            await OfflineService.RemoveMemberAsync(vm.List.Id, me.UserName);

            // 4. Also delete the list locally so it disappears
            await OfflineService.DeleteListAsync(vm.List.Id);

            // api/shopping-lists/{listId}/leave
            // without using the sync service, for this we have to call the API directly
            try
            {
                var response = await Http.DeleteAsync($"api/shopping-lists/{vm.List.Id}/leave");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Failed to leave list on server.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while trying to leave list: {ex.Message}");
            }
        }

        #endregion List Operations

        #region Item Operations

        private async Task AddItem(ListViewModel viewModel, string newItemName)
        {
            var newItem = new ListItemDto
            {
                Id = Guid.NewGuid(),
                ListId = viewModel.List.Id,
                Name = newItemName,
                Quantity = 1,
                IsChecked = false,
                IsSynced = false
            };


            viewModel.Items.Add(newItem);
            await OfflineService.AddItemAsync(newItem);
            Sync.RequestSync();
        }

        private async Task UpdateItem(ListItemDto item)
        {
            item.IsSynced = false;
            await OfflineService.AddItemAsync(item); // Using AddItemAsync to update the item since it checks for existing ID
            Sync.RequestSync();
        }

        private async Task DeleteItem(ListViewModel viewModel, ListItemDto item)
        {
            viewModel.Items.Remove(item);
            await OfflineService.DeleteItemAsync(item.Id);
            Sync.RequestSync();
        }
        
        #endregion Item Operations

        #region Invite Operations
        private async Task AcceptInvite(ListInviteDto invite)
        {
            // 1. Call the API Endpoint
            var response = await Http.PostAsync($"api/shopping-lists/{invite.ListId}/invite/accept", null);

            if (response.IsSuccessStatusCode)
            {
                // 2. Remove from the "Pending Invites" UI immediately
                _pendingInvites.Remove(invite);

                // 3. Optional: Trigger a full refresh to pull the new list into the main grid
                // Or manually fetch just this list and add it to _viewModels
                await SignalR.JoinListGroup(invite.ListId.ToString()); // Join SignalR group for real-time updates on this list
                await FetchLists();
                await LoadLocalData();

                await InvokeAsync(StateHasChanged);
            }
            else
            {
                // Handle error (maybe show a toast)
                Console.WriteLine("Failed to accept invite");
            }
        }

        private async Task DeclineInvite(ListInviteDto invite)
        {
            // 1. Call the API Endpoint
            var response = await Http.DeleteAsync($"api/shopping-lists/{invite.ListId}/invite/decline");

            if (response.IsSuccessStatusCode)
            {
                // 2. Remove from UI
                _pendingInvites.Remove(invite);
            }
            else
            {
                Console.WriteLine("Failed to decline invite");
            }
        }
        #endregion Invite Operations

        private async Task LoadLocalData()
        {
            var storedLists = await OfflineService.GetAllListsAsync(_currentUserId);
            _viewModels.Clear();
            foreach (var list in storedLists)
            {
                var items = await OfflineService.GetItemsAsync(list.Id);
                var members = await OfflineService.GetMembersAsync(list.Id);
                _viewModels.Add(new ListViewModel { List = list, Items = items, Members = members });

                await SignalR.LeaveListGroup(list.Id.ToString());
                await SignalR.JoinListGroup(list.Id.ToString());
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task FetchLists(bool getInvites = true, bool getMyLists = true)
        {
            if (getInvites)
                try
                {
                    _pendingInvites = await Http.GetFromJsonAsync<List<ListInviteDto>>("api/shopping-lists/invites")
                                      ?? [];
                }
                catch
                {
                    // User might be offline or no invites
                    _pendingInvites = [];
                }

            //Check end endpoint /api/shopping-lists/my-lists to get the ones that the user has been accepted into
            if (getMyLists)
                try
                {
                    var myLists = await Http.GetFromJsonAsync<List<ListDto>>("api/shopping-lists/my-lists") ?? [];
                    var localLists = await OfflineService.GetAllListsAsync();

                    foreach (var list in myLists)
                    {
                        var localList = localLists.FirstOrDefault(l => l.Id == list.Id);
                        if (localList == null)
                        {
                            await AutoUpdateListInfo(list, new ListDto { Id = list.Id });
                        }
                        else
                        {
                            // We have this list locally, check if we need to update it with server info
                            await AutoUpdateListInfo(list, localList);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch /api/shopping-lists/my-lists with exception: {ex}");
                }
        }

        // Method to accept Enter Key for adding items

        private async Task ForceLogout()
        {
            await LocalStorage.RemoveItemAsync("authToken");
            ((CustomAuthStateProvider)AuthStateProvider).NotifyUserLogout();

            Nav.NavigateTo("/login");
        }

        public void Dispose()
        {
            // 1. Dispose unmanaged or IDisposable resources
            _objRef?.Dispose();
            _retryTimer?.Dispose();

            // 2. Unsubscribe from Sync Service (FIXED MEMORY LEAK)
            if (Sync != null)
            {
                Sync.OnSyncStatusChanged -= HandleSyncStatusChanged;
            }

            // 3. Unsubscribe from SignalR
            if (SignalR != null)
            {
                SignalR.ListUpdated -= HandleListUpdate;
                SignalR.ListDeleted -= HandleAccessRemoved;
                SignalR.ListAccessRemoved -= HandleAccessRemoved;
                SignalR.ItemsChanged -= HandleListUpdate;
                SignalR.InviteReceived -= HandleInviteReceived;
                SignalR.InviteAccepted -= HandleInviteAccepted;
                SignalR.InviteRejected -= HandleInviteRejected_Or_MemberLeft;
                SignalR.MemberLeft -= HandleInviteRejected_Or_MemberLeft;
            }
        }
    }
}
