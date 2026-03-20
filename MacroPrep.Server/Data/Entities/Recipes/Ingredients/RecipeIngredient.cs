using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class RecipeIngredient
    {
        [Key, Required]
        public Guid Id { get; set; }

        public decimal Ammount { get; set; } = 1;

        private MeasuringUnit? unit;
        public MeasuringUnit? Unit{
            get { return unit ?? Ingredient.DefaultUnit; }
            set { unit = value; }
        }

        [Required]
        public virtual Ingredient Ingredient { get; set; }

        [Required]
        public virtual Recipe Recipe { get; set; }

        public virtual ICollection<Tag>? Tags { get; set; }

        public RecipeIngredient(Recipe recipe, Ingredient ingredient, decimal ammount = 1, MeasuringUnit? unit = null)
        {
            Id = Guid.NewGuid();
            Recipe = recipe;
            Ingredient = ingredient;
            Ammount = ammount;
            Unit = unit;
        }
    }
}
