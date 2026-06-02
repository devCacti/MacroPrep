using System;
using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Models.Recipes
{
    public class RecipeIngredientDto
    {
        // The Server ID
        public Guid? Id { get; set; }

        // The local ID
        [Required]
        public Guid IngredientId { get; set; }

        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, float.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public float Amount { get; set; } = 0;

        public string? MeasuringUnit { get; set; }

        // public List<string>? Tags { get; set; }

        /// <summary>
        /// Checks if the instance is empty or still holds default initialization values.
        /// </summary>
        /// <returns>Whether the current Object has default information or not.</returns>
        public bool IsEmpty()
        {
            return IngredientId == Guid.Empty
                && string.IsNullOrEmpty(Name);
        }
    }
}