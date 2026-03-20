using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities.Recipes
{
    public class Instrument
    {
        [Key, Required]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? IconSVG { get; set; }
        public string? ImageURL { get; set; }

        public virtual ICollection<Procedure>? Procedures { get; set; }
        public virtual ICollection<Recipe>? Recipes { get; set; }

        public Instrument(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

    }
}
