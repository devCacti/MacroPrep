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
            return ings.RemoveAll(i => i.IsEmpty());
        }

        public static int RemoveAllEmpty(this List<RecipeProcedureDto> procs)
        {
            return procs.RemoveAll(p => p.IsEmpty());
        }
    }
}
