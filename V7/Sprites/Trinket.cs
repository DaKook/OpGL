using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class Trinket : Sprite
    {
        public Script Script;
        private Game owner;
        public int ID = -1;

        public void SetID(int id)
        {
            if (owner.LevelTrinkets.ContainsKey(ID))
            {
                owner.LevelTrinkets[ID] -= 1;
                if (owner.LevelTrinkets[ID] == 0) owner.LevelTrinkets.Remove(ID);
            }
            ID = id;
            if (owner.LevelTrinkets.ContainsKey(ID))
                owner.LevelTrinkets[ID] += 1;
            else
                owner.LevelTrinkets.Add(ID, 1);
        }

        public Trinket(float x, float y, Texture texture, Animation animation, Script script, Game game, int id = -1) : base(x, y, texture, animation)
        {
            Script = script;
            ID = id;
            owner = game;
            if (game.CollectedTrinkets.Contains(ID))
                Visible = false;
            ColorModifier = AnimatedColor.Flashy;
        }

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("Script", new SpriteProperty("Script", () => Script?.Name, (t, g) => Script = g.ScriptFromName((string)t), "", SpriteProperty.Types.Script, "The script to run when the trinket is collected."));
                ret.Add("ID", new SpriteProperty("ID", () => ID, (t, g) => ID = (int)t, 0, SpriteProperty.Types.Int, "The ID of the trinket.", false));
                ret["Type"].GetValue = () => "Trinket";
                return ret;
            }
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (Visible)
            {
                if (owner.LoseTrinkets)
                    crewman.PendingTrinkets.Add(this);
                else
                    crewman.HeldTrinkets.Add(ID);
                owner.CollectedTrinkets.Add(ID);
                owner.ExecuteScript(Script, this, crewman, new Number[] { });
                Visible = false;
            }
        }
    }
}
