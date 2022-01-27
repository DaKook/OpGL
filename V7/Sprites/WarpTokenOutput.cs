using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using Newtonsoft.Json.Linq;

namespace V7
{
    public class WarpTokenOutput : Sprite
    {
        public WarpToken.WarpData Parent;
        public int RoomX;
        public int RoomY;
        
        public WarpTokenOutput(float x, float y, Texture texture, Animation animation, WarpToken.WarpData data) : base(x, y, texture, animation)
        {
            Color = Color.FromArgb(127, 255, 255, 255);
            Visible = false;
            Solid = SolidState.NonSolid;
            Static = true;
            Parent = data;
            RoomX = data.OutRoom.X;
            RoomY = data.OutRoom.Y;
        }

        public override JObject Save(Game game, bool isUniversal = false)
        {
            return null;
        }

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("RoomX", new SpriteProperty("RoomX", () => RoomX, (t, g) => RoomX = (int)t, 0, SpriteProperty.Types.Int, "", false));
                ret.Add("RoomY", new SpriteProperty("RoomY", () => RoomY, (t, g) => RoomY = (int)t, 0, SpriteProperty.Types.Int, "", false));
                ret["Type"].GetValue = () => "WarpOutput";
                return ret;
            }
        }
    }
}
