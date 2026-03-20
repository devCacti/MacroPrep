using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Server.Data.Entities
{
    public class ShoppingList
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid OwnerId { get; set; }

        public string Name { get; set; } = string.Empty;
        public bool IsShared { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;


        public List<ListItem> Items { get; set; } = new();
        public List<ListMember> Members { get; set; } = new();
    }
}
