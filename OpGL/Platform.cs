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
        private float XVel;
        private float YVel;
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

        public override void CollideX(float distance)
        {
            base.CollideX(distance);
            XVel *= -1;
        }

        public override void CollideY(float distance)
        {
            base.CollideY(distance);
            YVel *= -1;
        }
    }
}
