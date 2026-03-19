using Microsoft.EntityFrameworkCore;
using MacroPrep.Server.Data.Entities;

namespace MacroPrep.Server.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<ListItem> ListItems { get; set; }
        public DbSet<ListMember> ListMembers { get; set; }
    }
}
