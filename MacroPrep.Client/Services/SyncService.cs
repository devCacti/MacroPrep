using Blazored.LocalStorage;
using MacroPrep.Client.Services.Offline;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace MacroPrep.Client.Services
{
    public class SyncService
    {
        private readonly ShoppingListsService _offlineService; // Your IndexedDB service
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _http;
        private CancellationTokenSource? _debounceCts;

        private bool _isSyncing = false;
        private bool _isWaitingToSync = false;
        public bool IsGlobalSyncing => _isSyncing || _isWaitingToSync;
        private readonly HashSet<Guid> _inFlightItems = new();
        private readonly ConcurrentDictionary<Guid, DateTime> _recentSyncs = new();

        public event Action? OnSyncStatusChanged;
        private void NotifyStateChanged() => OnSyncStatusChanged?.Invoke();

        public SyncService(ShoppingListsService offlineService, HttpClient http, ILocalStorageService localStorage)
        {
            _offlineService = offlineService;
            _http = http;
            _localStorage = localStorage;
        }

        public void RequestSync()
        {
            // Cancel the previous pending sync request
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();

            _isWaitingToSync = true;
            NotifyStateChanged();

            var token = _debounceCts.Token;

            // Start a task that waits for the "Quiet Period"
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait for 2 seconds. If another change happens, this task is cancelled.
                    await Task.Delay(2000, token);

                    if (!token.IsCancellationRequested)
                    {
                        await SyncPendingChangesAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when a newer change arrives
                }
                finally
                {
                    if (token == _debounceCts?.Token && !token.IsCancellationRequested)
                    {
                        _isWaitingToSync = false;
                        NotifyStateChanged();
                    }
                }
            }, token);
        }

        public async Task SyncPendingChangesAsync()
        {
            if (_isSyncing) return;
            _isSyncing = true;

            NotifyStateChanged();

            try
            {
                var pendingLists = await _offlineService.GetPendingSyncListsAsync();
                var pendingItems = await _offlineService.GetPendingSyncItemsAsync();
                var pendingMembers = await _offlineService.GetPendingSyncMembersAsync();

                Console.WriteLine($"Syncing {pendingLists.Count} lists, {pendingItems.Count} items, {pendingMembers.Count} members...");

                foreach (var item in pendingItems)
                {
                    _inFlightItems.Add(item.Id);
                    NotifyStateChanged();

                    try
                    {
                        HttpResponseMessage response;

                        if (item.IsDeleted)
                        {
                            response = await _http.DeleteAsync($"api/shopping-lists/{item.ListId}/items/{item.Id}");
                        }
                        else
                        {
                            // 🔄 UPSERT (Update or Insert)
                            // Since you use GUIDs created on the client, you can use PUT for everything 
                            // if your API supports "Upsert" (Create if not exists).
                            // Otherwise, you need a way to know if it's New or Existing.
                            // A simple trick: Try PUT first. If 404, try POST.

                            // Option A: Be explicit (Requires tracking 'IsNew' locally)
                            // Option B: Just use PUT (and ensure your API creates if ID not found)

                            // Let's stick to your POST for now, but ensure your API handles "ID already exists" gracefully.
                            response = await _http.PutAsJsonAsync($"api/shopping-lists/{item.ListId}/items/{item.Id}", item);

                            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                // If PUT failed because it didn't exist, try creating it
                                response = await _http.PostAsJsonAsync($"api/shopping-lists/{item.ListId}/items", item);
                            }
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            if (item.IsDeleted)
                                await _offlineService.DeleteItemPAsync(item.Id);

                            await _offlineService.MarkAsSyncedAsync(item.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        _inFlightItems.Remove(item.Id);
                        _recentSyncs[item.Id] = DateTime.UtcNow; // Add the cooldown
                        NotifyStateChanged();
                    }
                }

                foreach (var member in pendingMembers)
                {
                    try
                    {
                        // api/shopping-lists/{listid}/invite?userName=username
                        if (member.IsDeleted)
                        {
                            Console.WriteLine("Sending Delete Member...");
                            var response = await _http.DeleteAsync($"api/shopping-lists/{member.ListId}/member/{member.UserName}");

                            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                                await _offlineService.DeleteMemberPAsync(member);
                            continue;
                        }
                        else
                        {
                            Console.Write("Sending Invite Member...");
                            var url = $"api/shopping-lists/{member.ListId}/invite?userName={Uri.EscapeDataString(member.UserName)}";
                            var response = await _http.PostAsync(url, null);

                            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
                            {
                                await _offlineService.MarkAsSyncedAsync(member.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                }

                foreach (var list in pendingLists)
                {
                    try
                    {
                        HttpResponseMessage response;

                        if (list.IsDeleted)
                        {
                            response = await _http.DeleteAsync($"api/shopping-lists/{list.Id}");
                        }
                        else
                        {
                            list.UpdatedAt = DateTimeOffset.Now;
                            response = await _http.PutAsJsonAsync($"api/shopping-lists/{list.Id}", list);

                            if (response.StatusCode == HttpStatusCode.NotFound)
                            {
                                // If PUT failed because it didn't exist, try creating it
                                response = await _http.PostAsJsonAsync("api/shopping-lists", list);
                            }
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            await _offlineService.MarkAsSyncedAsync(list.Id);
                        }

                        if ((response.StatusCode == HttpStatusCode.NotFound || response.IsSuccessStatusCode) && list.IsDeleted)
                        {
                            await _offlineService.DeleteListAndRelatedDataAsync(list.Id);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                }
            }
            finally
            {
                _isSyncing = false;
                NotifyStateChanged();
            }

        }

        public bool IsItemInFlight(Guid itemId)
        {
            if (_inFlightItems.Contains(itemId)) return true;

            // Protection: If the item finished syncing less than 5 seconds ago, 
            // treat it as still "in flight" to prevent the reconciliation race.
            if (_recentSyncs.TryGetValue(itemId, out var syncTime))
            {
                if (DateTime.UtcNow - syncTime < TimeSpan.FromSeconds(5)) return true;
                else _recentSyncs.TryRemove(itemId, out _); // Clean up old entries
            }
            return false;
        }
    }
}
