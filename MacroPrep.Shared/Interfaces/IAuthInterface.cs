using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Interfaces
{
    public interface IAuthInterface
    {
        string UserName { get; set; }

        string Password { get; set; }
    }
}
