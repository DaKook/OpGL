using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public interface IScriptExecutor
    {
        bool Activated { get; set; }
        Script Script { get; set; }
    }
}
