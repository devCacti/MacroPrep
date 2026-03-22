using MacroPrep.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class Tag
    {
        [Key, Required]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public TagType Type { get; set; } = TagType.None;

        public virtual ICollection<Recipe>? Recipes { get; set; }
        public virtual ICollection<Ingredient>? Ingredients { get; set; }
        public virtual ICollection<User>? Users { get; set; }

        public Tag(string name, TagType type)
        {
            Id = Guid.NewGuid();
            Name = name;
            Type = type;
        }
    }
}
