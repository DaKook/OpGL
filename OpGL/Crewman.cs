using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Crewman : Drawable
    {
        public Animation StandingAnimation;
        public Animation WalkingAnimation;
        public Animation FallingAnimation;
        public Animation DyingAnimation;
        public float XVelocity;
        public float YVelocity;
        public float MaxSpeed;
        public float Acceleration;
        private int inputDirection;
        public Crewman(float x, float y, Texture texture, Animation stand = null, Animation walk = null, Animation fall = null, Animation die = null)
        {
            X = x;
            Y = y;
            Texture = texture;
            StandingAnimation = stand;
            WalkingAnimation = walk;
            FallingAnimation = fall;
            DyingAnimation = die;
        }

        public override void Process()
        {
            base.Process();
            YVelocity -= Gravity;
            XVelocity += Math.Sign(inputDirection) * Acceleration;
        }
    }
}
