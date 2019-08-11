using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Platform : Sprite
    {
        public float XSpeed;
        public float YSpeed;
        public float XVel;
        public float YVel;
        public float Conveyor;
        public bool CanDisappear;
        public Animation DisappearAnimation;
        private int DisappearFrames = -1;
        /// <summary>
        /// Determines whether or not the platform pushes crewmen the opposite direction on the bottom. Set to true to disable this.
        /// </summary>
        public bool SingleDirection;
        public List<Sprite> OnTop = new List<Sprite>();
        public Platform(float x, float y, Texture texture, Animation animation, float xSpeed = 0, float ySpeed = 0, float conveyor = 0, bool disappear = false, Animation disappearAnimation = null) : base(x, y, texture, animation)
        {
            XSpeed = xSpeed;
            YSpeed = ySpeed;
            XVel = XSpeed;
            YVel = YSpeed;
            Conveyor = conveyor;
            CanDisappear = disappear;
            DisappearAnimation = disappearAnimation;
            Solid = SolidState.Ground;
        }

        public override void Process()
        {
            //Do not process if disappeared
            if (DisappearFrames == 0) return;
            base.Process();
            X += XVel;
            Y += YVel;
            if (DisappearFrames > 0)
            {
                DisappearFrames -= 1;
                if (DisappearFrames == 0)
                {
                    Visible = false;
                    Solid = SolidState.NonSolid;
                }
            }
        }

        public override CollisionData TestCollision(Sprite testFor)
        {
            if (testFor == this) return null;

            // Platforms colliding with an entity should cause the entity to do a collision check. This collision should not be used as such. Thus, NaN.
            CollisionData ret = null;
            if (testFor.Solid == SolidState.Entity && IsOverlapping(testFor))
            {
                ret = GetCollisionData(testFor);
                if (ret != null)
                    ret.Distance = float.NaN;
            }
            else
                ret = base.TestCollision(testFor);

            return ret;
        }

        public override void CollideX(float distance, Sprite collision)
        {
            if (XVel != 0)
            {
                base.CollideX(distance, collision);
                foreach (Sprite d in OnTop)
                {
                    d.X -= distance;
                }
                XVel *= -1;
            }
            else if (collision != null && !collision.Static && collision.Solid == SolidState.Ground)
                collision.X += distance;

            if (collision is Platform)
                collision.CollideX(-distance, null);
        }

        public override void CollideY(float distance, Sprite collision)
        {
            if (YVel != 0)
            {
                base.CollideY(distance, collision);
                foreach (Sprite d in OnTop)
                {
                    if (!d.Static)
                        d.Y -= distance;
                }
                YVel *= -1;
            }
            else if (!collision.Static && collision.Solid == SolidState.Ground)
                collision.Y += distance;

            if (collision is Platform)
                (collision as Platform).YVel *= -1;
        }

        public void Disappear()
        {
            if (!CanDisappear) return;
            if (DisappearAnimation != null)
            {
                ResetAnimation();
                Animation = DisappearAnimation;
                DisappearFrames = DisappearAnimation.FrameCount;
            }
        }
    }
}
