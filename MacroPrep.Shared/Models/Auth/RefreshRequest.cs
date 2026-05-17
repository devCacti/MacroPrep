namespace MacroPrep.Shared.Models.Auth
{
    public record RefreshRequest
    {
        public Guid SessionID { get; set; }
    }
}
