using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class Tag
    {
        [Key, Required]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Type { get; set; }

        public Tag(string name, string? type = null)
        {
            Id = Guid.NewGuid();
            Name = name;
            Type = type;
        }
    }
}
