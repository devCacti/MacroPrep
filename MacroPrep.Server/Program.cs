using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Services;
using MacroPrep.Shared.Models;
using MacroPrep.Shared.Models.Auth;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//if (builder.Environment.IsDevelopment())
//{
// Will use SQLite in development, but SQL Server is better for production and more realistic testing
//    builder.Services.AddDbContext<AppDbContext>(options =>
//        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
//} else {
// Will use SQL Server in production, but SQLite is easier for development and testing
//    builder.Services.AddDbContext<AppDbContext>(options =>
//        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
//}

builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

// Auto-Heal the Database
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// This will create the database if it doesn't exist and apply any pending migrations automatically.
await db.Database.MigrateAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

// API GROUP
var authGroup = app.MapGroup("/api/auth");

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

    // Return the DTO (Data Transfer Object)
    var userDto = new UserDto
    {
        Id = newUser.Id,
        UserName = newUser.UserName,
        Email = newUser.Email,
        FirstName = newUser.FirstName,
        LastName = newUser.LastName,
        IsVerified = newUser.IsVerified,
        CreatedAt = newUser.CreatedAt,
        UpdatedAt = newUser.UpdatedAt
    };

    SetSessionCookie(context, session.Id, session.Token, session.ExpiresAt);

    return Results.Ok(new {
        Token = tokenService.GenerateToken(newUser, session),
        User = userDto
    });
})
.Produces<UserDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status409Conflict)
.Produces(StatusCodes.Status400BadRequest)
.WithName("Register")
.WithOpenApi();

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

    return Results.Ok( new { Token = tokenService.GenerateToken(session.User!, session)});
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.WithName("RefreshToken")
.WithOpenApi();

app.Run();