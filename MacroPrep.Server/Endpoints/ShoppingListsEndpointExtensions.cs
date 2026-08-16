using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Hubs;
using MacroPrep.Shared.Enums.ShoppingLists;
using MacroPrep.Shared.Models.ShoppingLists;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.Security.Claims;

namespace MacroPrep.Server.Endpoints
{
    public static class ShoppingListsEndpointExtensions
    {
        public static void MapShoppingListsEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/shopping-lists").WithTags("Shopping Lists");
            if (app.Environment.IsDevelopment())
                group = app.MapGroup("/api/shopping-lists").WithTags("Shopping Lists");

            // LIST ONLY ENDPOINTS
            group.MapPost("/", CreateList)
                .Produces<Guid>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .RequireAuthorization("Authenticated")
                .WithName("CreateShoppingList")
                .WithOpenApi(operation =>
                {
                    operation.Summary = "Creates a new shopping list.";
                    operation.Description = "Creates a new shopping list with the provided details. The authenticated user will be set as the owner of the list.";

                    operation.RequestBody.Description = "The shopping list details to create.";
                    operation.RequestBody.Required = true;
                    operation.RequestBody.Content["application/json"].Schema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = nameof(ListDto)
                        }
                    };
                    operation.Responses["201"].Description = "The shopping list was successfully created. The response contains the ID of the newly created list.";
                    operation.Responses["400"].Description = "The request was invalid, such as missing required fields or invalid data formats.";
                    operation.Responses["401"].Description = "The user is not authenticated or the NameIdentifier claim is missing.";
                    return operation;
                });

            group.MapGet("/{listId:guid}", GetList)
                .Produces<ListDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("GetShoppingList")
                .WithOpenApi(op => {
                    op.Summary = "Retrieves a specific shopping list.";
                    op.Description = "Fetches details for a shopping list if the user is the owner or an accepted member.";

                    op.Responses["200"].Description = "The shopping list was successfully retrieved.";
                    op.Responses["401"].Description = "The user is not authenticated or the NameIdentifier claim is missing.";
                    op.Responses["403"].Description = "The user does not have permission to view this list (must be owner or accepted member).";
                    op.Responses["404"].Description = "No shopping list was found with the provided ID.";

                    return op;
                });

            group.MapPut("/{listId:guid}", UpdateList)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("UpdateShoppingList")
                .WithOpenApi();

            group.MapDelete("/{listId:guid}", DeleteList)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("DeleteShoppingList")
                .WithOpenApi();

            // LIST ITEMS ENDPOINTS
            group.MapPost("/{listId:guid}/items", CreateListItem)
                .Produces<Guid>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("AddListItem")
                .WithOpenApi();

            group.MapGet("/{listId:guid}/items", GetListItem)
                .Produces<List<ListItemDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("GetListItems")
                .WithOpenApi();

            group.MapPut("/{listId:guid}/items/{itemId:guid}", UpdateListItem)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("UpdateListItem")
                .WithOpenApi();

            group.MapDelete("/{listId:guid}/items/{itemId:guid}", DeleteListItem)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("DeleteListItem")
                .WithOpenApi();

            // MEMBERSHIP ENDPOINTS
            group.MapPost("/{listId:guid}/invite", InviteMember)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("InviteMember")
                .WithOpenApi();

            group.MapPost("/{listId:guid}/invite/accept", AcceptInvite)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("AcceptInvite")
                .WithOpenApi();

            group.MapDelete("/{listId:guid}/invite/decline", DeclineInvite)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("DeclineInvite")
                .WithOpenApi();

            group.MapDelete("/{listId:guid}/leave", LeaveList)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("LeaveList")
                .WithOpenApi();

            group.MapDelete("/{listId:guid}/members/{userName}", RemoveUserFromList)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization("Authenticated")
                .WithName("RemoveListMember")
                .WithOpenApi();

            // OTHER ENDPOINTS
            group.MapGet("/my-lists", GetLists)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .RequireAuthorization("Authenticated")
                .WithName("GetMyShoppingLists")
                .WithOpenApi();

            group.MapGet("/invites", GetInvites)
                .Produces<List<ListInviteDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .RequireAuthorization("Authenticated")
                .WithName("GetListInvites")
                .WithOpenApi();

        }

        public static async Task<IResult> CreateList(ListDto list, AppDbContext db, ClaimsPrincipal claims)
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
        }

        public static async Task<IResult> GetList(Guid listId, AppDbContext db, ClaimsPrincipal claims)
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
        }

        public static async Task<IResult> UpdateList(Guid listId, ListDto updatedList, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }
        
        public static async Task<IResult> DeleteList(Guid listId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }

        public static async Task<IResult> CreateListItem(Guid listId, ListItemDto item, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }

        public static async Task<IResult> GetListItem(Guid listId, AppDbContext db, ClaimsPrincipal claims)
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
        }

        public static async Task<IResult> UpdateListItem(Guid listId, Guid itemId, ListItemDto updatedItem, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }

        public static async Task<IResult> DeleteListItem(Guid listId, Guid itemId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }

        public static async Task<IResult> InviteMember(Guid listId, string userName, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }

        public static async Task<IResult> AcceptInvite(Guid listId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }
        
        public static async Task<IResult> DeclineInvite(Guid listId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }

        public static async Task<IResult> LeaveList(Guid listId, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }

        public static async Task<IResult> RemoveUserFromList(Guid listId, string userName, AppDbContext db, ClaimsPrincipal claims, IHubContext<ShoppingHub, IShoppingHubClient> hubContext)
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
        }

        public static async Task<IResult> GetLists(ClaimsPrincipal claims, AppDbContext db)
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
        }

        public static async Task<IResult> GetInvites(ClaimsPrincipal claims, AppDbContext db)
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
        }
    }
}
