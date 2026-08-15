using MacroPrep.Shared.Interfaces;
using MacroPrep.Shared.Models.Recipes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace MacroPrep.Client.Pages.Recipes
{
    public partial class CreateRecipe
    {
        private RecipeTab _activeTab { get; set; } = RecipeTab.Details;
        private RecipeFormModel FormModel { get; set; } = new();

        private ElementReference _ingredientsContainer;
        private bool _initializeIngredients = false;

        private ElementReference _stepsContainer;
        private bool _initializeSteps = false;

        private DotNetObjectReference<CreateRecipe>? _dotNetRef;

        // Method to change the active tab
        private void SetTab(RecipeTab tab)
        {
            _activeTab = tab;

            StateHasChanged();

            if (tab == RecipeTab.Ingredients)
                _initializeIngredients = true;

            else if (tab == RecipeTab.Steps)
                _initializeSteps = true;
        }

        // Will submit the entirety of the recipe to the server
        // Unlike previous projects, everything goes at once, not in parts
        private void HandleSubmit()
        {
            // Ready for abstraction context hooks or integration pipeline dispatch execution
        }

        private void AddItem<T>(List<T> collection) where T : IFormItem, new()
        {
            collection.RemoveAllEmpty();
            collection.Add(new T { Order = collection.Count + 1 });
        }

        // Removes an item from a list
        private void RemoveItem<T>(List<T> collection, T item) where T : IFormItem
        {
            collection.Remove(item);
            ReorderCollection(collection);
        }

        // Resets the order numbers based on the index of the items
        private void ReorderCollection<T>(List<T> collection) where T : IFormItem
        {
            for (int i = 0; i < collection.Count; i++)
            {
                collection[i].Order = i + 1;
            }
        }

        private void MoveItem<T>(List<T> collection, int oldIndex, int newIndex) where T : IFormItem
        {
            // If it's dropped at the same index do nothing
            if (oldIndex == newIndex)
                return;

            var item = collection[oldIndex];

            // Remove the item from the old index and insert it at the new index
            collection.RemoveAt(oldIndex);
            collection.Insert(newIndex, item);

            // Cause list reordering and update the UI
            ReorderCollection(collection);
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                _dotNetRef = DotNetObjectReference.Create(this);

            if (_initializeIngredients)
            {
                _initializeIngredients = false;

                await JS.InvokeVoidAsync(
                    "sortableInterop.initialize",
                    _ingredientsContainer,
                    _dotNetRef,
                    nameof(IngredientsReordered));
            }

            if (_initializeSteps)
            {
                _initializeSteps = false;

                await JS.InvokeVoidAsync(
                    "sortableInterop.initialize",
                    _stepsContainer,
                    _dotNetRef,
                    nameof(StepsReordered));
            }
        }

        // These functions can't be generalized because they are called from JS
        [JSInvokable]
        public async Task IngredientsReordered(int oldIndex, int newIndex)
        {
            MoveItem(FormModel.Ingredients, oldIndex, newIndex);

            await Task.Yield();

            await InvokeAsync(StateHasChanged);
        }

        [JSInvokable]
        public async Task StepsReordered(int oldIndex, int newIndex)
        {
            MoveItem(FormModel.Steps, oldIndex, newIndex);

            await Task.Yield();

            await InvokeAsync(StateHasChanged);
        }

        public enum RecipeTab { Details, Ingredients, Steps, Macros }

        public class RecipeFormModel
        {
            public string PhotoUrl { get; set; } = string.Empty;

            [Required(ErrorMessage = "Recipe name is required")]
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = "Other";
            public int CookTimeMinutes { get; set; } = 30;
            public int Servings { get; set; } = 4;

            // Macros
            public RecipeMacrosDto Macros { get; set; } = new ();

            // Collections
            public List<RecipeIngredientDto> Ingredients { get; set; } = [];
            public List<RecipeProcedureDto> Steps { get; set; } = [];
        }
    }
}
