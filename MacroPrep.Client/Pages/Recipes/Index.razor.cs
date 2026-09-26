using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;

namespace MacroPrep.Client.Pages.Recipes
{
    public partial class Index
    {
        [Inject]
        private NavigationManager NavManager { get; set; } = default!;

        private void CreateNewRecipe()
        {
            Console.WriteLine("Redirecting to 'New Recipe' page...");
            NavManager.NavigateTo("/recipes/create");
        }
    }
}
