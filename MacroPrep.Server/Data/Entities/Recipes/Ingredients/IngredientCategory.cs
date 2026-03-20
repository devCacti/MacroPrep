using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities;

public class IngredientCategory
{
    [Key, Required]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; }

    public virtual List<Ingredient>? Ingredients { get; set; }

    public IngredientCategory(string name) {
        Id = Guid.NewGuid();
        Name = name;
    }
}
