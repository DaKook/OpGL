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

        public JToken Save()
        {
            JsonSerializer js = new JsonSerializer();
            
            JTokenWriter ret = new JTokenWriter();
            ret.WritePropertyName("Objects");
            ret.WriteStartArray();
            for (int i = 0; i < Objects.Count; i++)
            {
                
            }
            return ret.Token;
        }
    }
}
