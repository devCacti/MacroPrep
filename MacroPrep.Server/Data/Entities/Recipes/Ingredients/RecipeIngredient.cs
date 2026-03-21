using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class RecipeIngredient
    {
        [Key, Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        public float Ammount { get; set; } = 1;

        public MeasuringUnit? Unit { get; set; }

        [Required]
        public virtual Ingredient Ingredient { get; set; } = null!;

        [Required]
        public virtual Recipe Recipe { get; set; } = null!;

        public virtual ICollection<Tag>? Tags { get; set; }

        public RecipeIngredient() { }

        public RecipeIngredient(Recipe recipe, Ingredient ingredient, float ammount = 1, MeasuringUnit? unit = null)
        {
            Id = Guid.NewGuid();
            Recipe = recipe;
            Ingredient = ingredient;
            Ammount = ammount;
            Unit = unit ?? ingredient.DefaultUnit;
        }
    }
}
