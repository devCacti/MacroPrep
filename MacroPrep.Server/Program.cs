using MacroPrep.Server.Hubs;
using MacroPrep.Server.Data;
using MacroPrep.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.SignalR;
using Azure;
using MacroPrep.Server.Endpoints;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DateOnly data type
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

// Add a CORS policy (Will only be used in development mode)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SignalRPolicy", policy =>
        {
            policy.WithOrigins("https://localhost:7050", "http://localhost:7273")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

// Add the Database context for the "DefaultConnection" application variable
builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add the token service to the services
builder.Services.AddScoped<ITokenService, TokenService>();

// Add authentication schemes
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

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
            (path.StartsWithSegments("/api/hubs/shopping-hub")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"\n > [Auth] VALIDATION FAILED: {context.Exception.Message}\n");
            return Task.CompletedTask;
        },

        // 3. Debug Successful Token Parsing
        OnTokenValidated = context =>
        {
            Console.WriteLine($"\n > [Auth] Token Validated! User: {context.Principal?.Identity?.Name}\n");
            // Optional: Print claims to see if 'sub' or 'nameid' exists
            foreach (var claim in context.Principal?.Claims ?? [])
            {
                Console.WriteLine($"       Claim: {claim.Type} = {claim.Value}");
            }
            return Task.CompletedTask;
        }
    };
});

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("CompletedSetup", policy => policy.RequireClaim("setup_completed", "true"));
    options.AddPolicy("UncompletedSetup", policy => policy.RequireClaim("setup_completed", "false"));
});

// Add SignalR to the services
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// Build the app
var app = builder.Build();

// Auto-Heal the Database
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// This will create the database if it doesn't exist and apply any pending migrations automatically.
await db.Database.MigrateAsync();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Use SwaggerUI and CORS policy if in dev environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("SignalRPolicy");
}

// Add Auth
app.UseAuthentication();
app.UseAuthorization();

// API HUB ENDPOINT
// Used for SignalR
if (app.Environment.IsDevelopment())
{
    // In production every endpoint will be under /api/ without choice due to being in the "api" alias, so we have to simulate it in development
    app.MapHub<ShoppingHub>("/api/hubs/shopping-hub");
}
else
{
    app.MapHub<ShoppingHub>("/hubs/shopping-hub");
}

app.MapAuthEndpoints();             // API AUTH GROUP
app.MapUserEndpoints();             // API USER GROUP
app.MapShoppingListsEndpoints();    // API SHOPPING LISTS GROUP
app.MapTestEndpoints();             // API TESTING GROUP

app.Run();