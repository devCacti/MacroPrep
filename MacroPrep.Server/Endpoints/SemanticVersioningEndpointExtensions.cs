using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Data.Entities.SemanticVersioning;
using MacroPrep.Shared.Enums;
using MacroPrep.Shared.Enums.Account;
using MacroPrep.Shared.Models.SemanticVersioning;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MacroPrep.Server.Endpoints
{
    public static class SemanticVersioningEndpointExtensions
    {
        // NOTE: Client and Server will likely not have the same version numbers
        public static void MapSemanticVersioningEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/version").WithTags("Semantic Versioning");

            // For Development Only
            if (app.Environment.IsDevelopment())
                group = app.MapGroup("/api/version").WithTags("Semantic Versioning");

            // Client Versioning Endpoints
            // Get Client is the only endpoint that does not require auth
            group.MapGet("/client", GetClientVersion)
                .Produces<SemanticVersionDto>(StatusCodes.Status200OK)
                .WithName("GetClientVersion")
                .WithOpenApi();

            // Server Versioning Endpoints
            group.MapGet("/server", GetServerVersion)
                .RequireAuthorization()
                .Produces<SemanticVersionDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("GetServerVersion")
                .WithOpenApi();

            // Operation Endpoints for Versioning
            group.MapPost("/", CreateVersion)
                .RequireAuthorization()
                .Accepts<CreateSemanticVersionRequest>("application/json")
                .Produces<SemanticVersion>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("CreateVersion")
                .WithOpenApi();

            group.MapPut("/{versionId:guid}", UpdateVersion)
                .RequireAuthorization()
                .Accepts<UpdateSemanticVersionRequest>("application/json")
                .Produces<SemanticVersion>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("UpdateVersion")
                .WithOpenApi();

            group.MapDelete("/{versionId:guid}", DeleteVersion)
                .RequireAuthorization()
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("DeleteVersion")
                .WithOpenApi();
        }

        // GET: /api/version/client
        private static IResult GetClientVersion(AppDbContext db)
        {
            var version = db.SemanticVersions
                .Where(v => v.Component == SystemComponent.Client)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new SemanticVersionDto
                {
                    VersionID = v.VersionID,
                    Major = v.Major,
                    Minor = v.Minor,
                    Patch = v.Patch,
                    Hash = v.Hash,
                    Component = v.Component
                })
                .FirstOrDefault();

            return Results.Ok(version);
        }

        // POST: /api/version
        private static async Task<IResult> CreateVersion(CreateSemanticVersionRequest semVer, AppDbContext db, ClaimsPrincipal claims)
        {
            // Get User ID and Validate
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.BadRequest("Invalid or missing User ID in token.");

            // Require Admin Previleges 
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Type != AccountType.Admin)
                return Results.Forbid();

            var version = new SemanticVersion
            {
                Major = semVer.Major,
                Minor = semVer.Minor,
                Patch = semVer.Patch,
                Hash = semVer.Hash,
                Component = semVer.Component,
                CreatedByUserID = userId,
                UpdatedByUserID = userId
            };

            db.SemanticVersions.Add(version);
            await db.SaveChangesAsync();

            return Results.Ok(version);
        }

        // PUT: /api/version
        private static async Task<IResult> UpdateVersion(Guid versionId, UpdateSemanticVersionRequest semVer, AppDbContext db, ClaimsPrincipal claims)
        {
            // Get User ID and Validate
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.BadRequest("Invalid or missing User ID in token.");

            // Require Admin Previleges 
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Type != AccountType.Admin)
                return Results.Forbid();

            // Find the version to update
            var version = await db.SemanticVersions.FirstOrDefaultAsync(v => v.VersionID == versionId);

            if (version == null)
                return Results.NotFound("Version not found.");

            // Update it
            version.Major = semVer.Major ?? version.Major;
            version.Minor = semVer.Minor ?? version.Minor;
            version.Patch = semVer.Patch ?? version.Patch;
            version.Hash = semVer.Hash ?? version.Hash;
            version.UpdatedByUserID = userId;
            version.UpdatedAt = DateTime.UtcNow;
            version.VersionIsValid = semVer.SetValid ?? version.VersionIsValid;

            await db.SaveChangesAsync();

            return Results.Ok(version);
        }

        // DELETE: /api/version
        private static async Task<IResult> DeleteVersion(Guid versionId, AppDbContext db, ClaimsPrincipal claims)
        {
            // Get User ID and Validate
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.BadRequest("Invalid or missing User ID in token.");

            // Require Admin Previleges 
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Type != AccountType.Admin)
                return Results.Forbid();

            // Find the version to delete
            var version = await db.SemanticVersions.FirstOrDefaultAsync(v => v.VersionID == versionId);

            if (version == null)
                return Results.NotFound("Version not found.");

            db.SemanticVersions.Remove(version);
            await db.SaveChangesAsync();

            return Results.Ok();
        }

        // GET: /api/version/server
        private static async Task<IResult> GetServerVersion(AppDbContext db, ClaimsPrincipal claims)
        {
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Results.BadRequest("Invalid or missing User ID in token.");

            var version = await db.SemanticVersions
                .Where(v => v.Component == SystemComponent.Server)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new SemanticVersionDto
                {
                    VersionID = v.VersionID,
                    Major = v.Major,
                    Minor = v.Minor,
                    Patch = v.Patch,
                    Hash = v.Hash,
                    Component = v.Component
                })
                .FirstOrDefaultAsync();

            return Results.Ok(version);
        }
    }
}
