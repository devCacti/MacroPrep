using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class Ingredient
    {
        [Key, Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        public MeasuringUnit DefaultUnit { get; set; } = null!;

        public virtual ICollection<IngredientCategory>? Categories { get; set; }
        public virtual ICollection<RecipeIngredient>? RecipeIngredients { get; set; }
        public virtual ICollection<Tag>? Tags { get; set; }

        public Ingredient() { }

        public Ingredient(string name, MeasuringUnit defaultUnit)
        {
            Id = Guid.NewGuid();
            Name = name;
            DefaultUnit = defaultUnit;
        }
    }
}
