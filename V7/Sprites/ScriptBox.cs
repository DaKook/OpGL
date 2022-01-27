using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace V7
{
    public class ScriptBox : BoxSprite, IScriptExecutor
    {
        public override bool AlwaysCollide => true;
        public Script Script { get; set; }
        Game game;
        public bool Activated { get; set; }
        public ScriptBox(float x, float y, Texture texture, int widthTiles, int heightTiles, Script script, Game owner) : base(x, y, texture, widthTiles, heightTiles)
        {
            Script = script;
            Solid = SolidState.Entity;
            Immovable = true;
            Static = true;
            Visible = false;
            Activated = false;
            game = owner;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (!Activated)
            {
                Activated = true;
                game.ExecuteScript(Script, this, crewman, new Number[] { });
            }
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "ScriptBox");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Width", WidthTiles);
        //    ret.Add("Height", HeightTiles);
        //    ret.Add("Script", Script.Name);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("Width", new SpriteProperty("Width", () => WidthTiles, (t, g) => SetWidth((int)t), 1, SpriteProperty.Types.Int, "The width intiles of the script box."));
                ret.Add("Height", new SpriteProperty("Height", () => HeightTiles, (t, g) => SetHeight((int)t), 1, SpriteProperty.Types.Int, "The height intiles of the script box."));
                ret.Add("Script", new SpriteProperty("Script", () => Script.Name, (t, g) => Script = g.ScriptFromName((string)t), "", SpriteProperty.Types.Script, "The script executed by this script box."));
                ret["Type"].GetValue = () => "ScriptBox";
                return ret;
            }
        }
    }
}
