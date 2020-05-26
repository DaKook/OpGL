using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public interface IActivityZone : IScriptExecutor
    {
        Sprite Sprite { get; set; }
        VTextBox TextBox { get; set; }
    }
}
