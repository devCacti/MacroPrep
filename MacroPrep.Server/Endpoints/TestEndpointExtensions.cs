using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Services;
using System.Security.Claims;

namespace MacroPrep.Server.Endpoints
{
    public static class TestEndpointExtensions
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
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status500InternalServerError)
                .WithName("TestToken")
                .WithOpenApi();
        }

        public static IResult TestConnection() => Results.Ok("Connection to the server is healthy!");

        public static IResult TestToken(ITokenService tokenService, ClaimsPrincipal claims)
        {
            try
            {
                var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Results.Unauthorized();

                // 3. If we get here, Token Service is HEALTHY
                return Results.Ok(new { Message = "Token Service Alive and Well (and you are authenticated! ;)" });
            }
            catch (Exception ex)
            {
                // 4. If we crash, SHOW THE ERROR
                return Results.Problem($"CRASH: {ex.Message} \n\n {ex.StackTrace}");
            }
        }
    }
}
