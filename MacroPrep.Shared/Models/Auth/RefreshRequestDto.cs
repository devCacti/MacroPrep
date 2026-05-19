namespace MacroPrep.Shared.Models.Auth
{
    public record RefreshRequestDto
    {
        public Guid SessionID { get; set; }
    }
}
