using MacroPrep.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Models.Recipes
{
    public static class RecipeExtensions
    {
        public static int RemoveAllEmpty<T>(this List<T> items) where T : IFormItem
        {
            return items.RemoveAll(item => item.IsEmpty());
        }
    }
}
