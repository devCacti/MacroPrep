using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Services;
using MacroPrep.Shared.Models.Auth;
using MacroPrep.Shared.Models.User;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.OpenApi.Models;

namespace MacroPrep.Server.Endpoints
{
    public static class AuthEndpoints
    {
        private static bool _isProduction = false;

        public static bool IsProduction => _isProduction;

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
                .WithOpenApi(operation =>
                {
                    operation.Summary = "Register a new user account";
                    operation.Description = "Creates a new user account with the provided username, email, and password. Returns a JWT token upon successful registration.";
                    operation.RequestBody.Description = "Registration details including username, email, password, and confirm password.";
                    operation.RequestBody.Required = true;
                    operation.RequestBody.Content["application/json"].Schema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = nameof(RegisterRequestDto)
                        }
                    };

                    operation.Responses["200"].Description = "User registered successfully. Returns a JWT token.";
                    operation.Responses["400"].Description = "Bad request. This can occur if required fields are missing or invalid.";
                    operation.Responses["409"].Description = "Conflict. This can occur if the username or email already exists.";

                    return operation;
                });

            group.MapPost("/login", Login)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("Login")
                .WithOpenApi(operation =>
                {
                    operation.Summary = "Authenticate a user and return a JWT token";
                    operation.Description = "Authenticates a user using their username/email and password. Returns a JWT token upon successful authentication.";
                    operation.RequestBody.Description = "Login details including username/email and password.";
                    operation.RequestBody.Required = true;
                    operation.RequestBody.Content["application/json"].Schema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = nameof(LoginRequestDto)
                        }
                    };

                    operation.Responses["200"].Description = "User authenticated successfully. Returns a JWT token.";
                    operation.Responses["400"].Description = "Bad request. This can occur if required fields are missing or invalid.";
                    operation.Responses["401"].Description = "Unauthorized. This can occur if the username/email or password is incorrect.";

                    return operation;
                });

            group.MapPost("/complete-setup", CompleteSetup)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status409Conflict)
                .WithName("CompleteAccountSetup")
                .RequireAuthorization("UncompletedSetup") // Only allow users who have NOT completed setup to access this endpoint
                .WithOpenApi(operation =>
                {
                    operation.Summary = "Complete initial account setup";
                    operation.Description = "Allows authenticated users who have not completed their account setup to provide additional profile information. This endpoint is typically used for onboarding new users after registration.";
                    operation.RequestBody.Description = "Account setup details including first name, last name, and date of birth.";
                    operation.RequestBody.Required = true;
                    operation.RequestBody.Content["application/json"].Schema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = nameof(AccountSetupRequest)
                        }
                    };

                    operation.Responses["200"].Description = "Account setup completed successfully.";
                    operation.Responses["400"].Description = "Bad request. This can occur if required fields are missing or invalid.";
                    operation.Responses["401"].Description = "Unauthorized. This can occur if the user is not authenticated.";
                    operation.Responses["403"].Description = "Forbidden. This can occur if the user has already completed account setup and is trying to access this endpoint again.";
                    operation.Responses["409"].Description = "Conflict. This can occur if there is a concurrency issue while updating the user's profile.";

                    return operation;
                });

            group.MapPost("/refresh", RefreshToken)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("RefreshToken")
                .WithOpenApi(operation =>
                {
                    operation.Summary = "Refresh JWT token using session cookie";
                    operation.Description = "Refreshes the JWT token for an authenticated user using the session cookie. This endpoint checks the validity of the session and issues a new JWT token if the session is still valid.";

                    operation.Responses["200"].Description = "Token refreshed successfully. Returns a new JWT token.";
                    operation.Responses["401"].Description = "Unauthorized. This can occur if the session cookie is missing, invalid, expired, or if the session has been revoked.";

                    return operation;
                });
        }

        private static async Task<IResult> Register(RegisterRequestDto request, AppDbContext db, ITokenService tokenService, HttpContext http)
        {
            // Input Validation
            if (request.EmptyCheck() || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return Results.BadRequest(new { Message = "All fields are required" });

            if (await request.ExistsCheck(db))
                return Results.Conflict(new { Message = "Username or email already exists" });

            // Password Hashing
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = passwordHash
            };

            await db.Users.AddAsync(newUser);
            await db.SaveChangesAsync();

            UserSession? session = await newUser.CreateSessionAsync(db);

            if (session == null)
                return Results.StatusCode(StatusCodes.Status500InternalServerError);

            session.SetSessionCookie(http);

            return Results.Ok(new {
                Token = tokenService.GenerateToken(newUser, session)
            });
        }
    
        private static async Task<IResult> Login(LoginRequestDto request, AppDbContext db, ITokenService tokenService, HttpContext http)
        {
            // Input Validation
            if (request.EmptyCheck())
                return Results.BadRequest(new { Message = "Username and password are required" });

            if (!await request.ExistsCheck(db))
                return Results.BadRequest(new { Message = "Wrong username/email or password" });

            var (isValid, user) = await request.ValidateCredentials(db);

            if (!isValid || user == null) // Null check is redundant but added to avoid null check operators
                return Results.Unauthorized();

            UserSession? session = await user.CreateSessionAsync(db);

            if (session == null)
                // This should not happen, but if it does, we should trigger a 500 error so we can investigate the issue.
                return Results.StatusCode(StatusCodes.Status500InternalServerError);

            session.SetSessionCookie(http);

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

            UserSession? session = await db.UserSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sessionId);

            if (!await session.CheckSession(db, sessionToken))
                return Results.Unauthorized();

            // The null check operator is required even though we check the session for null, the compiler just doesn't know that
            session!.Token = Guid.NewGuid().ToString(); // Rotate the session token and update the db
            await db.SaveChangesAsync();

            session.SetSessionCookie(http);

            return Results.Ok(new { Token = tokenService.GenerateToken(session.User!, session) });
        }

    }
}
