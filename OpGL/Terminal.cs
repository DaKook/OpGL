using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Terminal : Drawable
    {
        public Animation DeactivatedAnimation;
        public Animation ActivatedAnimation;
        public Script Script;
        public bool Repeat;
        public Terminal(float x, float y, Texture texture, Animation deactivated, Animation activated, Script script, bool repeat) : base(x, y, texture, deactivated)
        {
            DeactivatedAnimation = deactivated;
            ActivatedAnimation = activated;
            Script = script;
            Repeat = repeat;
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
            }
        }
    }
}
