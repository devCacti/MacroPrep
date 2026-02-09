
using MacroPrep.Shared.Enums.ShoppingLists;

namespace MacroPrep.Shared.Models.ShoppingLists
{
    public class ListMemberDto
    {
        public Guid Id { get; set; }
        public Guid ListId { get; set; }
        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;
        public MemberType Type { get; set; } = MemberType.Viewer;

        public bool HasAccepted { get; set; } = false;

        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
