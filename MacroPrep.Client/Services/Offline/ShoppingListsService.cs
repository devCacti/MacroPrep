using Microsoft.JSInterop;
using MacroPrep.Shared.Models.ShoppingLists;

namespace MacroPrep.Client.Services.Offline
{
    public class ShoppingListsService
    {
        private readonly IJSRuntime _js;

        public ShoppingListsService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task InitializeAsync()
        {
            await _js.InvokeVoidAsync("MacroPrep_DB.init");
        }

        // ==========================================
        // 📋 LIST OPERATIONS
        // ==========================================

        public async Task<List<ListDto>> GetAllListsAsync(Guid userId = default)
        {
            // Fetch everything
            var allLists = await _js.InvokeAsync<List<ListDto>>("MacroPrep_DB.getAll", "lists");

            // Filter out "Soft Deleted" items so the user doesn't see them
            var activeLists = allLists.Where(l => !l.IsDeleted).ToList();

            if (userId != Guid.Empty)
            {
                return activeLists.Where(l => l.OwnerId == userId).ToList();
            }

            return activeLists;
        }

        // Called by UI when user creates/updates a list
        public async Task SaveListAsync(ListDto list)
        {
            list.IsSynced = false; // 👈 Mark as "Dirty" (Needs Sync)
            list.IsDeleted = false;
            await _js.InvokeVoidAsync("MacroPrep_DB.save", "lists", list);
        }

        // Called by UI when user updates a list
        public async Task UpdateListAsync(ListDto list)
        {
            await SaveListAsync(list); // Same logic, IndexedDB "put" handles update
        }

        // Called by UI to delete
        public async Task DeleteListAsync(Guid listId)
        {
            // 1. Get the item first
            var list = await _js.InvokeAsync<ListDto>("MacroPrep_DB.get", "lists", listId);
            if (list == null) return;

            // 2. SOFT DELETE: Mark as deleted, unsynced
            list.IsDeleted = true;
            list.IsSynced = false;

            // 3. Save back to DB so SyncService finds it later
            await _js.InvokeVoidAsync("MacroPrep_DB.save", "lists", list);
        }

        // ==========================================
        // 🍎 ITEM OPERATIONS
        // ==========================================

        public async Task<List<ListItemDto>> GetItemsAsync(Guid listId)
        {
            var items = await _js.InvokeAsync<List<ListItemDto>>("MacroPrep_DB.getItemsByList", listId);
            return items.Where(i => !i.IsDeleted).ToList();
        }

        public async Task<List<ListItemDto>> GetItemsForListAsync(Guid listId) => await GetItemsAsync(listId);

        public async Task AddItemAsync(ListItemDto item)
        {
            item.IsSynced = false; // Mark dirty
            item.IsDeleted = false;
            await _js.InvokeVoidAsync("MacroPrep_DB.save", "items", item);
        }

        public async Task UpdateItemAsync(ListItemDto item)
        {
            await AddItemAsync(item);
        }

        public async Task DeleteItemAsync(Guid itemId)
        {
            var item = await _js.InvokeAsync<ListItemDto>("MacroPrep_DB.get", "items", itemId);
            if (item == null) return;

            item.IsDeleted = true;
            item.IsSynced = false;

            await _js.InvokeVoidAsync("MacroPrep_DB.save", "items", item);
        }

        public async Task AddMemberAsync(ListMemberDto member)
        {
            member.IsSynced = false;
            member.IsDeleted = false;
            await _js.InvokeVoidAsync("MacroPrep_DB.save", "members", member);
        }

        public async Task RemoveMemberAsync(ListMemberDto member)
        {
            member.IsDeleted = true;
            member.IsSynced = false;
            await _js.InvokeVoidAsync("MacroPrep_DB.save", "members", member);
        }

        // ==========================================
        // 🔄 SYNC SERVICE HELPERS
        // ==========================================

        // Used by SyncService to find work to do
        public async Task<List<ListDto>> GetPendingSyncListsAsync()
        {
            var all = await _js.InvokeAsync<List<ListDto>>("MacroPrep_DB.getAll", "lists");
            return all.Where(l => !l.IsSynced).ToList();
        }

        public async Task<List<ListItemDto>> GetPendingSyncItemsAsync()
        {
            var all = await _js.InvokeAsync<List<ListItemDto>>("MacroPrep_DB.getAll", "items");
            return all.Where(i => !i.IsSynced).ToList();
        }

        public async Task<List<ListMemberDto>> GetPendingSyncMembersAsync()
        {
            var all = await _js.InvokeAsync<List<ListMemberDto>>("MacroPrep_DB.getAll", "members");
            return all.Where(m => !m.IsSynced).ToList();
        }

        // Delete  list and all its related items/members from IndexedDB immediately after a successful delete sync with the server
        public async Task DeleteListAndRelatedDataAsync(Guid listId)
        {
            // Gets all items and members for this list and deletes them, then deletes the list itself
            var items = await GetItemsForListAsync(listId);
            foreach (var item in items)
            {
                await _js.InvokeVoidAsync("MacroPrep_DB.delete", "items", item.Id);
            }
            var members = await _js.InvokeAsync<List<ListMemberDto>>("MacroPrep_DB.getMembersByList", listId);
            foreach (var member in members)
            {
                await _js.InvokeVoidAsync("MacroPrep_DB.delete", "members", member.Id);
            }

            // Finally, delete the list itself
            await _js.InvokeVoidAsync("MacroPrep_DB.delete", "lists", listId);
        }

        // Delete a member from IndexedDB immediately after a successful delete sync with the server
        public async Task DeleteMemberPAsync(Guid memberId)
        {
            await _js.InvokeVoidAsync("MacroPrep_DB.delete", "members", memberId);
        }

        // Delete an item from IndexedDB immediately after a successful delete sync with the server
        public async Task DeleteItemPAsync(Guid itemId)
        {
            await _js.InvokeVoidAsync("MacroPrep_DB.delete", "items", itemId);
        }

        // Called by SyncService when API says "OK 200"
        public async Task MarkAsSyncedAsync(Guid id)
        {
            // We have to check both tables because SyncService passed a generic ID
            // Ideally, SyncService should tell us WHICH table to update.
            // But for now, we try 'lists' first, then 'items'.

            var found = await MarkTableAsSynced("lists", id);
            if (!found) found = await MarkTableAsSynced("items", id);
            if (!found) found = await MarkTableAsSynced("members", id);

        }

        private async Task<bool> MarkTableAsSynced(string storeName, Guid id)
        {
            var obj = await _js.InvokeAsync<dynamic>($"MacroPrep_DB.get", storeName, id);
            if (obj == null) return false;

            // In dynamic JS interop, we can't easily cast to DTO and back if types vary,
            // but since we know the structure:

            // If it was marked as deleted and we successfully synced that delete, remove it for real now
            // (Or keep it for history, your choice. Usually we Hard Delete now).
            // checking 'IsDeleted' via JsonElement or dynamic can be tricky.
            // Let's assume we just mark IsSynced = true.

            // Note: If you implement Soft Deletes, you need logic here:
            // IF obj.IsDeleted == true -> Hard Delete from DB
            // ELSE -> obj.IsSynced = true -> Save

            // Simplified approach:
            await _js.InvokeVoidAsync("MacroPrep_DB.markSynced", storeName, id);
            return true;
        }
    }
}