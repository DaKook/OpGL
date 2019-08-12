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

        public override JToken Save()
        {
            JTokenWriter ret = new JTokenWriter();
            write("X", X, ret);
            write("Y", Y, ret);
            write("Texture", Texture.Name, ret);
            write("Deactivated", DeactivatedAnimation.Name, ret);
            write("Activated", ActivatedAnimation.Name, ret);
            write("Script", Script.Name, ret);
            write("Repeat", Repeat, ret);
            write("FlipX", flipX, ret);
            write("FlipY", flipY, ret);
            return ret.Token;
        }
    }
}
