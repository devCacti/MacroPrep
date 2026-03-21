using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class Instrument
    {
        [Key, Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? IconSVG { get; set; }
        public string? ImageURL { get; set; }

        public virtual ICollection<Procedure>? Procedures { get; set; }
        public virtual ICollection<Recipe>? Recipes { get; set; }

        public Instrument() { }

        public Instrument(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }
    }
}
