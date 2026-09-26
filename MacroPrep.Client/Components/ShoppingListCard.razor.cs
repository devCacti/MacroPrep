using MacroPrep.Client.Pages.ShoppingLists;
using MacroPrep.Shared.Models.ShoppingLists;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
// Add other necessary using statements here

namespace MacroPrep.Client.Components;

public partial class ShoppingListCard : ComponentBase
{
    // --- Inputs from Parent ---
    [Parameter, EditorRequired]
    public Pantry.ListViewModel ViewModel { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid CurrentUserId { get; set; }

    [Parameter]
    public bool IsGlobalSyncing { get; set; }

    // --- Events bubbles to Parent ---
    [Parameter] public EventCallback<string> OnTitleChanged { get; set; }
    [Parameter] public EventCallback<ListItemDto> OnItemUpdated { get; set; }
    [Parameter] public EventCallback<ListItemDto> OnItemDeleted { get; set; }
    [Parameter] public EventCallback<string> OnItemAdded { get; set; }
    [Parameter] public EventCallback<bool> OnSharedStatusChanged { get; set; }
    [Parameter] public EventCallback<string> OnMemberAdded { get; set; }
    [Parameter] public EventCallback<ListMemberDto> OnMemberRemoved { get; set; }
    [Parameter] public EventCallback<Pantry.ListViewModel> OnListDeleted { get; set; }
    [Parameter] public EventCallback<Pantry.ListViewModel> OnListLeft { get; set; }

    // --- Local Component State ---
    private bool IsSettingsOpen { get; set; } = false;
    private string NewItemName { get; set; } = string.Empty;
    private string NewMemberName { get; set; } = string.Empty;

    // --- Computed Properties ---
    private int TotalItemsCount => ViewModel.Items.Count;
    private int CompletedItemsCount => ViewModel.Items.Count(i => i.IsChecked);
    private int ProgressPercentage => TotalItemsCount == 0 ? 0 : (int)((double)CompletedItemsCount / TotalItemsCount * 100);
    private bool IsListFullySynced => !ViewModel.IsSyncing && !IsGlobalSyncing && ViewModel.List.IsSynced;

    // --- Local Event Handlers ---
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    private async Task HandleTitleChange(ChangeEventArgs e)
    {
        var newTitle = e.Value?.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(newTitle))
        {
            await OnTitleChanged.InvokeAsync(newTitle);
        }
    }

    private async Task HandleItemToggled(ListItemDto item, bool isChecked)
    {
        item.IsChecked = isChecked;
        await OnItemUpdated.InvokeAsync(item);
    }

    private async Task HandleAddItem()
    {
        if (string.IsNullOrWhiteSpace(NewItemName)) return;

        await OnItemAdded.InvokeAsync(NewItemName.Trim());
        NewItemName = string.Empty; // Clear input locally after emitting
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await HandleAddItem();
        }
    }

    private async Task HandleAddMember()
    {
        if (string.IsNullOrWhiteSpace(NewMemberName)) return;

        await OnMemberAdded.InvokeAsync(NewMemberName.Trim());
        NewMemberName = string.Empty; // Clear input locally
    }
}