using Microsoft.EntityFrameworkCore;
using MacroPrep.Server.Data.Entities;

namespace MacroPrep.Server.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<UserEntity> Users { get; set; }
    }
}
