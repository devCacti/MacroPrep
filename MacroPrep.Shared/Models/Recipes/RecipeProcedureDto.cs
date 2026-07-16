using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Models.Recipes
{
    public class RecipeProcedureDto
    {
        public Guid? Id { get; set; }

        public Guid ProcedureId { get; set; }

        [Required]
        public string MainInstruction { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public required int Order { get; set; }

        public bool IsEmpty()
        {
            return ProcedureId == Guid.Empty
                && string.IsNullOrEmpty(MainInstruction);
        }
    }
}
