using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Trinket : Sprite
    {
        public Script Script;
        private Game owner;
        private int _id;
        public int ID
        {
            get => _id;
            set
            {
                if (owner.LevelTrinkets.ContainsKey(_id))
                {
                    owner.LevelTrinkets[_id] -= 1;
                    if (owner.LevelTrinkets[_id] == 0) owner.LevelTrinkets.Remove(_id);
                }
                _id = value;
                if (owner.LevelTrinkets.ContainsKey(_id))
                    owner.LevelTrinkets[_id] += 1;
                else
                    owner.LevelTrinkets.Add(_id, 1);
            }
        }
        public Trinket(float x, float y, Texture texture, Animation animation, Script script, Game game, int id) : base(x, y, texture, animation)
        {
            Script = script;
            owner = game;
            _id = id;
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
                owner.CollectedTrinkets.Add(_id);
                owner.ExecuteScript(Script, this, crewman);
                Visible = false;
            }
        }
    }
}
