using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Platform : Drawable
    {
        public float XSpeed;
        public float YSpeed;
        public float XVel;
        public float YVel;
        public bool Disappear;
        public Platform(float x, float y, Texture texture, Animation animation, float xSpeed = 0, float ySpeed = 0, bool disappear = false) : base(x, y, texture, animation)
        {
            XSpeed = xSpeed;
            YSpeed = ySpeed;
            XVel = XSpeed;
            YVel = YSpeed;
            Disappear = disappear;
            Solid = SolidState.Ground;
        }

        public override void Process()
        {
            base.Process();
            X += XVel;
            Y += YVel;
        }

        public override void CollideX(float distance, Drawable collision)
        {
            base.CollideX(distance, collision);
            XVel *= -1;
        }

        public override void CollideY(float distance, Drawable collision)
        {
            base.CollideY(distance, collision);
            YVel *= -1;
        }
    }
}
