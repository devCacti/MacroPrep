using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace MacroPrep.Server.Data.Entities
{
    public class Procedure
    {
        [Key, Required]
        public Guid Id { get; set; }

        [Required]
        public int StepNumber { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Details { get; set; }

        public int? TimerSeconds { get; set; }

        public int? ProcedureTime { get; set; }
        
        public virtual ICollection<Instrument>? Instrument { get; set; }
        public virtual ICollection<Ingredient>? Ingredients { get; set; }

        [Required]
        public virtual Recipe Recipe { get; set; }

        public Procedure(Recipe recipe, string title, int step, string? details = null, int? timerSeconds = null, int? procedureTime = null)
        {
            Id = Guid.NewGuid();
            Title = title;
            StepNumber = step;
            Details = details;
            Recipe = recipe;
            TimerSeconds = timerSeconds;
            ProcedureTime = procedureTime ?? timerSeconds;
        }
    }
}
