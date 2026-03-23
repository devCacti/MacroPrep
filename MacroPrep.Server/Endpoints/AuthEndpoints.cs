using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Services;
using MacroPrep.Shared.Models.Auth;
using MacroPrep.Shared.Models.User;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MacroPrep.Server.Endpoints
{
    public static class AuthEndpoints
    {
        private static bool isProduction = false;

        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/auth").WithTags("Authentication");
            if (app.Environment.IsDevelopment())
                group = app.MapGroup("/api/auth").WithTags("Authentication");


            group.MapPost("/register", Register)
                .Produces<UserDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status409Conflict)
                .WithName("Register")
                .WithOpenApi();

            group.MapPost("/login", Login)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("Login")
                .WithOpenApi();

            group.MapPost("/complete-setup", CompleteSetup)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status409Conflict)
                .WithName("CompleteAccountSetup")
                .RequireAuthorization("UncompletedSetup") // Only allow users who have NOT completed setup to access this endpoint
                .WithOpenApi();

            group.MapPost("/refresh", RefreshToken)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("RefreshToken")
                .WithOpenApi();
        }

        // Helper method to configure cookie policy for authentication 
        private static void SetSessionCookie(HttpContext http, Guid sessionId, string token, DateTimeOffset expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.None,
                Expires = expires,
                Path = "/api/auth",
                IsEssential = !isProduction
            };

            http.Response.Cookies.Append("MacroPrepSession", $"{sessionId}|{token}", cookieOptions);
        }

        private static async Task<IResult> Register(RegisterRequest request, AppDbContext db, ITokenService tokenService, HttpContext http)
        {
            // Input Validation
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return Results.BadRequest(new { Message = "All fields are required" });

            var userExists = await db.Users.AnyAsync(u => u.UserName == request.UserName || u.Email == request.Email);
            if (userExists)
                return Results.Conflict(new { Message = "Username or email already exists" });

            // Password Hashing
            string salt = BCrypt.Net.BCrypt.GenerateSalt(12); // 12 Factor salt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.Users.Add(newUser);

            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString(), // Rotation Secret
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7), // Expires after 7 days
                UserId = newUser.Id,
                User = newUser
            };

            db.UserSessions.Add(session);
            await db.SaveChangesAsync();

            SetSessionCookie(http, session.Id, session.Token, session.ExpiresAt);

            return Results.Ok(new
            {
                Token = tokenService.GenerateToken(newUser, session)
            });
        }
    
        private static async Task<IResult> Login(LoginRequest request, AppDbContext db, ITokenService tokenService, HttpContext http)
        {
            // Input Validation
            if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { Message = "Username/Email and password are required" });

            var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserNameOrEmail || u.Email == request.UserNameOrEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7), // Expires after 7 days
                UserId = user.Id,
                User = user
            };

            db.UserSessions.Add(session);
            await db.SaveChangesAsync();

            SetSessionCookie(http, session.Id, session.Token, session.ExpiresAt);

            return Results.Ok(new { Token = tokenService.GenerateToken(user, session) });
        }
    
        private static async Task<IResult> CompleteSetup(AccountSetupRequest request, AppDbContext db, ClaimsPrincipal user, ITokenService tokenService)
        {
            // Extract UserID from the JWT Claim
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Results.Unauthorized();

            var userGuid = Guid.Parse(userIdClaim);
            var userEntity = await db.Users.FirstOrDefaultAsync(u => u.Id == userGuid);

            if (userEntity == null)
                return Results.NotFound();

            if (userEntity.HasCompletedSetup)
                return Results.Conflict(new { Message = "Account setup is already done for this user." });

            // Map the new data
            userEntity.FirstName = request.FirstName;
            userEntity.LastName = request.LastName;
            userEntity.DateOfBirth = request.DateOfBirth;
            userEntity.HasCompletedSetup = true; // The flag that unlocks the rest of the app
            userEntity.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new {
                Message = "Profile updated successfully",
            });
        }

        private static async Task<IResult> RefreshToken(AppDbContext db, ITokenService tokenService, HttpContext http)
        {
            var cookie = http.Request.Cookies["MacroPrepSession"];
            if (string.IsNullOrEmpty(cookie) || !cookie.Contains("|"))
                return Results.Unauthorized();

            var parts = cookie.Split('|');
            var sessionId = Guid.Parse(parts[0]);
            var sessionToken = parts[1];

            var session = await db.UserSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null || session.IsRevoked || session.ExpiresAt < DateTimeOffset.UtcNow || session.Token != sessionToken)
            {
                if (session != null)
                {
                    session.IsRevoked = true; // Revoke the session if token is invalid or expired. A compromised account will have its 
                    await db.SaveChangesAsync();
                }
                return Results.Unauthorized();
            }

            session.Token = Guid.NewGuid().ToString(); // Rotate the session token
            session.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7); // Extend the session expiration

            await db.SaveChangesAsync();

            SetSessionCookie(http, session.Id, session.Token, session.ExpiresAt);

            return Results.Ok(new { Token = tokenService.GenerateToken(session.User!, session) });
        }

    }
}
