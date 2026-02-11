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
                return activeLists.Where(
                    l => l.OwnerId == userId || 
                    (l.Members != null && l.Members.Any(m => m.UserId == userId))
                ).ToList();
            }

            return activeLists;
        }

        // Called by UI when user creates/updates a list
        public async Task SaveListAsync(ListDto list, bool synced = false)
        {
            list.IsSynced = synced;
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

        public async Task AddItemAsync(ListItemDto item, bool synced = false)
        {
            item.IsSynced = synced;
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

        public async Task AddMemberAsync(ListMemberDto member, bool synced = false)
        {
            member.IsSynced = synced;
            member.IsDeleted = false;
            member.UserName = member.UserName.ToLower();

            // Fetch all members for this list to check for duplicates (a duplicate is one with the same username)
            try
            {
                var existingMembers = await _js.InvokeAsync<List<ListMemberDto>>("MacroPrep_DB.getMembersByList", member.ListId);

                // check for existing member with same username (case-insensitive)
                if (existingMembers != null)
                {
                    ListMemberDto? existingMember = existingMembers.FirstOrDefault(m => m.UserName.ToLower() == member.UserName);

                    // delete the existing member
                    if (existingMember != null)
                        await _js.InvokeVoidAsync("MacroPrep_DB.delete", "members", existingMember.Id);
                }
            } catch { // The only exception should be "No Match"
            }


            // Verify there isn't already a member with the same email for this list
            await _js.InvokeVoidAsync("MacroPrep_DB.save", "members", member);
        }

        public async Task RemoveMemberAsync(Guid listId, string userName)
        {
            var member = await _js.InvokeAsync<ListMemberDto>("MacroPrep_DB.getMemberByListAndUserName", listId, userName);

            if (member == null)
            {
                throw new Exception("Did not find user in local DB");
            }

            member.IsDeleted = true;
            member.IsSynced = false;
            await _js.InvokeVoidAsync("MacroPrep_DB.save", "members", member);
        }

        public async Task<List<ListMemberDto>> GetMembersAsync(Guid listId)
        {
            var members = await _js.InvokeAsync<List<ListMemberDto>>("MacroPrep_DB.getMembersByList", listId);
            return members.Where(m => !m.IsDeleted).ToList();
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
            var items = await GetItemsAsync(listId);
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
        public async Task DeleteMemberPAsync(ListMemberDto member)
        {
            var dMember = await _js.InvokeAsync<ListMemberDto>("MacroPrep_DB.getMemberByListAndUserName", member.ListId, member.UserName);
            if (member == null) return;

            await _js.InvokeVoidAsync("MacroPrep_DB.delete", "members", member.Id);
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
            try
            {
                // 1. Fetch as a Nullable JsonElement
                var result = await _js.InvokeAsync<System.Text.Json.JsonElement?>($"MacroPrep_DB.get", storeName, id);

                // 2. Check for null or 'undefined' safely
                if (!result.HasValue ||
                     result.Value.ValueKind == System.Text.Json.JsonValueKind.Null ||
                     result.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                {
                    return false; // Not found in this table
                }

                // 3. Found it! Mark as synced.
                await _js.InvokeVoidAsync("MacroPrep_DB.markSynced", storeName, id);
                return true;
            }
            catch
            {
                // If JS throws an error (e.g., store not found), treat as "not found"
                return false;
            }
        }
    }
}