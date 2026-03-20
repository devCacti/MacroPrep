using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class Ingredient
    {
        [Key, Required]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public MeasuringUnit DefaultUnit { get; set; }

        public virtual ICollection<IngredientCategory>? Categories { get; set; }
        public virtual ICollection<Tag>? Tags { get; set; }
        public virtual ICollection<RecipeIngredient>? RecipeIngredients { get; set; }

        public Ingredient(string name, MeasuringUnit unit)
        {
            Name = name;
            DefaultUnit = unit;
        }
    }
}
