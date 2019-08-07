using System;
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
        private Animation jumpingAnimation;
        private Animation dyingAnimation;
        public float YVelocity;
        public static float TerminalVelocity = 5f;
        public float MaxSpeed = 3f;
        public float Acceleration = 0.375f;
        public bool OnGround = false;
        public int InputDirection;
        public bool CanFlip = true;
        public float Jump = 1.6875f;
        public float CheckpointX;
        public float CheckpointY;
        public bool CheckpointFlipX;
        public bool CheckpointFlipY;
        public override bool IsCrewman { get => true; }
        public Animation WalkingAnimation { get => walkingAnimation ?? defaultAnimation; set => walkingAnimation = value; }
        public Animation StandingAnimation { get => standingAnimation ?? defaultAnimation; set => standingAnimation = value; }
        public Animation FallingAnimation { get => fallingAnimation ?? defaultAnimation; set => fallingAnimation = value; }
        public Animation JumpingAnimation { get => jumpingAnimation ?? defaultAnimation; set => jumpingAnimation = value; }
        public Animation DyingAnimation { get => dyingAnimation ?? defaultAnimation; set => dyingAnimation = value; }
        public int DyingFrames;
        public float XVelocity;

        public Crewman(float x, float y, Texture texture, string name = "", Animation stand = null, Animation walk = null, Animation fall = null, Animation jump = null, Animation die = null) : base(x, y, texture, stand)
        {
            Name = name;
            StandingAnimation = stand;
            WalkingAnimation = walk;
            FallingAnimation = fall;
            JumpingAnimation = jump;
            DyingAnimation = die;
            defaultAnimation = StandingAnimation ?? new Animation(new System.Drawing.Point[] { new System.Drawing.Point(0, 0) }, System.Drawing.Rectangle.Empty, texture);
            Gravity = 0.6875f;
        }

        public override void Process()
        {
            base.Process();
            if (DyingFrames == 0)
            {
                YVelocity += Gravity;
                if (YVelocity > TerminalVelocity) YVelocity = TerminalVelocity;
                else if (YVelocity < -TerminalVelocity) YVelocity = -TerminalVelocity;
                if (OnGround)
                {
                    changeAnimationOnGround();
                }
                else
                {
                    if (onPlatform != null)
                    {
                        onPlatform.OnTop.Remove(this);
                        onPlatform = null;
                    }

                    changeAnimationInAir();
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
                    X += onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                    Y += onPlatform.YVel;
                }
            }
            else
            {
                DyingFrames -= 1;
                if (DyingFrames == 0)
                {
                    X = CheckpointX;
                    Y = CheckpointY;
                    XVelocity = 0;
                    YVelocity = 0;
                    ResetAnimation();
                    Animation = StandingAnimation;
                    flipX = CheckpointFlipX;
                    if (Math.Sign(Gravity) == 1 == CheckpointFlipY)
                    {
                        Gravity *= -1;
                    }
                }
            }
        }

        private void changeAnimationOnGround()
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

        private void changeAnimationInAir()
        {
            if (Math.Sign(YVelocity) == Math.Sign(Gravity) && Animation != FallingAnimation)
            {
                ResetAnimation();
                Animation = FallingAnimation;
            }
            else if (Math.Sign(YVelocity) == -Math.Sign(Gravity) && Animation != JumpingAnimation)
            {
                Animation = JumpingAnimation;
            }
        }

        public virtual void Die()
        {
            Animation = DyingAnimation;
            ResetAnimation();
            DyingFrames = 60;
        }

        public override void CollideY(float distance, Drawable collision)
        {
            base.CollideY(distance, collision);
            //Check if landing on ground
            if (Math.Sign(distance) == Math.Sign(Gravity))
            {
                YVelocity = 0;
                OnGround = true;
                changeAnimationOnGround();
                //Check if landing on a platform
                if (collision as Platform != null && onPlatform != collision)
                {
                    onPlatform = collision as Platform;
                    onPlatform.OnTop.Add(this);
                }
                else if (onPlatform != null && collision as Platform == null)
                {
                    onPlatform.OnTop.Remove(this);
                    onPlatform = null;
                }
            }
            else if (Math.Sign(distance) == Math.Sign(YVelocity))
            {
                YVelocity = 0;
            }
        }

        public override void CollideX(float distance, Drawable collision)
        {
            base.CollideX(distance, collision);
            if (distance > 0 == XVelocity > 0)
                XVelocity = 0;
        }

        public void FlipOrJump()
        {
            OnGround = false;
            if (onPlatform != null)
            {
                onPlatform.OnTop.Remove(this);
                onPlatform = null;
            }
            if (CanFlip)
            {
                Gravity *= -1;
            }
            YVelocity = -Jump * Math.Sign(Gravity);
            changeAnimationInAir();
        }

        public override CollisionData TestCollision(Drawable testFor)
        {
            if (DyingFrames > 0) return null;
            if (testFor.KillCrewmen && testFor.Solid == SolidState.Entity && IsOverlapping(testFor))
            {
                return new CollisionData(true, 0, testFor);
            }
            return base.TestCollision(testFor);
        }

        public override void Collide(CollisionData cd)
        {
            if (cd.CollidedWith.KillCrewmen)
                Die();
            else
                base.Collide(cd);
        }

        public override CollisionData GetFirstCollision(List<CollisionData> data)
        {
            return base.GetFirstCollision(data);
        }
    }
}
