using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    class Timer : RectangleSprite, IScriptExecutor
    {
        public Script Script { get; set; }
        public int Interval;
        public Sprite Target;
        public Game Owner;
        private int frameCount;
        public bool Activated { get; set; }

        public Timer(Script script, int interval, Sprite target, Game game) : base(0, 0, 0, 0)
        {
            Script = script;
            Interval = interval;
            Target = target;
            Owner = game;
            Immovable = true;
            Solid = SolidState.NonSolid;
        }


        public override void Process()
        {
            if (!Activated)
            {
                frameCount += 1;
                if (frameCount >= Interval)
                {
                    Owner.ExecuteScript(Script, this, Target);
                    frameCount = 0;
                }
            }
        }
    }
}
