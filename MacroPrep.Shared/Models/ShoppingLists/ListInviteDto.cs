namespace MacroPrep.Shared.Models.ShoppingLists
{
    public class ListInviteDto
    {
        public Guid ListId { get; set; }
        public string ListName { get; set; } = string.Empty;
        public string OwnerUserName { get; set; } = string.Empty;
    }
}
