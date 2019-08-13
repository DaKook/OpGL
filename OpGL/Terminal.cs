using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Terminal : Sprite
    {
        public static VTextBox TextBox;
        public Animation DeactivatedAnimation;
        public Animation ActivatedAnimation;
        public Script Script;
        public bool Repeat;
        public bool AlreadyUsed;
        public Terminal(float x, float y, Texture texture, Animation deactivated, Animation activated, Script script, bool repeat) : base(x, y, texture, deactivated)
        {
            DeactivatedAnimation = deactivated;
            ActivatedAnimation = activated;
            Script = script;
            Repeat = repeat;
            AlreadyUsed = false;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (Animation == DeactivatedAnimation)
            {
                Animation = ActivatedAnimation;
                //Play terminal sound
            }
            if (crewman.CurrentTerminal == null || (crewman.CurrentTerminal != this && (Math.Abs(crewman.CurrentTerminal.CenterX - crewman.CenterX) > Math.Abs(CenterX - crewman.CenterX))))
            {
                crewman.CurrentTerminal = this;
                TextBox.Appear();
            }
        }

        public override JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            ret.Add("Deactivated", DeactivatedAnimation.Name);
            ret.Add("Activated", ActivatedAnimation.Name);
            ret.Add("Script", Script.Name);
            ret.Add("Repeat", Repeat);
            ret.Add("FlipX", flipX);
            ret.Add("FlipY", flipY);
            return ret;
        }
    }
}
