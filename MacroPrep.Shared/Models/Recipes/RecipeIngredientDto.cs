using System;
using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Models.Recipes
{
    public class RecipeIngredientDto
    {
        // The ID of the RecipeIngredient link itself (useful if the UI needs to delete or update this specific row)
        public Guid? Id { get; set; }

        // The ID of the base ingredient (required so the backend knows which ingredient to link to on save)
        [Required]
        public Guid IngredientId { get; set; }

        // Flattened property: Just the name of the ingredient for the UI to display
        public string IngredientName { get; set; } = string.Empty;

        // Note: I fixed the "Ammount" typo from your entity to "Amount" for cleaner frontend usage.
        [Required]
        [Range(0.01, float.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public float Amount { get; set; } = 1;

        // Assuming MeasuringUnit is an Enum or a class shared in your MacroPrep.Shared project
        public string? MeasuringUnit { get; set; }

        // Optional: Flattened tags if your frontend UI needs to display them next to the ingredient
        // public List<string>? Tags { get; set; } 
    }
}