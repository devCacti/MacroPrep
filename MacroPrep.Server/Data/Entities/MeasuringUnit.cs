using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class MeasuringUnit
    {
        [Key, Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<Ingredient>? Ingredients { get; set; }
        public virtual ICollection<RecipeIngredient>? RecipeIngredients { get; set; }

        public MeasuringUnit() { }
    }
}
