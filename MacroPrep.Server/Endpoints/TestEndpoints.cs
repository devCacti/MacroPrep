using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Services;

namespace MacroPrep.Server.Endpoints
{
    public static class TestEndpoints
    {
        public static void MapTestEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/").WithTags("Test Endpoints");
            if (app.Environment.IsDevelopment())
            {
                group = app.MapGroup("/api").WithTags("Test Endpoints");
            }

            group.MapGet("/test-connection", TestConnection)
                .Produces(StatusCodes.Status200OK)
                .WithName("TestConnection")
                .WithOpenApi();

            group.MapGet("/test-token", TestToken)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError)
                .WithName("TestToken")
                .WithOpenApi();
        }

        public static IResult TestConnection() => Results.Ok("Connection to the server is healthy!");

        public static IResult TestToken(ITokenService tokenService)
        {
            try
            {
                // 1. Create a Fake User
                var fakeUser = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = "TestUser",
                    Email = "test@test.com",
                    CreatedAt = DateTimeOffset.UtcNow // Valid date
                };

                var fakeSession = new UserSession
                {
                    Id = Guid.NewGuid(),
                    Token = "fake-token",
                    ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(15) // Not valid for long, but enough for testing
                };

                // 2. Try to Generate Token
                var token = tokenService.GenerateToken(fakeUser, fakeSession);

                // 3. If we get here, Token Service is HEALTHY
                return Results.Ok(new { Message = "Token Service Works!", Token = token });
            }
            catch (Exception ex)
            {
                // 4. If we crash, SHOW THE ERROR
                return Results.Problem($"CRASH: {ex.Message} \n\n {ex.StackTrace}");
            }
        }
    }
}
