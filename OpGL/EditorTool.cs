using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    class EditorTool
    {
        public char Hotkey;
        public string Name;
        public string Description;

        public EditorTool(char hotkey, string name, string description)
        {
            Hotkey = hotkey;
            Name = name;
            Description = description;
        }
    }
}
