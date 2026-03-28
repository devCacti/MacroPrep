using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Models.Recipes
{
    public class RecipeDto
    {
        [Required]
        public string Name { get; set; }

        [MaxLength(250)]
        public string? Description { get; set; }

        public float Servings { get; set; } = 1;

        // Time
        public float CookingTimeMinutes { get; set; } = 0;
        public float PreparationTimeMinutes { get; set; } = 0;
        public float RestingTimeMinutes { get; set; } = 0;
        public virtual float TotalTimeMinutes { get => CookingTimeMinutes + PreparationTimeMinutes + RestingTimeMinutes; }


        public RecipeDto()
        {
            Name = string.Empty;
        }

    }
}
