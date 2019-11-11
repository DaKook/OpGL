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
            EnterScript = enter ?? Script.Empty;
            ExitScript = leave ?? Script.Empty;
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
                Objects[i].X -= X * ROOM_WIDTH;
                Objects[i].Y -= Y * ROOM_HEIGHT;
                objs[i] = Objects[i].Save();
                Objects[i].X += X * ROOM_WIDTH;
                Objects[i].Y += Y * ROOM_HEIGHT;
            }
            ret.Add("Objects", new JArray(objs));
            return ret;
        }
        public static Room LoadRoom(JToken loadFrom, Game game, int xOffset = 0, int yOffset = 0)
        {
            JArray sArr = loadFrom["Objects"] as JArray;
            Room ret = new Room(new SpriteCollection(), null, null);
            ret.X = (int)loadFrom["X"];
            ret.Y = (int)loadFrom["Y"];
            foreach (JToken sprite in sArr)
            {
                Sprite s = Sprite.LoadSprite(sprite, game);
                s.X += ret.X * ROOM_WIDTH + xOffset;
                s.Y += ret.Y * ROOM_HEIGHT + yOffset;
                s.PreviousX = s.X;
                s.PreviousY = s.Y;
                if (s != null)
                    ret.Objects.Add(s);
                if (s is Checkpoint && game.ActivePlayer.CurrentCheckpoint != null && s.X == game.ActivePlayer.CurrentCheckpoint.X && s.Y == game.ActivePlayer.CurrentCheckpoint.Y)
                {
                    (s as Checkpoint).Activate(false);
                    game.ActivePlayer.CurrentCheckpoint = s as Checkpoint;
                }
            }
            ret.EnterScript = game.ScriptFromName((string)loadFrom["EnterScript"]) ?? Script.Empty;
            ret.ExitScript = game.ScriptFromName((string)loadFrom["ExitScript"]) ?? Script.Empty;
            return ret;
        }
    }
}
