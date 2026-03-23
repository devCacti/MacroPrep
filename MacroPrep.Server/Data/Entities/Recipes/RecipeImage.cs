using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class RecipeImage
    {
        [Key, Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string ImageURL { get; set; } = string.Empty; // Could be a URL or a Disk path

        [Required]
        public string? Title { get; set; }

        public float? Width { get; set; }
        public float? Height { get; set; }

        public virtual Recipe Recipe { get; set; } = null!;

        public RecipeImage() { }

        public RecipeImage(string imageURL, string? title=null, float ? width = null, float ? height = null)
        {
            Id = Guid.NewGuid();
            ImageURL = imageURL;
            Title = title;
            Width = width;
            Height = height;
            Recipe = new Recipe(new User(), string.Empty);
        }
    }
}
