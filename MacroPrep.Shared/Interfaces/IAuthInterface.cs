using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPrep.Shared.Interfaces
{
    public interface IAuthInterface
    {
        public string UserName { get; set; }

        public string Password { get; set; }
    }
}
