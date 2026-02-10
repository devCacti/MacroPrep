using System.ComponentModel.DataAnnotations;
using MacroPrep.Shared.Enums.ShoppingLists;

namespace MacroPrep.Shared.Models.ShoppingLists
{
    public class ListDto
    {
        public Guid Id { get; set; }

        public Guid OwnerId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string OwnerName { get; set; } = "Unknown User";

        public string? Description { get; set; }

        public ListStatus Status { get; set; } = ListStatus.Active;


        // Shared with...
        public bool IsShared { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public bool IsSynced { get; set; } = true;

        public byte[]? RowVersion { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // This property is not stored in the database but is used to hold the items when fetching a list with its items included
        // Easier for when coding to avoid needing to fetch the items separately and then combine them into a single object
        public ICollection<ListItemDto> Items { get; set; } = new List<ListItemDto>();
        public ICollection<ListMemberDto> Members { get; set; } = new List<ListMemberDto>();
    }
}
