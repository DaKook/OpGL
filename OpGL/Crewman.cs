﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Crewman : Drawable
    {
        private Animation defaultAnimation;
        private Animation standingAnimation;
        private Animation walkingAnimation;
        private Animation fallingAnimation;
        private Animation dyingAnimation;
        public float YVelocity;
        public float MaxSpeed = 5f;
        public float Acceleration = 0.5f;
        public bool OnGround = false;
        public int InputDirection;
        public bool CanFlip = true;
        public override bool IsCrewman { get => true; }
        public Animation WalkingAnimation { get => walkingAnimation ?? defaultAnimation; set => walkingAnimation = value; }
        public Animation StandingAnimation { get => standingAnimation ?? defaultAnimation; set => standingAnimation = value; }
        public Animation FallingAnimation { get => fallingAnimation ?? defaultAnimation; set => fallingAnimation = value; }
        public Animation DyingAnimation { get => dyingAnimation ?? defaultAnimation; set => dyingAnimation = value; }
        public float XVelocity;

        public Crewman(float x, float y, Texture texture, string name = "", Animation stand = null, Animation walk = null, Animation fall = null, Animation die = null) : base(x, y, texture, stand)
        {
            Name = name;
            StandingAnimation = stand;
            WalkingAnimation = walk;
            FallingAnimation = fall;
            DyingAnimation = die;
            defaultAnimation = StandingAnimation ?? new Animation(new System.Drawing.Point[] { new System.Drawing.Point(0, 0) }, System.Drawing.Rectangle.Empty, texture);
            Gravity = 0.375f;
        }

        public override void Process()
        {
            base.Process();
            YVelocity += Gravity;
            if (OnGround)
            {
                if (XVelocity != 0 && Animation != WalkingAnimation)
                {
                    ResetAnimation();
                    Animation = WalkingAnimation;
                }
                else if (XVelocity == 0 && Animation != StandingAnimation)
                {
                    ResetAnimation();
                    Animation = StandingAnimation;
                }
            }
            else
            {
                onPlatform = null;
            }
            OnGround = false;
            XVelocity += Math.Sign(InputDirection) * Acceleration;
            if (XVelocity > MaxSpeed)
                XVelocity = MaxSpeed;
            else if (XVelocity < -MaxSpeed)
                XVelocity = -MaxSpeed;
            if (InputDirection == 0)
            {
                int s = Math.Sign(XVelocity);
                XVelocity -= s * Acceleration;
                if (Math.Sign(XVelocity) != s)
                    XVelocity = 0;
            }
            if ((flipX && XVelocity > 0) || (!flipX && XVelocity < 0))
            {
                flipX = !flipX;
            }
            if (Gravity >= 0 == flipY) flipY = !flipY;
            X += XVelocity;
            Y += YVelocity;
            if (onPlatform != null)
            {
                X += onPlatform.XVel;
                Y += onPlatform.YVel;
            }
        }

        public virtual void Die()
        {
            Animation = DyingAnimation;
            ResetAnimation();
        }

        public override void CollideY(float distance, Drawable collision)
        {
            base.CollideY(distance, collision);
            if (Math.Sign(distance) == Math.Sign(Gravity))
            {
                YVelocity = 0;
                OnGround = true;
                if (collision as Platform != null && onPlatform != collision) onPlatform = collision as Platform;
                else if (collision as Platform == null) onPlatform = null;
            }
        }

        public override void CollideX(float distance, Drawable collision)
        {
            base.CollideX(distance, collision);
            XVelocity = 0;
        }
    }
}
