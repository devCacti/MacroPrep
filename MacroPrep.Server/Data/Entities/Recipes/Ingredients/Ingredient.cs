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

        public virtual ICollection<IngredientCategory>? Category { get; set; }
        public virtual ICollection<Tag>? Tags { get; set; }

        public Ingredient(string name, MeasuringUnit unit)
        {
            Name = name;
            DefaultUnit = unit;
        }
    }
}
