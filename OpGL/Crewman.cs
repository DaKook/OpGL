using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Crewman : Sprite
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
        public Checkpoint CurrentCheckpoint;
        public int JumpBuffer = 0;
        public int LedgeMercy = 0;
        private bool _sad = false;
        public AIStates AIState = AIStates.Stand;
        public enum AIStates { Follow, Face, Stand };
        public Crewman Target;
        public string Tag;
        public bool Sad
        {
            get => _sad;
            set
            {
                if (value != _sad)
                {
                    _sad = value;
                    if (value)
                        animationOffset.Y += 1;
                    else
                        animationOffset.Y -= 1;
                }
            }
        }
        public Color TextBoxColor;
        //Squeak sound
        public Terminal CurrentTerminal = null;
        public override bool IsCrewman { get => true; }
        public Animation WalkingAnimation { get => walkingAnimation ?? defaultAnimation; set => walkingAnimation = value; }
        public Animation StandingAnimation { get => standingAnimation ?? defaultAnimation; set => standingAnimation = value; }
        public Animation FallingAnimation { get => fallingAnimation ?? defaultAnimation; set => fallingAnimation = value; }
        public Animation JumpingAnimation { get => jumpingAnimation ?? defaultAnimation; set => jumpingAnimation = value; }
        public Animation DyingAnimation { get => dyingAnimation ?? defaultAnimation; set => dyingAnimation = value; }
        public int DyingFrames;
        public float XVelocity;

        public Crewman(float x, float y, Texture texture, string name = "", Animation stand = null, Animation walk = null, Animation fall = null, Animation jump = null, Animation die = null, Color? textBoxColor = null) : base(x, y, texture, stand)
        {
            Name = name;
            StandingAnimation = stand;
            WalkingAnimation = walk;
            FallingAnimation = fall;
            JumpingAnimation = jump;
            DyingAnimation = die;
            defaultAnimation = StandingAnimation ?? new Animation(new System.Drawing.Point[] { new System.Drawing.Point(0, 0) }, System.Drawing.Rectangle.Empty, texture);
            Gravity = 0.6875f;
            CheckpointX = x;
            CheckpointY = y;
            CheckpointFlipX = flipX;
            CheckpointFlipY = flipY;
            TextBoxColor = textBoxColor ?? Color.White;
        }

        public override void Process()
        {
            base.Process();
            if (DyingFrames == 0)
            {
                if (Target != null)
                {
                    if (AIState == AIStates.Follow)
                    {
                        if (Target.X > Right + 16) InputDirection = 1;
                        else if (Target.Right < X - 16) InputDirection = -1;
                        else InputDirection = 0;
                        if (Target.CenterX > CenterX) flipX = false;
                        else if (Target.CenterX < CenterX) flipX = true;
                    }
                    else if (AIState == AIStates.Face)
                    {
                        if (Target.CenterX > CenterX) flipX = false;
                        else if (Target.CenterX < CenterX) flipX = true;
                    }
                }

                YVelocity += Gravity;
                if (JumpBuffer > 0) JumpBuffer -= 1;
                if (YVelocity > TerminalVelocity) YVelocity = TerminalVelocity;
                else if (YVelocity < -TerminalVelocity) YVelocity = -TerminalVelocity;
                if (OnGround)
                {
                    changeAnimationOnGround();
                    LedgeMercy = 2;
                }
                else
                {
                    if (LedgeMercy > 0)
                        LedgeMercy -= 1;
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
                    ResetAnimation();
                    Animation = StandingAnimation;
                    flipX = CheckpointFlipX;
                    if (Math.Sign(Gravity) == 1 == (flipY = CheckpointFlipY))
                    {
                        Gravity *= -1;
                    }
                    CenterX = CheckpointX;
                    if (flipY) Y = CheckpointY;
                    else Bottom = CheckpointY;

                    XVelocity = 0;
                    YVelocity = 0;
                    PreviousX = X;
                    PreviousY = Y;
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
            if (onPlatform != null)
            {
                onPlatform.OnTop.Remove(this);
                onPlatform = null;
            }
            Animation = DyingAnimation;
            ResetAnimation();
            DyingFrames = 60;
        }

        public override void CollideY(float distance, Sprite collision)
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
                    if (onPlatform != null)
                        onPlatform.OnTop.Remove(this);
                    onPlatform = collision as Platform;
                    onPlatform.OnTop.Add(this);
                    onPlatform.Disappear();
                }
                else if (onPlatform != null && collision as Platform == null)
                {
                    onPlatform.OnTop.Remove(this);
                    onPlatform = null;
                }
                if (JumpBuffer > 0)
                    FlipOrJump();
            }
            else if (Math.Sign(distance) == Math.Sign(YVelocity))
            {
                YVelocity = 0;
            }
        }

        public override void CollideX(float distance, Sprite collision)
        {
            base.CollideX(distance, collision);
            if (distance > 0 == XVelocity > 0)
                XVelocity = 0;
        }

        public void FlipOrJump()
        {
            if (DyingFrames > 0) return;
            if (OnGround || LedgeMercy > 0)
            {
                LedgeMercy = 0;
                JumpBuffer = 0;
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
            else
            {
                JumpBuffer = 6;
            }
        }

        public override CollisionData TestCollision(Sprite testFor)
        {
            if (testFor == this || DyingFrames > 0) return null;

            // Crewmen can collide with entities; normal Drawables cannot
            if (testFor.Solid == SolidState.Entity && IsOverlapping(testFor))
            {
                if (testFor.KillCrewmen)
                    return new CollisionData(true, 0, testFor);
                else
                    return GetCollisionData(testFor);
            }
            else
                return base.TestCollision(testFor);
        }

        public override void Collide(CollisionData cd)
        {
            if (cd.CollidedWith.KillCrewmen)
                Die();
            else if (cd.CollidedWith.Solid == SolidState.Entity)
            {
                cd.CollidedWith.HandleCrewmanCollision(this);
            }
            else
                base.Collide(cd);
        }

        public void KillSelf()
        {
            if (DyingFrames == 0)
            {
                Die();
            }
        }
    }
}
