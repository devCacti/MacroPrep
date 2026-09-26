using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MacroPrep.Server.Services
{
    public static class UserService
    {
        public static async Task<User?> GetUserAsync(string id, AppDbContext db)
        {
            Guid userId = Guid.Parse(id);

            User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            return user;
        }

        public static async Task<User?> GetUserAsync(Guid id, AppDbContext db)
        {
            return await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}