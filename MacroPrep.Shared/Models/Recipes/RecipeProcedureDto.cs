using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Models.Recipes
{
    public class RecipeProcedureDto
    {
        public Guid? Id { get; set; }

        public Guid ProcedureId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Details { get; set; }

        [Required]
        public int Order { get; set; }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Title);
        }
    }
}
