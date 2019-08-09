using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Enemy : Drawable
    {
        public float XVel;
        public float YVel;
        public bool Pushable = true;
        public Enemy(float x, float y, Texture texture, Animation animation, float xSpeed, float ySpeed, Color? color = null) : base(x, y, texture, animation)
        {
            XVel = xSpeed;
            YVel = ySpeed;
            KillCrewmen = true;
            Solid = SolidState.Entity;
            Color = color ?? Color.White;
        }

        public override void Process()
        {
            base.Process();
            X += XVel;
            Y += YVel;
        }

        public override void CollideX(float distance, Drawable collision)
        {
            if (XVel != 0 || Pushable)
            {
                base.CollideX(distance, collision);
                XVel *= -1;
            }
            //else if (!collision.Static && collision.Solid == SolidState.Ground)
            //{
            //    collision.X += distance;
            //    if (collision is Platform)
            //    {
            //        (collision as Platform).XVel *= -1;
            //    }
            //}
        }

        public override void CollideY(float distance, Drawable collision)
        {
            if (YVel != 0 || Pushable)
            {
                base.CollideY(distance, collision);
                YVel *= -1;
            }
            //else if (!collision.Static && collision.Solid == SolidState.Ground)
            //{
            //    collision.Y += distance;
            //    if (collision is Platform)
            //    {
            //        (collision as Platform).YVel *= -1;
            //    }
            //}
        }
    }
}
