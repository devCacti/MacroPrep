using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MacroPrep.Server.Data.Entities
{
    public class ListItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ListId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;
        public bool IsChecked { get; set; } = false;
        public string Description { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(ListId))]
        public virtual ShoppingList? List { get; set; }
    }
}
