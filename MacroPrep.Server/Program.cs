using MacroPrep.Server.Hubs;
using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Services;
using MacroPrep.Shared.Models.Auth;
using MacroPrep.Shared.Models.User;
using MacroPrep.Shared.Models.ShoppingLists;
using MacroPrep.Shared.Enums.ShoppingLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.SignalR;


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
    .WithOpenApi();

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
var listsGroup = app.MapGroup("/shopping-lists");

if (app.Environment.IsDevelopment())
{
    listsGroup = app.MapGroup("/api/shopping-lists");
}

// BASE LISTS ENDPOINTS (Create, Read, Update, Delete) with proper authorization and error handling
// CREATE
listsGroup.MapPost("/", async (ListDto list, AppDbContext db, ClaimsPrincipal claims) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var listExists = await db.ShoppingLists.AnyAsync(l => l.Id == list.Id);
        if (listExists)
            return Results.Conflict(new { Message = "A list with the same ID already exists" });

        var newList = new ShoppingList
        {
            Id = list.Id,
            Name = list.Name,
            OwnerId = Guid.Parse(userIdClaim),
            IsShared = list.IsShared,
            CreatedAt = list.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.ShoppingLists.Add(newList);
        await db.SaveChangesAsync();

        return Results.Created($"/api/shopping-lists/{newList.Id}", newList.Id);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while creating the list: {ex.Message}");
    }
})
    .Produces<Guid>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization("Authenticated")
    .WithName("CreateShoppingList")
    .WithOpenApi()
    ;

// READ
listsGroup.MapGet("/{listId:guid}", async (Guid listId, AppDbContext db, ClaimsPrincipal claims) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var list = await db.ShoppingLists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null) return Results.NotFound();

        var currentUserId = Guid.Parse(userIdClaim);
        var member = list.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // The logic: If you aren't the owner, you MUST be an accepted member with Editor role.
        bool isOwner = list.OwnerId == currentUserId;
        bool isValidMember = member != null && member.HasAccepted;

        if (!isOwner && !isValidMember)
        {
            return Results.Forbid();
        }

        List<ListItem> listItems = await db.ListItems.Where(i => i.ListId == listId).ToListAsync();
        List<ListMember> listMembers = new();

        var listDto = new ListDto
        {
            Id = list.Id,
            Name = list.Name,
            OwnerId = list.OwnerId,
            OwnerName = db.Users.FirstOrDefault(u => u.Id == list.OwnerId)!.UserName ?? "User",
            IsShared = list.IsShared,
            CreatedAt = list.CreatedAt,
            UpdatedAt = list.UpdatedAt,
            Items = listItems.Select(i => new ListItemDto
            {
                Id = i.Id,
                ListId = i.ListId,
                Name = i.Name,
                Quantity = i.Quantity,
                IsChecked = i.IsChecked,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList(),
            Members = list.Members.Select(m => new ListMemberDto
            {
                Id = m.Id,
                ListId = list.Id,
                UserId = m.UserId,
                UserName = m.UserName,
                Type = m.Type,
                HasAccepted = m.HasAccepted
            }).ToList()
        };

        return Results.Ok(listDto);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while fetching the list: {ex.Message}");
    }
})
    .Produces<ListDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("Authenticated")
    .WithName("GetShoppingList")
    .WithOpenApi()
    ;

// UPDATE
listsGroup.MapPut("/{listId:guid}", async (Guid listId, ListDto updatedList, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var list = await db.ShoppingLists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null) return Results.NotFound();

        var currentUserId = Guid.Parse(userIdClaim);
        var member = list.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // The logic: If you aren't the owner, you MUST be an accepted member with Editor role.
        bool isOwner = list.OwnerId == currentUserId;
        bool isValidMember = member != null && member.HasAccepted && member.Type >= MemberType.Master;

        if (!isOwner && !isValidMember)
        {
            return Results.Forbid();
        }

        list.Name = updatedList.Name;
        list.IsShared = updatedList.IsShared;
        list.UpdatedAt = updatedList.UpdatedAt;

        await db.SaveChangesAsync();

        await hubContext.Clients.Group(listId.ToString()).ListUpdated(listId.ToString());

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while updating the list: {ex.Message}");
    }
})
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("Authenticated")
    .WithName("UpdateShoppingList")
    .WithOpenApi()
    ;

// DELETE
listsGroup.MapDelete("/{listId:guid}", async (Guid listId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var list = await db.ShoppingLists.FirstOrDefaultAsync(l => l.Id == listId);
        if (list == null)
            return Results.NotFound();

        if (list.OwnerId != Guid.Parse(userIdClaim))
            return Results.Forbid();

        // Delete all items and members associated with the list
        var items = db.ListItems.Where(i => i.ListId == listId);
        var members = db.ListMembers.Where(m => m.ListId == listId);
        db.ListItems.RemoveRange(items);
        db.ListMembers.RemoveRange(members);
        db.ShoppingLists.Remove(list);

        await db.SaveChangesAsync();
        await hubContext.Clients.Group(listId.ToString()).ListDeleted(listId.ToString());

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while deleting the list: {ex.Message}");
    }
})
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("Authenticated")
    .WithName("DeleteShoppingList")
    .WithOpenApi()
    ;

// ITEMS ENDPOINTS (Create, Update, Delete) with proper authorization and error handling will be added here (not included in this snippet for brevity)
// CREATE
listsGroup.MapPost("/{listId:guid}/items", async (Guid listId, ListItemDto item, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var list = await db.ShoppingLists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null) return Results.NotFound();

        var currentUserId = Guid.Parse(userIdClaim);
        var member = list.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // The logic: If you aren't the owner, you MUST be an accepted member with Editor role.
        bool isOwner = list.OwnerId == currentUserId;
        bool isValidMember = member != null && member.HasAccepted && member.Type >= MemberType.Editor;

        if (!isOwner && !isValidMember)
        {
            return Results.Forbid();
        }

        var itemExists = await db.ListItems.AnyAsync(i => i.Id == item.Id && i.ListId == listId);

        if (itemExists)
            return Results.Conflict(new { Message = "An item with the same ID already exists in this list" });

        var newItem = new ListItem
        {
            Id = item.Id,
            ListId = listId,
            Name = item.Name,
            Quantity = item.Quantity,
            IsChecked = item.IsChecked,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.ListItems.Add(newItem);
        list.UpdatedAt = DateTimeOffset.UtcNow; // Update the list's UpdatedAt when a new item is added

        await db.SaveChangesAsync();

        await hubContext.Clients.Group(listId.ToString()).ListUpdated(listId.ToString());

        return Results.Created($"/api/shopping-lists/{listId}/items/{newItem.Id}", newItem.Id);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while adding the item: {ex.Message}");
    }
})
    .Produces<Guid>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("Authenticated")
    .WithName("AddListItem")
    .WithOpenApi()
    ;

// READ
listsGroup.MapGet("/{listId:guid}/items", async (Guid listId, AppDbContext db, ClaimsPrincipal claims) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var list = await db.ShoppingLists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null) return Results.NotFound();

        var currentUserId = Guid.Parse(userIdClaim);
        var member = list.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // The logic: If you aren't the owner, you MUST be an accepted member with Editor role.
        bool isOwner = list.OwnerId == currentUserId;
        bool isValidMember = member != null && member.HasAccepted && member.Type >= MemberType.Viewer;

        if (!isOwner && !isValidMember)
        {
            return Results.Forbid();
        }


        var items = list.Items.Select(i => new ListItemDto
        {
            Id = i.Id,
            ListId = i.ListId,
            Name = i.Name,
            Quantity = i.Quantity,
            IsChecked = i.IsChecked,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        }).ToList();

        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while fetching the items: {ex.Message}");
    }
})
    .Produces<List<ListItemDto>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("Authenticated")
    .WithName("GetListItems")
    .WithOpenApi()
    ;

// UPDATE
listsGroup.MapPut("/{listId:guid}/items/{itemId}", async (Guid listId, Guid itemId, ListItemDto updatedItem, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var list = await db.ShoppingLists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null) return Results.NotFound();

        var currentUserId = Guid.Parse(userIdClaim);
        var member = list.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // The logic: If you aren't the owner, you MUST be an accepted member with Editor role.
        bool isOwner = list.OwnerId == currentUserId;
        bool isValidMember = member != null && member.HasAccepted && member.Type >= MemberType.Editor;

        if (!isOwner && !isValidMember)
        {
            return Results.Forbid();
        }


        var item = await db.ListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId);
        if (item == null)
            return Results.NotFound();

        item.Name = updatedItem.Name;
        item.Quantity = updatedItem.Quantity;
        item.IsChecked = updatedItem.IsChecked;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        await hubContext.Clients.Group(listId.ToString()).ListUpdated(listId.ToString());

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while updating the item: {ex.Message}");
    }
})
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("Authenticated")
    .WithName("UpdateListItem")
    .WithOpenApi()
    ;

// DELETE
listsGroup.MapDelete("/{listId:guid}/items/{itemId}", async (Guid listId, Guid itemId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var list = await db.ShoppingLists
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId);

        if (list == null) return Results.NotFound();

        var currentUserId = Guid.Parse(userIdClaim);
        var member = list.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // The logic: If you aren't the owner, you MUST be an accepted member with Editor role.
        bool isOwner = list.OwnerId == currentUserId;
        bool isValidMember = member != null && member.HasAccepted && member.Type >= MemberType.Editor;

        if (!isOwner && !isValidMember)
        {
            return Results.Forbid();
        }

        var item = await db.ListItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId);
        if (item == null)
            // This item was not found in the db
            return Results.NotFound();

        // Remove item from db
        db.ListItems.Remove(item);
        await db.SaveChangesAsync();

        // notify change
        await hubContext.Clients.Group(listId.ToString()).ListUpdated(listId.ToString());

        // Return 204 No Content
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while deleting the item: {ex.Message}");
    }
})
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("Authenticated")
    .WithName("DeleteListItem")
    .WithOpenApi()
    ;


// MEMBERSHIP ENDPOINTS (Invite, Accept Invite, Decline Invite, Leave List, Remove Member) with proper authorization and error handling
// CREATE - Invite Member
listsGroup.MapPost("/{listId:guid}/invite", async (Guid listId, string userName, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    try
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var isListOwner = await db.ShoppingLists.AnyAsync(l => l.Id == listId && l.OwnerId == Guid.Parse(userIdClaim));
        if (!isListOwner)
            return Results.Forbid();

        // Check if the user is already a member of the list
        var existingMember = await db.ListMembers.FirstOrDefaultAsync(m => m.ListId == listId && m.UserName == userName);
        if (existingMember != null)
        {
            if (existingMember.HasAccepted)
                return Results.Conflict(new { Message = "User is already a member of the list" });
            else
                return Results.Conflict(new { Message = "An invite has already been sent to this user" });
        }

        var userToInvite = await db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (userToInvite == null)
            return Results.Ok("Invite sent!"); // Don't reveal whether the user exists or not to prevent username enumeration attacks

        var member = new ListMember
        {
            Id = Guid.NewGuid(),
            ListId = listId,
            UserId = userToInvite.Id,
            UserName = userToInvite.UserName,
            Type = MemberType.Editor, // Default to Editor, can be changed later by the owner
            HasAccepted = false
        };

        db.ListMembers.Add(member);
        await db.SaveChangesAsync();

        await hubContext.Clients.User(userToInvite.Id.ToString().ToLower()).InviteReceived(listId.ToString());

        return Results.Ok("Invite sent!");
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while inviting the user: {ex.Message}");
    }
})
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status409Conflict)
    .RequireAuthorization("Authenticated")
    .WithName("InviteToList")
    .WithOpenApi()
    ;

// POST - Accept Invite
listsGroup.MapPost("/{listId:guid}/invite/accept", async (Guid listId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) => 
{
    var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim))
        return Results.Unauthorized();

    var member = await db.ListMembers.FirstOrDefaultAsync(m => m.ListId == listId && m.UserId == Guid.Parse(userIdClaim));
    if (member == null)
        return Results.NotFound(new { Message = "Invite not found" });

    if (member.HasAccepted)
        return Results.Conflict(new { Message = "Invite already accepted" });

    member.HasAccepted = true;

    var list = await db.ShoppingLists.Include(l => l.Members).FirstOrDefaultAsync(l => l.Id == listId);
    if (list != null)
    {
        // Notify the owner and other members that a new member has accepted the invite
        list.UpdatedAt = DateTimeOffset.UtcNow;
        var memberUserIds = list.Members.Where(m => m.HasAccepted).Select(m => m.UserId.ToString()).ToList();
        await hubContext.Clients.Group(listId.ToString()).InviteAccepted(listId.ToString(), member.UserName.ToString().ToLower());
    }
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Invite accepted" });
})
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status409Conflict)
    .RequireAuthorization("Authenticated")
    .WithName("AcceptListInvite")
    .WithOpenApi()
    ;

// DELETE - Decline Invite
listsGroup.MapDelete("/{listId:guid}/invite/decline", async (Guid listId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim))
        return Results.Unauthorized();

    var member = await db.ListMembers.FirstOrDefaultAsync(m => m.ListId == listId && m.UserId == Guid.Parse(userIdClaim));
    if (member == null)
        return Results.NotFound(new { Message = "Invite not found" });

    if (member.HasAccepted)
        return Results.Conflict(new { Message = "Invite already accepted, cannot decline" });

    db.ListMembers.Remove(member);

    var list = await db.ShoppingLists.Include(l => l.Members).FirstOrDefaultAsync(l => l.Id == listId);
    if (list != null)
    {
        // Notify the owner that the invite was declined
        var ownerUserId = list.OwnerId.ToString();
        await hubContext.Clients.Group(listId.ToString()).InviteRejected(listId.ToString(), member.UserName.ToString().ToLower());
    }

    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Invite declined" });
})
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status409Conflict)
    .RequireAuthorization("Authenticated")
    .WithName("DeclineListInvite")
    .WithOpenApi()
    ;

// DELETE
listsGroup.MapDelete("/{listId:guid}/leave", async (Guid listId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim))
        return Results.Unauthorized();

    var member = await db.ListMembers.FirstOrDefaultAsync(m => m.ListId == listId && m.UserId == Guid.Parse(userIdClaim));
    if (member == null)
        return Results.NotFound(new { Message = "Membership not found" });

    if (member.HasAccepted)
    {
        db.ListMembers.Remove(member);
        await db.SaveChangesAsync();
        await hubContext.Clients.Group(listId.ToString()).MemberLeftList(listId.ToString(), member.UserName.ToString().ToLower());
        return Results.Ok(new { Message = "You have left the list" });
    }

    return Results.Conflict(new { Message = "You cannot leave a list you haven't accepted an invite for." });
})
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status409Conflict)
    .RequireAuthorization("Authenticated")
    .WithName("LeaveList")
    .WithOpenApi()
    ;

// DELETE
listsGroup.MapDelete("/{listId:guid}/member/{userName}", async (Guid listId, string userName, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext) =>
{
    var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim))
        return Results.Unauthorized();

    var isListOwner = await db.ShoppingLists.AnyAsync(l => l.Id == listId && l.OwnerId == Guid.Parse(userIdClaim));
    if (!isListOwner)
        return Results.Forbid();

    var member = await db.ListMembers.FirstOrDefaultAsync(m => m.ListId == listId && m.UserName == userName);
    if (member == null)
        return Results.NotFound(new { Message = "Membership not found" });

    db.ListMembers.Remove(member);
    await db.SaveChangesAsync();

    var userId = db.Users.FirstOrDefault(u => u.UserName.ToLower() == member.UserName.ToLower())?.Id ?? member.UserId;

    await hubContext.Clients.User(userId.ToString().ToLower()).ListAccessRemoved(listId.ToString()); // Notify the removed member that their access has been revoked
    await hubContext.Clients.Group(listId.ToString()).ListUpdated(listId.ToString());

    return Results.Ok(new { Message = "Member removed from the list" });
})
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("Authenticated")
    .WithName("RemoveListMember")
    .WithOpenApi()
    ;

// GET - Get All Lists for the Authenticated User (both owned and shared) with proper authorization and error handling
listsGroup.MapGet("/my-lists", async (ClaimsPrincipal claims, AppDbContext db) =>
{
    var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim))
        return Results.Unauthorized();

    var lists = await db.ShoppingLists
        .Where(l => l.OwnerId == Guid.Parse(userIdClaim) || l.Members.Any(m => m.UserId == Guid.Parse(userIdClaim) && m.HasAccepted) && l.IsShared)
        .Select(l => new ListDto
        {
            Id = l.Id,
            Name = l.Name,
            OwnerId = l.OwnerId,
            OwnerName = db.Users.FirstOrDefault(u => u.Id == l.OwnerId)!.UserName ?? "User",
            IsShared = l.IsShared,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,
            Items = l.Items.Select(i => new ListItemDto
            {
                Id = i.Id,
                ListId = i.ListId,
                Name = i.Name,
                Quantity = i.Quantity,
                IsChecked = i.IsChecked,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList(),
            Members = l.Members.Select(m => new ListMemberDto
            {
                Id = m.Id,
                ListId = l.Id,
                UserId = m.UserId,
                UserName = m.UserName,
                Type = m.Type,
                HasAccepted = m.HasAccepted
            }).ToList()

        })
        .ToListAsync();

    return Results.Ok(lists);
})
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization("Authenticated")
    .WithName("GetMyShoppingLists")
    .WithOpenApi()
    ;

// GET - Get all Lists that the Authenticated User has been invited to but has NOT accepted yet with proper authorization and error handling
listsGroup.MapGet("/invites", async (ClaimsPrincipal claims, AppDbContext db) =>
{
    var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim))
        return Results.Unauthorized();

    var invites = await db.ListMembers
        .Where(m => m.UserId == Guid.Parse(userIdClaim) && !m.HasAccepted && db.ShoppingLists.Any(l => l.Id == m.ListId && l.IsShared))
        .Select(m => new ListInviteDto
        {
            ListId = m.ListId,
            ListName = m.List!.Name,
            OwnerUserName = db.Users.Where(u => u.Id == m.List.OwnerId).Select(u => u.UserName).FirstOrDefault() ?? "Unknown",
        })
        .ToListAsync();

    return Results.Ok(invites);
})
    .Produces<List<ListInviteDto>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization("Authenticated")
    .WithName("GetListInvites")
    .WithOpenApi()
    ;

// TEST ENDPOINT: Checks if Token Generation is the killer
app.MapGet("/test-token", (ITokenService tokenService) =>
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
})
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status500InternalServerError)
    .WithName("TestToken")
    .WithOpenApi()
    ;

var testGroup = app.MapGroup("/");

if (app.Environment.IsDevelopment())
{
    testGroup = app.MapGroup("/api");
}


// Try a connection. This endpoint will only respond 200OK if the user has a internet connection this should help the app know if the issue is the connectivity
testGroup.MapGet("/test-connection", () => Results.Ok(new { Message = "Connection is healthy!" }))
    .Produces(StatusCodes.Status200OK)
    .WithName("TestConnection")
    .WithOpenApi();

app.Run();