using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    public class Room
    {
        public SpriteCollection Objects = new SpriteCollection();
        public Script EnterScript;
        public Script ExitScript;

        public Room(SpriteCollection objects, Script enter, Script leave)
        {
            Objects = objects;
            EnterScript = enter;
            ExitScript = leave;
        }
    }
}
