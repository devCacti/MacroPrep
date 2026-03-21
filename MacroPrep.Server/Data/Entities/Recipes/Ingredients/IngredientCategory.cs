using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities;

public class IngredientCategory
{
    [Key, Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = string.Empty;

    public virtual List<Ingredient>? Ingredients { get; set; }

    public IngredientCategory() { }

    public IngredientCategory(string name) {
        Id = Guid.NewGuid();
        Name = name;
    }
}
