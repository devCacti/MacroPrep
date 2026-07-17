using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Models.Recipes
{
    public static class RecipeExtensions
    {
        public static int RemoveAllEmpty(this List<RecipeIngredientDto> ings)
        {
            int howManyCleaned = ings.RemoveAll(i => i.IsEmpty());

            return howManyCleaned;
        }
    }
}
