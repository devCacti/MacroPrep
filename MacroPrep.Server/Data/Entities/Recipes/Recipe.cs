using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class Recipe
    {
        [Key, Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Images
        public virtual RecipeImage? CoverImage {
            get => GalleryImages?.FirstOrDefault();
        }
        public virtual ICollection<RecipeImage>? GalleryImages { get; set; }

        public float Servings { get; set; } = 1;

        // Time
        public float CookingTimeMinutes { get; set; } = 0;
        public float PreparationTimeMinutes { get; set; } = 0;
        public float RestingTimeMinutes { get; set; } = 0;
        public virtual float TotalTimeMinutes { get => CookingTimeMinutes + PreparationTimeMinutes + RestingTimeMinutes; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;



        [Required]
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<RecipeIngredient>? Ingredients { get; set; }
        public virtual ICollection<Procedure>? Procedures { get; set; }
        public virtual ICollection<Tag>? Tags { get; set; }


        public Recipe() { }

        public Recipe(User owner, string title)
        {
            Id = Guid.NewGuid();
            Owner = owner;
            Title = title;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Recipe(User owner, string title, string? description, float servings, float cookingTimeMinutes, float preparationTimeMinutes, float restingTimeMinutes)
        {
            Id = Guid.NewGuid();
            Owner = owner;
            Title = title;
            Description = description;
            Servings = servings;
            CookingTimeMinutes = cookingTimeMinutes;
            PreparationTimeMinutes = preparationTimeMinutes;
            RestingTimeMinutes = restingTimeMinutes;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
