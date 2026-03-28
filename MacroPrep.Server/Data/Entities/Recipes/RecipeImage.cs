using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using MacroPrep.Shared.Enums.Recipes;

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


        // Images within a specific section of the recipe will appear under the corresponding index in the ui.
        // If the SectionIndex variable is larger than the number of items on the section, the image will be placed at the end of the section.
        // If the Section variable is set to Cover, the image will be used as the cover image for the recipe.
        // If there is already a cover image, the new image will be discarded.
        public RecipeSection Section { get; set; }
        public int SectionIndex { get; set; } // For ordering images within a section


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


        // Images should be shown in "/api/recipes/{recipeId}/images/{image.Id}"
        public static async Task<IResult> AddImageToRecipe(Guid recipeId, RecipeImage image, AppDbContext db)
        {
            var recipe = await db.Recipes.FindAsync(recipeId);
            
            if (recipe == null)
                return Results.NotFound(new { Message = "Recipe not found" });
            
            image.Recipe = recipe;
            db.RecipeImages.Add(image);
            
            await db.SaveChangesAsync();

            return Results.Created($"/api/recipes/{recipeId}/images/{image.Id}", image);
        } 
    }
}
