using MacroPrep.Server.Data;
using MacroPrep.Shared.Models.User;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MacroPrep.Server.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/user").WithTags("Users");
            if (app.Environment.IsDevelopment())
                group = app.MapGroup("/api/user").WithTags("Users");

            group.MapGet("/profile", GetUserProfile)
                .Produces<UserDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("GetUserProfile")
                .WithOpenApi()
                .RequireAuthorization("Authenticated")
                .RequireAuthorization("CompletedSetup");
        }

        private static async Task<IResult> GetUserProfile(ClaimsPrincipal user, AppDbContext db)
        {
            var id = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");

            var userDto = await db.Users
                .Where(u => u.Id == id)
                .Select(u => new UserDto
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    UserName = u.UserName,
                    Email = u.Email,
                    Plan = u.Plan,
                    Type = u.Type,
                })
                .FirstOrDefaultAsync();

            if (userDto == null) return Results.NotFound();
            return Results.Ok(userDto);
        }
    }
}
