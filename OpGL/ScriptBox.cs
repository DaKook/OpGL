using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    public class ScriptBox : BoxSprite
    {
        Script Script;
        bool Activated;
        public ScriptBox(float x, float y, Texture texture, int widthTiles, int heightTiles, Script script) : base(x, y, texture, widthTiles, heightTiles)
        {
            Script = script;
            Solid = SolidState.Entity;
            Immovable = true;
            Static = true;
            Visible = false;
            Activated = false;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (!Activated)
            {
                Script.ExecuteFromBeginning();
                Activated = true;
            }
        }

        public override JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "ScriptBox");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            ret.Add("Width", WidthTiles);
            ret.Add("Height", HeightTiles);
            ret.Add("Script", Script.Name);
            return ret;
        }
    }
}
