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
        public float MaxSpeed = 8;
        public float Acceleration = 1;
        public bool OnGround = false;
        public int InputDirection;
        public override bool IsCrewman { get => true; }
        public Crewman(float x, float y, Texture texture, string name = "", Animation stand = null, Animation walk = null, Animation fall = null, Animation die = null) : base(x, y, texture, stand)
        {
            Name = name;
            StandingAnimation = stand;
            WalkingAnimation = walk;
            FallingAnimation = fall;
            DyingAnimation = die;
        }

        public override void Process()
        {
            base.Process();
            YVelocity -= Gravity;
            OnGround = false;
            XVelocity += Math.Sign(InputDirection) * Acceleration;
            if (XVelocity > MaxSpeed)
                XVelocity = MaxSpeed;
            else if (XVelocity < -MaxSpeed)
                XVelocity = -MaxSpeed;
        }

        public virtual void Die()
        {
            Animation = DyingAnimation;
            animFrame = 0;
        }

        public override void CollideY(float distance)
        {
            base.CollideY(distance);
            YVelocity = 0;
            OnGround = true;
        }

        public override void CollideX(float distance)
        {
            base.CollideX(distance);
            XVelocity = 0;
        }
    }
}
