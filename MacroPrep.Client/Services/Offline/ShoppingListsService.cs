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
            await _js.InvokeVoidAsync("MacroPrepDB.init");
        }

        // --- LIST OPERATIONS ---

        public async Task<List<ListDto>> GetAllListsAsync(Guid userId)
        {
            var allLists = await _js.InvokeAsync<List<ListDto>>("MacroPrepDB.getAll", "lists");

            return allLists.Where(l => l.OwnerId == userId).ToList();
        }

        public async Task SaveListAsync(ListDto list)
        {
            await _js.InvokeVoidAsync("MacroPrepDB.save", "lists", list);
        }

        public async Task DeleteListAsync(Guid listId)
        {
            await _js.InvokeVoidAsync("MacroPrepDB.delete", "lists", listId);
        }

        // --- ITEM OPERATIONS ---

        public async Task<List<ListItemDto>> GetItemsAsync(Guid listId)
        {
            return await _js.InvokeAsync<List<ListItemDto>>("MacroPrepDB.getItemsByList", listId);
        }

        public async Task AddItemAsync(ListItemDto item)
        {
            await _js.InvokeVoidAsync("MacroPrepDB.save", "items", item);
        }

        public async Task DeleteItemAsync(Guid itemId)
        {
            await _js.InvokeVoidAsync("MacroPrepDB.delete", "items", itemId);
        }
    }
}