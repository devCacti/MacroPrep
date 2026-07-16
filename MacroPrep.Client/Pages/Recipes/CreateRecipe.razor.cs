using MacroPrep.Shared.Models.Recipes;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Client.Pages.Recipes
{
    public partial class CreateRecipe
    {
        private RecipeTab ActiveTab { get; set; } = RecipeTab.Details;
        private RecipeFormModel FormModel { get; set; } = new();

        private void SetTab(RecipeTab tab) => ActiveTab = tab;

        private void HandleSubmit()
        {
            // Ready for abstraction context hooks or integration pipeline dispatch execution
        }

        private void AddIngredient() => FormModel.Ingredients.Add(new RecipeIngredientDto());
        private void RemoveIngredient(RecipeIngredientDto item) => FormModel.Ingredients.Remove(item);

        private void AddStep() => FormModel.Steps.Add(new RecipeProcedureDto { Order = FormModel.Steps.Count + 1 });
        private void RemoveStep(RecipeProcedureDto item)
        {
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
            public string Category { get; set; } = "Dinner";
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
