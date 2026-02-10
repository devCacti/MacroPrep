using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Models.ShoppingLists
{
    public class ListItemDto
    {
        public Guid Id { get; set; }
        public Guid ListId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        public bool IsChecked { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public bool IsSynced { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // WIll be used for concurrency control (ETag) and to determine if the item has been updated since it was last fetched
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
