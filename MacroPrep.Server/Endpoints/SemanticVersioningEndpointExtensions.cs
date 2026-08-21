using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Data.Entities.SemanticVersioning;
using MacroPrep.Shared.Enums;
using MacroPrep.Shared.Enums.Account;
using MacroPrep.Shared.Models.SemanticVersioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
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
            // Gives the current active client version (Or null if not set)
            group.MapGet("/client", GetClientVersion)
                .Produces<SemanticVersionDto>(StatusCodes.Status200OK)
                .WithName("GetClientVersion")
                .WithOpenApi();

            group.MapGet("/", GetAllVersions)
                .RequireAuthorization()
                .Produces<List<SemanticVersionDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("GetAllVersions")
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
                .Accepts<SemanticVersionDto>("application/json")
                .Produces<SemanticVersion>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("CreateVersion")
                .WithOpenApi();

            group.MapPut("/{versionId:guid}", UpdateVersion)
                .RequireAuthorization()
                .Accepts<SemanticVersionDto>("application/json")
                .Produces<SemanticVersion>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("UpdateVersion")
                .WithOpenApi();

            group.MapPatch("/set-active", SetVersionActive)
                .RequireAuthorization()
                .Produces<SemanticVersion>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("SetVersionActive")
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

        // GET: /api/version/client (Accepts version number)
        private static async Task<IResult> GetClientVersion(AppDbContext db, [FromQuery] Guid? versionId = null)
        {
            // version variable is nullable, this endpoint can return null if none is set to active
            SemanticVersion? currentVersion = await db.SemanticVersions
                .Where(v => v.Component == SystemComponent.Client && v.ActiveVersion)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            // Get a boolean indicating (if the request provided a version ID) wether the client will need to update to the current version or not
            // This will happen if along the way the client has fallen behind a version that requires a forced refresh
            // If in between the clients version and the current version there is a version with ForceRefresh set to true, then the client will need to update
            // The server will also provide a number with how many versions the client is behind
            if (versionId.HasValue && currentVersion != null)
            {
                var clientVersion = await db.SemanticVersions.FirstOrDefaultAsync(v => v.VersionID == versionId.Value);

                if (clientVersion == null)
                    return Results.BadRequest("Invalid version ID provided.");

                // Check if the client is behind and if any version in between has ForceRefresh set to true
                var versionsInBetween = await db.SemanticVersions
                    .Where(v => v.Component == SystemComponent.Client &&
                                v.CreatedAt > clientVersion.CreatedAt &&
                                v.CreatedAt <= currentVersion.CreatedAt)
                    .OrderBy(v => v.CreatedAt)
                    .ToListAsync();

                bool needsUpdate = versionsInBetween.Any(v => v.ForceRefresh);

                // "You are w versions behind, and you need to update to the latest version."
                int total = versionsInBetween.Count;

                if (needsUpdate)
                {
                    return Results.Ok(new
                    {
                        CurrentVersion = currentVersion,
                        VersionsBehind = total,
                        NeedsUpdate = true
                    });
                }
            }

            return Results.Ok(currentVersion);
        }

        // GET: /api/version
        // This endpoint is for Admins only to get all versions in the database
        private static async Task<IResult> GetAllVersions(
            AppDbContext db, ClaimsPrincipal claims, 
            [FromQuery] int itemsPerPage    = 10,
            [FromQuery] int pageNumber      = 1
            ){
            // Get User ID and Validate
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim ?? throw new InvalidOperationException("User ID claim is missing."));

            var versions = await db.SemanticVersions
                    .OrderByDescending(v => v.CreatedAt)
                    .Skip((pageNumber - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .Select(v => new SemanticVersionDto
                    {
                        VersionID = v.VersionID,
                        Major = v.Major,
                        Minor = v.Minor,
                        Patch = v.Patch,
                        Hash = v.Hash,
                        Component = v.Component,
                        ActiveVersion = v.ActiveVersion,
                        ForceRefresh = v.ForceRefresh
                    })
                    .ToListAsync();

            int totalCount = await db.SemanticVersions.CountAsync();

            // Response with pagination info
            var response = new SemanticVersionsPagesDto
            {
                VersionCount = totalCount,
                PageCount = (int)Math.Ceiling((double)totalCount / itemsPerPage),
                Versions = versions
            };
            return Results.Ok(response);
        }

        // POST: /api/version
        private static async Task<IResult> CreateVersion(SemanticVersionDto semVer, AppDbContext db, ClaimsPrincipal claims)
        {
            // Get User ID and Validate
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim ?? throw new InvalidOperationException("User ID claim is missing."));

            var version = new SemanticVersion
            {
                Major = semVer.Major,
                Minor = semVer.Minor,
                Patch = semVer.Patch,
                Hash = semVer.Hash,
                Component = semVer.Component,
                ActiveVersion = semVer.ActiveVersion,
                ForceRefresh = semVer.ForceRefresh,
                CreatedByUserID = userId,
                UpdatedByUserID = userId
            };

            // Set all to not active before adding the new item
            if (semVer.ActiveVersion)
                await db.SemanticVersions.ExecuteUpdateAsync(v => v.SetProperty(e => e.ActiveVersion, false));

            // Add and save
            await db.SemanticVersions.AddAsync(version);
            await db.SaveChangesAsync();

            return Results.Created($"api/version/{version.VersionID}", version);
        }

        // PUT: /api/version
        private static async Task<IResult> UpdateVersion(Guid versionId, SemanticVersionDto semVer, AppDbContext db, ClaimsPrincipal claims)
        {
            // Get User ID and Validate
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim ?? throw new InvalidOperationException("User ID claim is missing."));

            // Find the version to update
            var version = await db.SemanticVersions.FirstOrDefaultAsync(v => v.VersionID == versionId);

            if (version == null)
                return Results.NotFound("Version not found.");

            // Update values, this directly updates the values on the database, all that is left is to save changes
            version.Major = semVer.Major;
            version.Minor = semVer.Minor;
            version.Patch = semVer.Patch;
            version.Hash = semVer.Hash;
            version.UpdatedByUserID = userId;
            version.UpdatedAt = DateTime.UtcNow;
            version.ActiveVersion = semVer.ActiveVersion;
            version.ForceRefresh = semVer.ForceRefresh;
            await db.SaveChangesAsync();

            return Results.Ok(version);
        }

        // PATCH: /api/version/set-active?verionId={id}
        private static async Task<IResult> SetVersionActive([FromQuery] Guid versionId, AppDbContext db, ClaimsPrincipal claims)
        {
            // Get User ID and Validate
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim ?? throw new InvalidOperationException("User ID claim is missing."));

            // Find the version to update
            var version = await db.SemanticVersions.FirstOrDefaultAsync(v => v.VersionID == versionId);

            if (version == null)
                return Results.NotFound("Version not found.");

            await db.SemanticVersions.ExecuteUpdateAsync(v => v.SetProperty(e => e.ActiveVersion, false));

            version.ActiveVersion = true;
            await db.SaveChangesAsync();

            // If it succeeds, the client already knows what changed, no need to tell it
            return Results.NoContent();
        }

        // DELETE: /api/version
        private static async Task<IResult> DeleteVersion(Guid versionId, AppDbContext db, ClaimsPrincipal claims)
        {
            // Get User ID and Validate
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim ?? throw new InvalidOperationException("User ID claim is missing."));

            // Find the version to delete
            var version = await db.SemanticVersions.FirstOrDefaultAsync(v => v.VersionID == versionId);

            if (version == null)
                return Results.NotFound("Version not found.");

            db.SemanticVersions.Remove(version);
            await db.SaveChangesAsync();

            // NoContent is returned because the version has been deleted and there is no content to return
            return Results.NoContent();
        }

        // Even though not admin, this will require auth, will fail if the user is not valid
        // GET: /api/version/server
        private static async Task<IResult> GetServerVersion(AppDbContext db, ClaimsPrincipal claims)
        {
            var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.Parse(userIdClaim ?? throw new InvalidOperationException("User ID claim is missing."));

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
