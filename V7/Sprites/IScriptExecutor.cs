using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public interface IScriptExecutor
    {
        bool Activated { get; set; }
        Script Script { get; set; }
    }
}
