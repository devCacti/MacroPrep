using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class Recipe
    {
        [Key, Required]
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }

        public decimal Servings { get; set; }

        // Time
        public decimal CookingTimeMinutes { get; set; } = 0;
        public decimal PreparationTimeMinutes { get; set; } = 0;
        public decimal RestingTimeMinutes { get; set; } = 0;
        public virtual decimal TotalTimeMinutes { get => CookingTimeMinutes + PreparationTimeMinutes + RestingTimeMinutes; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;



        [Required]
        public virtual User Owner { get; set; }
        public virtual ICollection<Ingredient>? Ingredients { get; set; }
        public virtual ICollection<Procedure>? Procedures { get; set; }
    }
}
