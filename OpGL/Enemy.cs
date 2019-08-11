﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Enemy : Sprite
    {
        public float XVel;
        public float YVel;
        public bool Pushable = true;
        public Rectangle Bounds;
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
            CheckBounds();
        }

        public void CheckBounds()
        {
            if (Bounds.Width > 0 && Bounds.Height > 0)
            {
                if (Right > Bounds.X + Bounds.Width)
                {
                    Right = Bounds.X + Bounds.Width;
                    XVel *= -1;
                }
                else if (X < Bounds.X)
                {
                    X = Bounds.X;
                    XVel *= -1;
                }
                else if (Bottom > Bounds.Y + Bounds.Height)
                {
                    Bottom = Bounds.Y + Bounds.Height;
                    YVel *= -1;
                }
                else if (Y < Bounds.Y)
                {
                    Y = Bounds.Y;
                    YVel *= -1;
                }
            }
        }

        public override void CollideX(float distance, Sprite collision)
        {
            if (XVel != 0 || Pushable)
            {
                base.CollideX(distance, collision);
                XVel *= -1;
            }
            //CheckBounds();
        }

        public override void CollideY(float distance, Sprite collision)
        {
            if (YVel != 0 || Pushable)
            {
                base.CollideY(distance, collision);
                YVel *= -1;
            }
            //CheckBounds();
        }
    }
}
