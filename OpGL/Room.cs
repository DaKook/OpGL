using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    public class Room
    {
        public SpriteCollection Objects = new SpriteCollection();
        public Script EnterScript;
        public Script ExitScript;

        public int X;
        public int Y;

        public const int ROOM_WIDTH = 320;
        public const int ROOM_HEIGHT = 240;

        public Room(SpriteCollection objects, Script enter, Script leave)
        {
            Objects = objects;
            EnterScript = enter;
            ExitScript = leave;
        }

        public JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("EnterScript", EnterScript.Name);
            ret.Add("ExitScript", ExitScript.Name);
            ret.Add("X", X);
            ret.Add("Y", Y);
            JObject[] objs = new JObject[Objects.Count];
            for (int i = 0; i < Objects.Count; i++)
            {
                objs[i] = Objects[i].Save();
            }
            ret.Add("Objects", new JArray(objs));
            return ret;
        }
    }
}
