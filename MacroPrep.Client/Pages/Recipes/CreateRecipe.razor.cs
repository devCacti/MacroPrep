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

        private void AddIngredient() => FormModel.Ingredients.Add(new IngredientFormModel());
        private void RemoveIngredient(IngredientFormModel item) => FormModel.Ingredients.Remove(item);

        private void AddStep() => FormModel.Steps.Add(new StepFormModel { Order = FormModel.Steps.Count + 1 });
        private void RemoveStep(StepFormModel item)
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
            public int Calories { get; set; }
            public int Protein { get; set; }
            public int Carbs { get; set; }
            public int Fat { get; set; }

            // Collections
            public List<IngredientFormModel> Ingredients { get; set; } = [];
            public List<StepFormModel> Steps { get; set; } = [];
        }

        // View-specific models bridging to DTOs
        public class IngredientFormModel
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public string Unit { get; set; } = string.Empty;
        }

        public class StepFormModel
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public int Order { get; set; }
            public string Instruction { get; set; } = string.Empty;
        }
    }
}
