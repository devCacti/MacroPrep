using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MacroPrep.Shared.Enums.ShoppingLists;

namespace MacroPrep.Server.Data.Entities
{
    public class ListMembers
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ListId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public MemberType Type { get; set; } = MemberType.Viewer;

        public bool HasAccepted { get; set; } = false;

        [ForeignKey(nameof(ListId))]
        public virtual ShoppingList? List { get; set; }
    }
}
