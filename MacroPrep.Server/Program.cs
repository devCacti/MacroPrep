using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Shared.Models;
using MacroPrep.Shared.Models.Auth;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;


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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

//! Please remove this before production
app.MapGet("/users", async (AppDbContext db) =>
{
    // This is just a simple example.
    var users = await db.Users.Select(user => new UserDto
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        IsVerified = user.IsVerified
    }).ToListAsync();

    return users;
}).WithName("GetUsers").WithOpenApi();

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// API GROUP
var apiGroup = app.MapGroup("/api");

apiGroup.MapPost("/auth/register", async (RegisterRequest request, AppDbContext db) =>
{
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

    try
    {
        db.Users.Add(newUser);
        await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        // Log the exception (not implemented here)
        //If it's in dev mode, we can return the exception message for easier debugging, but in production, we should return a generic error message to avoid exposing sensitive information.
        if (app.Environment.IsDevelopment())
            return Results.Problem($"An error occurred while creating the user: {ex.Message}", title: "Internal Server Error", statusCode: 500);

        return Results.Problem("An error occurred while creating the user. Please try again later.", title: "Internal Server Error", statusCode: 500);
    }

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

    return userDto;
})
.WithName("Register")
.WithOpenApi();

apiGroup.MapPost("/auth/login", async (LoginRequest request, AppDbContext db) =>
{

    return Results.Ok();
})
.WithName("Login")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
