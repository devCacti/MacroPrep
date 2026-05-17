using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace MacroPrep.Server.Data.Entities
{
    public class MeasuringUnit
    {
        [Key, Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        // EFCore 8.0 will save the collection as a Json string in the database, meaning we don't need to create a separate table for aliases.
        public ICollection<string> Aliases { get; set; } = new List<string>();

        public virtual ICollection<Ingredient>? Ingredients { get; set; }
        public virtual ICollection<RecipeIngredient>? RecipeIngredients { get; set; }

        public MeasuringUnit() { }
    }
}
