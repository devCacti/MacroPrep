using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Services;
using MacroPrep.Shared.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.OpenApi.Models;
using MacroPrep.Shared.Models.User;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    // Tells Swagger: "Whenever you see DateOnly, treat it as a string formatted as a date"
    c.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date",
        Example = new Microsoft.OpenApi.Any.OpenApiString("2000-01-01")
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Development Testing", policy =>
    {
        policy.WithOrigins("https://localhost:7050", "https://localhost:7273")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("CompletedSetup", policy => policy.RequireClaim("setup_completed", "true"));
    options.AddPolicy("UncompletedSetup", policy => policy.RequireClaim("setup_completed", "false"));
});

var app = builder.Build();

// Auto-Heal the Database
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// This will create the database if it doesn't exist and apply any pending migrations automatically.
await db.Database.MigrateAsync();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("Development Testing");
}

app.UseAuthentication();
app.UseAuthorization();

// Helper method to configure cookie policy for authentication 
void SetSessionCookie(HttpContext context, Guid sessionId, string token, DateTimeOffset expires)
{
    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = expires,
        Path = "/api/auth"
    };

    context.Response.Cookies.Append("MacroPrepSession", $"{sessionId}|{token}", cookieOptions);
}

// API AUTH GROUP
var authGroup = app.MapGroup("/auth");

if (app.Environment.IsDevelopment())
{
    authGroup = app.MapGroup("/api/auth");
}

// Register Endpoint
authGroup.MapPost("/register", async (RegisterRequest request, AppDbContext db, ITokenService tokenService, HttpContext context) =>
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

    var newUser = new UserEntity
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

    SetSessionCookie(context, session.Id, session.Token, session.ExpiresAt);

    return Results.Ok(new {
        Token = tokenService.GenerateToken(newUser, session)
    });
})
.Produces<UserDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status409Conflict)
.WithName("Register")
.WithOpenApi();

// Login Endpoint
authGroup.MapPost("/login", async (LoginRequest request, AppDbContext db, ITokenService tokenService, HttpContext context) =>
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

    SetSessionCookie(context, session.Id, session.Token, session.ExpiresAt);

    return Results.Ok(new {Token = tokenService.GenerateToken(user, session)});
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.WithName("Login")
.WithOpenApi();

// Complete Account Setup Endpoint
authGroup.MapPost("/complete-setup", async (AccountSetupRequest request, AppDbContext db, ClaimsPrincipal user) =>
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
        return Results.Conflict(new { Message = "Account setup is already done for this user."});

    // Map the new data
    userEntity.FirstName = request.FirstName;
    userEntity.LastName = request.LastName;
    userEntity.DateOfBirth = request.DateOfBirth;
    userEntity.HasCompletedSetup = true; // The flag that unlocks the rest of the app
    userEntity.UpdatedAt = DateTimeOffset.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Profile updated successfully" });
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status409Conflict)
.WithName("CompleteAccountSetup")
.WithOpenApi()
.RequireAuthorization("UncompletedSetup"); // Only allow users who have NOT completed setup to access this endpoint

// Refresh Token Endpoint - This will be used to refresh the JWT token using the session cookie
authGroup.MapPost("/refresh", async (AppDbContext db, ITokenService tokenService, HttpContext context) =>
{
    var cookie = context.Request.Cookies["MacroPrepSession"];
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

    SetSessionCookie(context, session.Id, session.Token, session.ExpiresAt);

    return Results.Ok(new { Token = tokenService.GenerateToken(session.User!, session) });
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.WithName("RefreshToken")
.WithOpenApi()
.RequireAuthorization("Authenticated");

// API USER GROUP
var userGroup = app.MapGroup("/user");

if (app.Environment.IsDevelopment())
{
    userGroup = app.MapGroup("/api/user");
}

userGroup.MapGet("/profile", async (ClaimsPrincipal user, AppDbContext db) =>
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
})
.Produces<UserDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.WithName("GetUserProfile")
.WithOpenApi()
.RequireAuthorization("Authenticated")
.RequireAuthorization("CompletedSetup"); // Only allow users who have completed setup to access this endpoint

// API SHOPPING LISTS GROUP
var listsGroup = app.MapGroup("/s-lists");

if (app.Environment.IsDevelopment())
{
    listsGroup = app.MapGroup("/api/s-lists");
}

listsGroup.MapGet("/my-lists", async (ClaimsPrincipal claims, AppDbContext db) =>
{
    return Results.Ok();
});



// TEST ENDPOINT: Checks if Token Generation is the killer
app.MapGet("/test-token", (ITokenService tokenService) =>
{
    try
    {
        // 1. Create a Fake User
        var fakeUser = new UserEntity
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
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

app.Run();