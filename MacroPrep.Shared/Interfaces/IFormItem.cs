using System.ComponentModel.DataAnnotations;

namespace MacroPrep.Shared.Interfaces
{
    public interface IFormItem
    {
        int Order { get; set; }
        bool IsEmpty();
    }
}
