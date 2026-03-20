using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class MeasuringUnit
    {
        [Key, Required]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public virtual ICollection<Ingredient>? Ingredients { get; set; }

        public MeasuringUnit(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }
    }
}
