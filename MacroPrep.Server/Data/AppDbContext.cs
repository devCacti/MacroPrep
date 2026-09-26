using Microsoft.EntityFrameworkCore;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Data.Entities.SemanticVersioning;

namespace MacroPrep.Server.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        /// SHOPPING LISTS SECTION
        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<ListItem> ListItems { get; set; }
        public DbSet<ListMember> ListMembers { get; set; }
        /// END OF SHOPPPING LISTS SECTION

        /// RECIPES SECTION
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<Procedure> Procedures { get; set; }
        public DbSet<Instrument> Instruments { get; set; }
        public DbSet<RecipeImage> RecipeImages { get; set; }
        /// END OF RECIPES SECTION
        
        public DbSet<MeasuringUnit> MeasuringUnits { get; set; }
        public DbSet<Tag> Tags { get; set; }

        // SYSTEM RELATED ENTITIES
        public DbSet<SemanticVersion> SemanticVersions { get; set; }
    }
}
