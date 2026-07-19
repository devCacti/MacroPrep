using MacroPrep.Shared.Models.Recipes;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace MacroPrep.Client.Pages.Recipes
{
    public partial class CreateRecipe
    {
        private RecipeTab ActiveTab { get; set; } = RecipeTab.Details;
        private RecipeFormModel FormModel { get; set; } = new();
        private RecipeIngredientDto? _draggedIngredient;

        private void SetTab(RecipeTab tab) => ActiveTab = tab;

        private void HandleSubmit()
        {
            // Ready for abstraction context hooks or integration pipeline dispatch execution
        }

        private void HandleDragStart(RecipeIngredientDto ingredient)
        {
            _draggedIngredient = ingredient;
        }

        private void HandleDrop(RecipeIngredientDto targetItem)
        {
            // If the dragged ingredient is null or the same as the target item, do nothing (return)
            if (_draggedIngredient == null || _draggedIngredient == targetItem) return;

            // Correct the list order
            int draggedIndex = FormModel.Ingredients.IndexOf(_draggedIngredient);
            int targetIndex = FormModel.Ingredients.IndexOf(targetItem);

            if (draggedIndex < 0 || targetIndex < 0) return;

            if (FormModel.Ingredients[targetIndex].IsEmpty())
                targetIndex--;

            if (draggedIndex == targetIndex) return;

            // Perform a Position swap (only of the dragged item) 
            FormModel.Ingredients.RemoveAt(draggedIndex); 
            FormModel.Ingredients.Insert(targetIndex, _draggedIngredient);

            // Reorder the ingredients to ensure the Order property is consistent with their position in the list
            for (int i = 0; i < FormModel.Ingredients.Count; i++)
            {
                FormModel.Ingredients[i].Order = i + 1;
            }

            // Reset the dragged ingredient after the drop operation
            _draggedIngredient = null;
        }

        private void AddIngredient()
        {
            // Makes it seem that the system is preventing the user from creating more, but its always removing the empty ones
            // All local so no issues with the server
            FormModel.Ingredients.RemoveAllEmpty();
            FormModel.Ingredients.Add(new RecipeIngredientDto { Order = FormModel.Steps.Count + 1});
        }

        private void RemoveIngredient(RecipeIngredientDto item)
        {
            // Removes the ingredient, then reorders
            FormModel.Ingredients.Remove(item);
            ReorderIngredients();
        }

        private void ReorderIngredients()
        {
            for (int i = 0; i < FormModel.Ingredients.Count; i++)
            {
                FormModel.Ingredients[i].Order = i + 1;
            }
        }

        private void AddStep()
        {
            // Same process as the add ingredient method
            FormModel.Steps.RemoveAllEmpty();
            FormModel.Steps.Add(new RecipeProcedureDto { Order = FormModel.Steps.Count + 1 });
        }

        private void RemoveStep(RecipeProcedureDto item)
        {
            // Removes the step then reorders the steps
            FormModel.Steps.Remove(item);
            ReorderSteps();
        }

        private void ReorderSteps()
        {
            for (int i = 0; i < FormModel.Steps.Count; i++)
            {
                FormModel.Steps[i].Order = i + 1;
            }
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
