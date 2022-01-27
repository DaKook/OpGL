using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;

namespace V7
{
    public class Crewman : Sprite, IMovingSprite
    {
        private Animation defaultAnimation;
        private Animation standingAnimation;
        private Animation walkingAnimation;
        private Animation fallingAnimation;
        private Animation jumpingAnimation;
        private Animation dyingAnimation;
        public float YVelocity { get; set; }
        public float XVelocity { get; set; }
        public static float UniversalTerminalVelocity = 5f;
        public float OwnTerminalVelocity = 5f;
        public float MaxSpeed = 3f;
        public float Acceleration = 0.475f;
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
        //private int spikeMercy = 4;
        private bool _sad = false;
        public AIStates AIState = AIStates.Stand;
        public List<int> HeldTrinkets = new List<int>();
        public List<Trinket> PendingTrinkets = new List<Trinket>();
        public Game Owner;
        public Script Script;
        public int IFrames = 0;
        public bool Invincible = false;
        public bool IsPlayer => this == Owner.ActivePlayer;
        public int Jumps;
        public int MaxJumps = 0;
        public bool Sideways = false;

        public override float Width => Sideways ? base.Height : base.Width;
        public override float Height => Sideways ? base.Width : base.Height;

        public delegate void RespawnedDelegate();
        public event RespawnedDelegate Respawned;

        public enum AIStates { Follow, Face, Stand };
        public Crewman Target;
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
        public override bool IsCrewman { get => true; }
        public Animation WalkingAnimation { get => walkingAnimation ?? defaultAnimation; set => walkingAnimation = value; }
        public Animation StandingAnimation { get => standingAnimation ?? defaultAnimation; set => standingAnimation = value; }
        public Animation FallingAnimation { get => fallingAnimation ?? defaultAnimation; set => fallingAnimation = value; }
        public Animation JumpingAnimation { get => jumpingAnimation ?? defaultAnimation; set => jumpingAnimation = value; }
        public Animation DyingAnimation { get => dyingAnimation ?? defaultAnimation; set => dyingAnimation = value; }
        public int DyingFrames;

        public static SoundEffect Flip1;
        private SoundEffect ivFlip1;
        public SoundEffect OwnFlip1 { get => ivFlip1 ?? Flip1; set => ivFlip1 = value; }
        public static SoundEffect Flip2;
        private SoundEffect ivFlip2;
        public SoundEffect OwnFlip2 { get => ivFlip2 ?? Flip2; set => ivFlip2 = value; }
        public static SoundEffect Cry;
        private SoundEffect ivCry;
        public SoundEffect OwnCry { get => ivCry ?? Cry; set => ivCry = value; }
        public SoundEffect Squeak;

        private bool jumped = false;
        public new CrewmanTexture Texture => base.Texture as CrewmanTexture;

        public Crewman(float x, float y, CrewmanTexture texture, Game owner, string name = "", Animation stand = null, Animation walk = null, Animation fall = null, Animation jump = null, Animation die = null, Color? textBoxColor = null) : base(x, y, texture, stand)
        {
            Name = name;
            StandingAnimation = stand ?? texture.AnimationFromName("Standing");
            WalkingAnimation = walk ?? texture.AnimationFromName("Walking");
            FallingAnimation = fall ?? texture.AnimationFromName("Falling");
            JumpingAnimation = jump ?? texture.AnimationFromName("Jumping");
            DyingAnimation = die ?? texture.AnimationFromName("Dying");
            defaultAnimation = StandingAnimation ?? new Animation(new Point[] { new Point(0, 0) }, Rectangle.Empty, texture);
            Gravity = 0.6875f;
            CheckpointX = x;
            CheckpointY = y;
            CheckpointFlipX = flipX;
            CheckpointFlipY = flipY;
            TextBoxColor = textBoxColor ?? texture.TextBoxColor;
            Squeak = owner.GetSound(texture.Squeak ?? "");
            Animation = StandingAnimation;
            Owner = owner;
        }

        public override void Process()
        {
            base.Process();
            if (DyingFrames == 0)
            {
                if (IFrames > 0)
                    IFrames -= 1;
                if (Target != null)
                {
                    float tcx, scx;
                    float tx, tr, sx, sr;
                    if (Sideways)
                    {
                        tcx = Target.CenterY; scx = CenterY;
                        tx = Target.Y; tr = Target.Bottom; sx = Y; sr = Bottom;
                    }
                    else
                    {
                        tcx = Target.CenterX; scx = CenterX;
                        tx = Target.X; tr = Target.Right; sx = X; sr = Right;
                    }
                    if (AIState == AIStates.Follow)
                    {
                        if (tx > sr + 16) InputDirection = 1;
                        else if (tr < sx - 16) InputDirection = -1;
                        else InputDirection = 0;
                        if (tcx > scx) flipX = false;
                        else if (tcx < scx) flipX = true;
                    }
                    else if (AIState == AIStates.Face)
                    {
                        if (tcx > scx) flipX = false;
                        else if (tcx < scx) flipX = true;
                    }
                }

                //if (spikeMercy < 3)
                //    spikeMercy += 1;

                float yv;
                if (Sideways)
                {
                    yv = XVelocity;
                    XVelocity += Gravity;
                    if (jumped)
                    {
                        if (Math.Sign(yv) != Math.Sign(Gravity))
                        {
                            if (!Owner.IsInputActive(Game.Inputs.Jump))
                                XVelocity += Gravity;
                        }
                        else
                            jumped = false;
                    }
                }
                else
                {
                    yv = YVelocity;
                    YVelocity += Gravity;
                    if (jumped)
                    {
                        if (Math.Sign(yv) != Math.Sign(Gravity))
                        {
                            if (!Owner.IsInputActive(Game.Inputs.Jump))
                                YVelocity += Gravity;
                        }
                        else
                            jumped = false;
                    }
                }
                if (JumpBuffer > 0) JumpBuffer -= 1;
                if (yv > OwnTerminalVelocity && Gravity > 0)
                {
                    if (Sideways)
                        XVelocity = OwnTerminalVelocity;
                    else
                        YVelocity = OwnTerminalVelocity;
                }
                else if (yv < -OwnTerminalVelocity && Gravity < 0)
                {
                    if (Sideways)
                        XVelocity = -OwnTerminalVelocity;
                    else
                        YVelocity = -OwnTerminalVelocity;
                }
                if (OnGround)
                {
                    changeAnimationOnGround();
                    if (!(IsOnPlatform && onPlatform.Sticky))
                        LedgeMercy = 2;
                    else
                        LedgeMercy = 0;
                }
                else
                {
                    if (LedgeMercy > 0)
                        changeAnimationOnGround();
                    else
                        changeAnimationInAir();
                    if (LedgeMercy > 0)
                        LedgeMercy -= 1;
                    if (onPlatform != null)
                    {
                        XVelocity += onPlatform.XVelocity + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                        YVelocity += onPlatform.YVelocity;
                        onPlatform.OnTop.Remove(this);
                        onPlatform = null;
                    }
                }
                //if (onPlatform != null)
                //{
                //    if (Right <= (onPlatform as Sprite).X || X >= (onPlatform as Sprite).Right)
                //    {
                //        XVelocity += onPlatform.XVel + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                //        YVelocity += onPlatform.YVel;
                //        onPlatform.OnTop.Remove(this);
                //        onPlatform = null;
                //    }
                //}
                OnGround = false;
                if (Sideways)
                    YVelocity = MoveX(YVelocity);
                else
                    XVelocity = MoveX(XVelocity);
                if ((flipX && InputDirection > 0) || (!flipX && InputDirection < 0))
                {
                    flipX = !flipX;
                }
                if (Gravity >= 0 == flipY) flipY = !flipY;
                X += XVelocity;
                Y += YVelocity;
                if (IFrames > 0 && (XVelocity != 0 || Math.Abs(YVelocity) > 1))
                    IFrames = 0;
            }
            else
            {
                DyingFrames -= 1;
                if (DyingFrames <= 0)
                {
                    Respawn();
                }
            }
        }

        private float MoveX(float xVel)
        {
            if (Math.Sign(InputDirection) == -1)
            {
                if (xVel > -MaxSpeed)
                {
                    xVel -= Acceleration;
                    if (xVel < -MaxSpeed)
                        xVel = -MaxSpeed;
                }
                else if (xVel < -MaxSpeed)
                {
                    xVel += Acceleration;
                    if (xVel > -MaxSpeed)
                        xVel = -MaxSpeed;
                }
            }
            else if (Math.Sign(InputDirection) == 1)
            {
                if (xVel < MaxSpeed)
                {
                    xVel += Acceleration;
                    if (xVel > MaxSpeed)
                        xVel = MaxSpeed;
                }
                else if (xVel > MaxSpeed)
                {
                    xVel -= Acceleration;
                    if (xVel < MaxSpeed)
                        xVel = MaxSpeed;
                }
            }
            else if (InputDirection == 0)
            {
                int s = Math.Sign(xVel);
                xVel -= s * Acceleration;
                if (Math.Sign(xVel) != s)
                    xVel = 0;
            }
            return xVel;
        }

        public void Respawn()
        {
            DyingFrames = 0;
            ResetAnimation();
            Animation = StandingAnimation;
            flipX = CheckpointFlipX;
            if (Math.Sign(Gravity) == 1 == (flipY = CheckpointFlipY))
            {
                Gravity *= -1;
            }
            if (CurrentCheckpoint is object)
            {
                Checkpoint c = CurrentCheckpoint;
                CenterX = CurrentCheckpoint.CenterX;
                if (flipY) Y = CurrentCheckpoint.Y;
                else Bottom = CurrentCheckpoint.Bottom;
                Owner.CheckPlayerRoom(false);
                if (CurrentCheckpoint != c)
                {
                    CenterX = CurrentCheckpoint.CenterX;
                    if (flipY) Y = CurrentCheckpoint.Y;
                    else Bottom = CurrentCheckpoint.Bottom;
                }
            }
            else
            {
                CenterX = CheckpointX;
                if (flipY) Y = CheckpointY;
                else Bottom = CheckpointY;
            }

            XVelocity = 0;
            YVelocity = 0;
            PreviousX = DX;
            PreviousY = DY;
            IFrames = 60;
            foreach (Trinket tr in PendingTrinkets)
            {
                tr.Visible = true;
                Owner.CollectedTrinkets.Remove(tr.ID);
            }
            PendingTrinkets.Clear();
            Respawned?.Invoke();
            MultiplePositions = false;
            IsWarpingH = false;
            IsWarpingV = false;
            Offsets.Clear();
            Solid = SolidState.Entity;
            SetPreviousLoaction();
            if (IsPlayer && Owner.OnPlayerRespawn is object)
            {
                Owner.ExecuteScript(Owner.OnPlayerRespawn, this, this, new Number[] { });
            }
        }

        private void changeAnimationOnGround()
        {
            float xv = Sideways ? YVelocity : XVelocity;
            if (xv != 0 && Animation != WalkingAnimation)
            {
                ResetAnimation();
                Animation = WalkingAnimation;
            }
            else if (xv == 0 && Animation != StandingAnimation)
            {
                ResetAnimation();
                Animation = StandingAnimation;
            }
        }

        private void changeAnimationInAir()
        {
            float yv = Sideways ? XVelocity : YVelocity;
            if (Math.Sign(yv) == Math.Sign(Gravity) && Animation != FallingAnimation)
            {
                ResetAnimation();
                Animation = FallingAnimation;
            }
            else if (Math.Sign(yv) == -Math.Sign(Gravity) && Animation != JumpingAnimation && Gravity != 0)
            {
                Animation = JumpingAnimation;
            }
        }

        public virtual void Die(bool ignoreInvincible = false)
        {
            if (DyingFrames > 0 || IFrames > 0 || (Invincible && !ignoreInvincible)) return;
            if (onPlatform != null)
            {
                onPlatform.OnTop.Remove(this);
                onPlatform = null;
            }
            XVelocity = 0;
            YVelocity = 0;
            JumpBuffer = 0;
            Cry?.Play();
            Animation = DyingAnimation;
            ResetAnimation();
            DyingFrames = 60;
            Solid = SolidState.NonSolid;
            if (IsPlayer && Owner.OnPlayerDeath is object)
            {
                Owner.ExecuteScript(Owner.OnPlayerDeath, this, this, new Number[] { });
            }
        }

        public override void CollideY(double distance, Sprite collision)
        {
            base.CollideY(distance, collision);
            //Check if landing on ground
            if (!Sideways)
            {
                YVelocity = CheckLanding(distance, collision, YVelocity);
            }
            else
                YVelocity = 0;
        }

        public override void CollideX(double distance, Sprite collision)
        {
            base.CollideX(distance, collision);
            //Check if landing on ground
            if (Sideways)
            {
                XVelocity = CheckLanding(distance, collision, XVelocity);
            }
            else if (Math.Sign(XVelocity) == Math.Sign(distance))
                XVelocity = 0;
        }

        public float CheckLanding(double distance, Sprite collision, float yv)
        {
            if (Math.Sign(distance) == Math.Sign(Gravity))
            {
                yv = 0;
                OnGround = true;
                Jumps = 0;
                //changeAnimationOnGround();
                //Check if landing on a platform
                if (collision is IPlatform && onPlatform != collision)
                {
                    if (onPlatform != null)
                    {
                        XVelocity += onPlatform.XVelocity + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                        YVelocity += onPlatform.YVelocity;
                        onPlatform.OnTop.Remove(this);
                    }
                    onPlatform = collision as IPlatform;
                    bool xg = XVelocity > 0;
                    //if (XVelocity != 0)
                    {
                        XVelocity -= onPlatform.XVelocity + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                        //if (XVelocity < 0 && xg)
                        //    XVelocity = 0;
                        //else if (XVelocity > 0 && !xg)
                        //    XVelocity = 0;
                    }
                    onPlatform.OnTop.Add(this);
                    onPlatform.Disappear();
                }
                else if (onPlatform != null && collision as IPlatform == null)
                {
                    XVelocity += onPlatform.XVelocity + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                    YVelocity += onPlatform.YVelocity;
                    onPlatform.OnTop.Remove(this);
                    onPlatform = null;
                }
                if (JumpBuffer > 0)
                {
                    FlipOrJump();
                    yv = Sideways ? XVelocity : YVelocity;
                }
            }
            else if (Math.Sign(distance) == Math.Sign(yv))
            {
                yv = 0;
            }
            return yv;
        }

        public void FlipOrJump()
        {
            if (DyingFrames > 0 || Jump == 0 || (IsOnPlatform && onPlatform.Sticky)) return;
            if (OnGround || LedgeMercy > 0 || Jumps < MaxJumps)
            {
                if (!OnGround && LedgeMercy <= 0)
                    Jumps += 1;
                LedgeMercy = 0;
                JumpBuffer = 0;
                OnGround = false;
                if (onPlatform != null)
                {
                    XVelocity += onPlatform.XVelocity + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                    YVelocity += onPlatform.YVelocity;
                    onPlatform.OnTop.Remove(this);
                    onPlatform = null;
                }
                if (Sideways)
                    XVelocity = -Jump * Math.Sign(Gravity);
                else
                    YVelocity = -Jump * Math.Sign(Gravity);
                if (CanFlip)
                {
                    Gravity *= -1;
                    if (Math.Sign(Gravity) == -1)
                        Flip1?.Play();
                    else if (Math.Sign(Gravity) == 1)
                        Flip2?.Play();
                }
                else
                {
                    if (Jump > 0)
                        Flip1?.Play();
                    jumped = true;
                }
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

            // Crewmen can collide with entities; normal Sprites cannot
            Vector4i? overlap;
            if ((testFor.Solid == SolidState.Entity || testFor.KillCrewmen || testFor is GravityLine || testFor is WarpLine) && (overlap = IsOverlapping(testFor)) is object)
            {
                if (testFor.KillCrewmen || testFor.AlwaysCollide)
                {
                    if (testFor is Tile && (testFor as Tile).State != Tile.TileStates.Normal)
                    {
                        switch ((testFor as Tile).State)
                        {
                            case Tile.TileStates.SpikeR:
                                {
                                    if (Bottom < testFor.Y + 4)
                                    {
                                        if ((X - testFor.X) * 2 > Bottom - testFor.Y)
                                            return null;
                                        else
                                            return new CollisionData(true, 0, testFor);
                                    }
                                    else if (Y > testFor.Y + 4)
                                    {
                                        if ((X - testFor.X) * 2 > testFor.Bottom - Y)
                                            return null;
                                        else
                                            return new CollisionData(true, 0, testFor);
                                    }
                                    else
                                        return new CollisionData(true, 0, testFor);
                                }
                            case Tile.TileStates.SpikeL:
                                {
                                    if (Bottom > testFor.Y + 4)
                                    {
                                        if ((Right - testFor.Right) * 2 > Bottom - testFor.Y)
                                            return null;
                                        else
                                            return new CollisionData(true, 0, testFor);
                                    }
                                    else if (Y < testFor.Y + 4)
                                    {
                                        if ((Right - testFor.Right) * 2 > testFor.Bottom - Y)
                                            return null;
                                        else
                                            return new CollisionData(true, 0, testFor);
                                    }
                                    else
                                        return new CollisionData(true, 0, testFor);
                                }
                            case Tile.TileStates.SpikeU:
                                {
                                    if (-YVelocity > Math.Abs(XVelocity))
                                        return null;
                                    if (Right < testFor.X + 4)
                                    {
                                        if ((testFor.Bottom - Bottom) * 2 > Right - testFor.X)
                                            return null;
                                        else
                                            return new CollisionData(true, 0, testFor);
                                    }
                                    else if (X > testFor.X + 4)
                                    {
                                        if ((testFor.Bottom - Bottom) * 2 > testFor.Right - X)
                                            return null;
                                        else
                                            return new CollisionData(true, 0, testFor);
                                    }
                                    else
                                        return new CollisionData(true, 0, testFor);
                                }
                            case Tile.TileStates.SpikeD:
                                {
                                    if (YVelocity > Math.Abs(XVelocity))
                                        return null;
                                    if (Right < testFor.X + 4)
                                    {
                                        if ((Y - testFor.Y) * 2 > Right - testFor.X)
                                            return null;
                                        else
                                            return new CollisionData(true, 0, testFor);
                                    }
                                    else if (X > testFor.X + 4)
                                    {
                                        if ((Y - testFor.Y) * 2 > testFor.Right - X)
                                            return null;
                                        else
                                            return new CollisionData(true, 0, testFor);
                                    }
                                    else
                                        return new CollisionData(true, 0, testFor);
                                }
                            default:
                                return new CollisionData(true, 0, testFor);
                        }
                    }
                    else
                        return new CollisionData(true, 0, testFor);
                }
                else
                    return GetCollisionData(testFor, new Point(overlap.Value.X, overlap.Value.Y), new Point(overlap.Value.Z, overlap.Value.W));
            }
            else
                return base.TestCollision(testFor);
        }

        protected override CollisionData GetCollisionData(Sprite testFor, Point offsets, Point hitboxes)
        {
            bool isTile = testFor is ISolidObject;
            int direction = -1;
            if (isTile)
            {
                direction = (int)(testFor as ISolidObject).State;
                if (direction < 5)
                {
                    direction = -1;
                }
                else
                    direction -= 5;
            }
            for (int i = -1; i < Offsets.Count; i++)
            {
                float ofX = i > -1 ? Offsets[i].X : 0;
                float ofY = i > -1 ? Offsets[i].Y : 0;
                for (int j = -1; j < testFor.Offsets.Count; j++)
                {
                    float ofXO = j > -1 ? testFor.Offsets[j].X : 0;
                    float ofYO = j > -1 ? testFor.Offsets[j].Y : 0;
                    if (!testFor.Within(X + ofX, Y + ofY, Width, Height, ofXO, ofYO)) continue;
                    // check for vertical collision first
                    // top
                    if (Math.Round(PreviousY + PreviousHeight + ofY - ((LedgeMercy > 0 && !Sideways) ? 2 : 0), 4) <= Math.Round(testFor.PreviousY, 4) + ofYO && direction < 1)
                        return new CollisionData(true, Bottom + ofY - (testFor.Y + ofYO), testFor);
                    // bottom
                    else if (Math.Round(PreviousY + ofY + ((LedgeMercy > 0 && !Sideways) ? 2 : 0), 4) >= Math.Round(testFor.PreviousY + testFor.Height + ofYO, 4) && (direction == -1 || direction == 1))
                        return new CollisionData(true, Y + ofY - (testFor.Bottom + ofYO), testFor);
                    // right
                    else if (Math.Round(PreviousX + PreviousWidth + ofX - ((LedgeMercy > 0 && Sideways) ? 2 : 0), 4) <= Math.Round(testFor.PreviousX + ofXO, 4) && (direction == -1 || direction == 3))
                        return new CollisionData(false, Right + ofX - (testFor.X + ofXO), testFor);
                    // left
                    else if (Math.Round(PreviousX + ofX + ((LedgeMercy > 0 && Sideways) ? 2 : 0), 4) >= Math.Round(testFor.PreviousX + testFor.Width + ofXO, 4) && (direction == -1 || direction == 2))
                        return new CollisionData(false, X + ofX - (testFor.Right + ofXO), testFor);
                    else if (testFor is ScriptBox || testFor is WarpLine)
                        return new CollisionData(true, 0, testFor);
                    if (!testFor.MultiplePositions)
                        break;
                }
                if (!MultiplePositions) break;
            }

            return null;
        }

        public override bool CollideWith(CollisionData data)
        {
            if (data.CollidedWith is WarpLine) data.CollidedWith.Collide(new CollisionData(data.Vertical, -data.Distance, this));
            if (data.CollidedWith.KillCrewmen)
            {
                //if (!OnGround && data.CollidedWith is Tile)
                //{
                //    spikeMercy -= 2;
                //    if (spikeMercy <= 0)
                //    {
                //        spikeMercy = 3;
                //        Die();
                //    }
                //}
                //else
                    Die();
            }
            else if (data.CollidedWith.Solid != SolidState.Ground)
            {
                data.CollidedWith.HandleCrewmanCollision(this);
            }
            else
                base.Collide(data);
            return true;
        }

        public void KillSelf()
        {
            if (DyingFrames == 0)
            {
                Die(true);
            }
        }

        //public override void SetProperty(string name, JToken value, Game game)
        //{
        //    switch (name)
        //    {
        //        case "Speed":
        //            MaxSpeed = (float)value;
        //            break;
        //        case "Acceleration":
        //            Acceleration = (float)value;
        //            break;
        //        case "Standing":
        //            StandingAnimation = Texture.AnimationFromName((string)value);
        //            break;
        //        case "Walking":
        //            WalkingAnimation = Texture.AnimationFromName((string)value);
        //            break;
        //        case "Jumping":
        //            JumpingAnimation = Texture.AnimationFromName((string)value);
        //            break;
        //        case "Falling":
        //            FallingAnimation = Texture.AnimationFromName((string)value);
        //            break;
        //        case "Dying":
        //            DyingAnimation = Texture.AnimationFromName((string)value);
        //            break;
        //        case "Sad":
        //            Sad = (bool)value;
        //            break;
        //        case "TextBox":
        //            TextBoxColor = Color.FromArgb((int)value);
        //            break;
        //        case "Squeak":
        //            Squeak = game.GetSound((string)value);
        //            break;
        //        default:
        //            base.SetProperty(name, value, game);
        //            break;
        //    }
        //}

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "Crewman");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    if (StandingAnimation.Name != "Standing")
        //        ret.Add("Standing", StandingAnimation?.Name ?? "");
        //    if (WalkingAnimation.Name != "Walking")
        //        ret.Add("Walking", WalkingAnimation?.Name ?? "");
        //    if (FallingAnimation.Name != "Falling")
        //        ret.Add("Falling", FallingAnimation?.Name ?? "");
        //    if (JumpingAnimation.Name != "Jumping")
        //        ret.Add("Jumping", JumpingAnimation?.Name ?? "");
        //    if (DyingAnimation.Name != "Dying")
        //        ret.Add("Dying", DyingAnimation?.Name ?? "");
        //    ret.Add("Name", Name);
        //    ret.Add("TextBox", TextBoxColor.ToArgb());
        //    ret.Add("FlipX", flipX);
        //    if (Sad)
        //        ret.Add("Sad", Sad);
        //    if (Gravity != 0.6875f)
        //        ret.Add("Gravity", Gravity);
        //    if (Acceleration != 0.475f)
        //        ret.Add("Acceleration", Acceleration);
        //    ret.Add("Squeak", Squeak?.Name ?? "");
        //    return ret;
        //}
        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Remove("Animation");
                ret["Gravity"].CanSet = true;
                ret["Gravity"].DefaultValue = 0.6875f;
                ret.Add("Standing", new SpriteProperty("Standing", () => StandingAnimation.Name, (t, g) => StandingAnimation = Texture.AnimationFromName((string)t), "Standing", SpriteProperty.Types.Animation, "The standing animation of the crewman."));
                ret.Add("Walking", new SpriteProperty("Walking", () => WalkingAnimation.Name, (t, g) => WalkingAnimation = Texture.AnimationFromName((string)t), "Walking", SpriteProperty.Types.Animation, "The walking animation of the crewman."));
                ret.Add("Jumping", new SpriteProperty("Jumping", () => JumpingAnimation.Name, (t, g) => JumpingAnimation = Texture.AnimationFromName((string)t), "Jumping", SpriteProperty.Types.Animation, "The jumping animation of the crewman."));
                ret.Add("Falling", new SpriteProperty("Falling", () => FallingAnimation.Name, (t, g) => FallingAnimation = Texture.AnimationFromName((string)t), "Falling", SpriteProperty.Types.Animation, "The falling animation of the crewman."));
                ret.Add("Dying", new SpriteProperty("Dying", () => DyingAnimation.Name, (t, g) => DyingAnimation = Texture.AnimationFromName((string)t), "Dying", SpriteProperty.Types.Animation, "The dying animation of the crewman."));
                ret.Add("TextBox", new SpriteProperty("TextBox", () => System.Drawing.Color.FromArgb(TextBoxColor.A, TextBoxColor.R, TextBoxColor.G, TextBoxColor.B).ToArgb(), (t, g) => {
                    System.Drawing.Color c = System.Drawing.Color.FromArgb((int)t);
                    TextBoxColor = Color.FromArgb(c.A, c.R, c.G, c.B); 
                }, 0, SpriteProperty.Types.Color, "The color of the textboxes for the crewman."));
                ret.Add("Sad", new SpriteProperty("Sad", () => Sad, (t, g) => Sad = (bool)t, false, SpriteProperty.Types.Bool, "Whether or not the crewman is sad."));
                ret.Add("Speed", new SpriteProperty("Speed", () => MaxSpeed, (t, g) => MaxSpeed = (float)t, 3f, SpriteProperty.Types.Float, "The max speed in pixels/frame of the crewman."));
                ret.Add("Acceleration", new SpriteProperty("Acceleration", () => Acceleration, (t, g) => Acceleration = (float)t, 0.475f, SpriteProperty.Types.Float, "The acceleration in pixels/frame/frame of the crewman."));
                ret.Add("Jump", new SpriteProperty("Jump", () => Jump, (t, g) => Jump = (float)t, 1.6875f, SpriteProperty.Types.Float, "The jump height of the crewman."));
                ret.Add("CanFlip", new SpriteProperty("CanFlip", () => CanFlip, (t, g) => CanFlip = (bool)t, true, SpriteProperty.Types.Bool, "Whether the crewman can flip, otherwise can jump."));
                ret.Add("Squeak", new SpriteProperty("Squeak", () => Squeak?.Name, (t, g) => Squeak = g.GetSound((string)t), "crew1", SpriteProperty.Types.Sound, "The sound played when this crewman talks."));
                ret.Add("DoubleJump", new SpriteProperty("DoubleJump", () => MaxJumps, (t, g) => MaxJumps = (int)t, 0, SpriteProperty.Types.Int, "The amount of double jumps the crewman can perform."));
                ret.Add("Invincible", new SpriteProperty("Invincible", () => Invincible, (t, g) => Invincible = (bool)t, false, SpriteProperty.Types.Bool, "Whether the crewman can die or not."));
                ret.Add("TerminalVelocity", new SpriteProperty("TerminalVelocity", () => OwnTerminalVelocity, (t, g) => OwnTerminalVelocity = (float)t, 5f, SpriteProperty.Types.Float, "The max falling speed of the crewman."));
                ret.Add("XVelocity", new SpriteProperty("XVelocity", () => XVelocity, (t, g) => XVelocity = (float)t, 0f, SpriteProperty.Types.Float, "The X speed of the crewman."));
                ret.Add("YVelocity", new SpriteProperty("YVelocity", () => YVelocity, (t, g) => YVelocity = (float)t, 0f, SpriteProperty.Types.Float, "The Y speed of the crewman."));
                ret.Add("Sideways", new SpriteProperty("Sideways", () => Sideways, (t, g) => Sideways = (bool)t, false, SpriteProperty.Types.Bool, "Whether the crewmate falls sideways or not."));
                ret["Type"].GetValue = () => "Crewman";
                return ret;
            }
        }
        protected override void ResetSprite()
        {
            base.ResetSprite();
            XVelocity = YVelocity = 0;
            InputDirection = 0;
            onPlatform = null;
            HeldTrinkets.Clear();
            PendingTrinkets.Clear();
        }

        public override void RenderPrep()
        {
            RenderPrep(Sideways);
        }
    }
}
