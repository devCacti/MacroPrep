using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;

namespace MacroPrep.Client.Pages.Recipes
{
    public partial class Index
    {
        [Inject]
        private NavigationManager _navManager { get; set; } = default!;

        private void CreateNewRecipe()
        {
            Console.WriteLine("Redirecting to 'New Recipe' page...");
            _navManager.NavigateTo("/recipes/new");
        }
    }
}
