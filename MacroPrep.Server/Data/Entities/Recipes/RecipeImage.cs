using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class RecipeImage
    {
        [Key, Required]
        public Guid Id { get; set; }

        [Required]
        public string ImageURL { get; set; } // Could be a URL or a Disk path

        [Required]
        public string? Title { get; set; }

        public decimal? Width { get; set; }
        public decimal? Height { get; set; }

        public RecipeImage(string imageURL, string? title=null, decimal ? width = null, decimal ? height = null)
        {
            Id = Guid.NewGuid();
            ImageURL = imageURL;
            Title = title;
            Width = width;
            Height = height;
        }
    }
}
