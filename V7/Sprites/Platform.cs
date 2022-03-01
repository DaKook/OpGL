using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
//using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace V7
{
    public class Platform : BoxSprite, IPlatform, IBoundSprite, IMovingSprite, ISolidObject
    {
        public override Pushability Pushability => Pushability.Solid;
        public float XSpeed;
        public float YSpeed;
        public Tile.TileStates State { get; set; }
        public float XVelocity { get; set; }
        public float YVelocity { get; set; }
        public float Conveyor { get; set; }
        public bool Sticky { get; set; }
        public Script BoardEvent;
        public Script LeaveEvent;
        SpriteProperty evBoard => new SpriteProperty("BoardEvent", () => BoardEvent?.Name ?? "", (t, g) => BoardEvent = g.ScriptFromName((string)t ?? ""), "", SpriteProperty.Types.Script, "The script to run when the platform is boarded by a crewman.");
        SpriteProperty evLeave => new SpriteProperty("LeaveEvent", () => LeaveEvent?.Name ?? "", (t, g) => LeaveEvent = g.ScriptFromName((string)t ?? ""), "", SpriteProperty.Types.Script, "The script to run when the platform is left by a crewman.");
        public bool CanDisappear;
        public Animation NormalAnimation;
        public Animation DisappearAnimation;
        public bool IsDisappearing = false;
        private int DisappearFrames = -1;
        public bool MultiTexture = true;

        public override float Height => Animation.Hitbox.Height * VLength;
        public override float Width => Animation.Hitbox.Width * Length;
        //private int _length = 4;
        //private int _height = 1;
        public int Length
        {
            get => WidthTiles;
            set
            {
                SetWidth(value);
            }
        }
        public int VLength
        {
            get => HeightTiles;
            set
            {
                SetHeight(value);
            }
        }
        /// <summary>
        /// Determines whether or not the platform (conveyor) pushes crewmen the opposite direction on the bottom. Set to true to disable this.
        /// </summary>
        public bool SingleDirection { get; set; } = true;
        public List<Sprite> OnTop { get; set; } = new List<Sprite>();
        private Rectangle _bounds;
        public Rectangle Bounds { get => _bounds; set => _bounds = value; }
        public static SoundEffect DisappearSound;
        public Platform(float x, float y, Texture texture, Animation animation, float xSpeed = 0, float ySpeed = 0, float conveyor = 0, bool disappear = false, Animation disappearAnimation = null, int length = 4, int height = 1) : base(x, y, texture, length, height)
        {
            NormalAnimation = animation;
            Animation = animation;
            XVelocity = xSpeed;
            YVelocity = ySpeed;
            Conveyor = conveyor;
            CanDisappear = disappear;
            DisappearAnimation = disappearAnimation ?? Animation.EmptyAnimation;
            Length = length;
            VLength = height;
            Solid = SolidState.Ground;
        }

        //public void SetBuffer()
        //{
        //    instances = _length * _height;
        //    bufferData = new float[_length * _height * 4];
        //    int index = 0;
        //    int curX = 0;
        //    int curY = 0;
        //    for (int y = 0; y < _height; y++)
        //    {
        //        for (int x = 0; x < _length; x++)
        //        {
        //            bufferData[index++] = curX;
        //            bufferData[index++] = curY;
        //            bufferData[index++] = x == 0 ? 0 : (x == _length - 1 ? 2 : 1);
        //            bufferData[index++] = y == 0 ? 0 : (y == _height - 1 ? 2 : 1);
        //            curX += Texture.TileSizeX;
        //        }
        //        curY += Texture.TileSizeY;
        //    }
        //    updateBuffer = true;
        //}

        public void Reappear()
        {
            Animation = NormalAnimation;
            ResetAnimation();
            DisappearFrames = -1;
            Visible = true;
            Solid = SolidState.Ground;
            IsDisappearing = false;
        }

        public override void Process()
        {
            //Do not process if disappeared
            if (DisappearFrames == 0) return;
            base.Process();
            if (XVelocity == 0)
            {
                FramePush.Left = FramePush.Right = Pushability.Immovable;
            }
            if (YVelocity == 0)
            {
                FramePush.Up = FramePush.Down = Pushability.Immovable;
            }
            Move(XVelocity, YVelocity);
            CheckBounds();
            if (DisappearFrames > 0)
            {
                DisappearFrames -= 1;
                if (DisappearFrames == 0)
                {
                    Visible = false;
                    Solid = SolidState.NonSolid;
                }
            }
            foreach (Sprite sprite in OnTop)
            {
                sprite.Move(SingleDirection ? Conveyor : (sprite.Gravity > 0 ? Conveyor : -Conveyor), 0);
            }
        }

        public void CheckBounds()
        {
            if (_bounds.Width > 0 && _bounds.Height > 0)
            {
                if (Right > _bounds.X + _bounds.Width + InitialX)
                {
                    float x = Right - (_bounds.X + _bounds.Width + InitialX);
                    Move(-x, 0);
                    XVelocity *= -1;
                }
                else if (X < _bounds.X + InitialX)
                {
                    float x = X - (_bounds.X + InitialX);
                    Move(-x, 0);
                    XVelocity *= -1;
                }
                else if (Bottom > _bounds.Y + _bounds.Height + InitialY)
                {
                    float y = Bottom - (_bounds.Y + _bounds.Height + InitialY);
                    Move(0, -y);
                    YVelocity *= -1;
                }
                else if (Y < _bounds.Y + InitialY)
                {
                    float y = Y - (_bounds.Y + InitialY);
                    Move(0, -y);
                    YVelocity *= -1;
                }
            }
        }

        public override void Move(double x, double y)
        {
            base.Move(x, y);
            foreach (Sprite sprite in OnTop)
            {
                sprite.Move(x, y);
            }
        }

        //public override CollisionData TestCollision(Sprite testFor)
        //{
        //    if (testFor == this) return null;

        //    // Platforms colliding with an entity should cause the entity to do a collision check. This collision should not be used as such. Thus, NaN.
        //    CollisionData ret = null;
        //    if ((testFor.Solid == SolidState.Entity || testFor is PushSprite) && IsOverlapping(testFor))
        //    {
        //        ret = GetCollisionData(testFor);
        //        if (ret != null)
        //            ret.Distance = float.NaN;
        //    }
        //    else
        //        ret = base.TestCollision(testFor);

        //    return ret;
        //}

        public override void CollideX(double distance, Sprite collision)
        {
            if (XVelocity != 0)
            {
                base.CollideX(distance, collision);
                foreach (Sprite d in OnTop)
                {
                    d.DX -= distance;
                }
                if (Math.Sign(distance) == Math.Sign(XVelocity))
                    XVelocity *= -1;
            }
            else if (collision != null && !collision.Static && collision.Solid == SolidState.Ground)
                collision.DX += distance;

            if (collision is Platform)
                collision.CollideX(-distance, null);

            CheckBounds();
        }

        public override void CollideY(double distance, Sprite collision)
        {
            if (YVelocity != 0)
            {
                base.CollideY(distance, collision);
                foreach (Sprite d in OnTop)
                {
                    if (!d.Static && d != collision)
                        d.DY -= distance;
                }
                if (Math.Sign(distance) == Math.Sign(YVelocity))
                    YVelocity *= -1;
            }
            else if (!collision.Static && collision.Solid == SolidState.Ground)
                collision.DY += distance;

            if (collision is Platform)
                (collision as Platform).YVelocity *= -1;

            //CheckBounds();
        }

        public override bool CollideWith(CollisionData data)
        {
            if (Static || Immovable) return false;
            if (data.CollidedWith is WarpLine) data.CollidedWith.Collide(new CollisionData(data.Vertical, -data.Distance, this));
            if (data.CollidedWith.Solid == SolidState.Entity && (data.CollidedWith.Static || data.CollidedWith.Immovable)) return true;
            if (data.Vertical)
            {
                if (YVelocity == 0) return false;
                CollideY(data.Distance, data.CollidedWith);
            }
            else
            {
                if (XVelocity == 0) return false;
                CollideX(data.Distance, data.CollidedWith);
            }
            return true;
        }

        public void Disappear()
        {
            if (!CanDisappear || IsDisappearing) return;
            if (DisappearAnimation != null)
            {
                IsDisappearing = true;
                DisappearSound?.Play();
                ResetAnimation();
                Animation = DisappearAnimation;
                DisappearFrames = DisappearAnimation.FrameCount * DisappearAnimation.BaseSpeed;
            }
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "Platform");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Animation", Animation.Name);
        //    ret.Add("DisappearAnimation", DisappearAnimation.Name);
        //    ret.Add("XSpeed", XVel);
        //    ret.Add("YSpeed", YVel);
        //    ret.Add("Conveyor", Conveyor);
        //    ret.Add("Name", Name);
        //    ret.Add("Disappear", CanDisappear);
        //    ret.Add("Color", Color.ToArgb());
        //    ret.Add("_boundsX", _bounds.X);
        //    ret.Add("_boundsY", _bounds.Y);
        //    ret.Add("_boundsWidth", _bounds.Width);
        //    ret.Add("_boundsHeight", _bounds.Height);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Events
        {
            get
            {
                SortedList<string, SpriteProperty> ret = new SortedList<string, SpriteProperty>();
                ret.Add("BoardEvent", evBoard);
                ret.Add("LeaveEvent", evLeave);
                return ret;
            }
        }

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("DisappearAnimation", new SpriteProperty("DisappearAnimation", () => DisappearAnimation.Name, (t, g) => DisappearAnimation = Texture.AnimationFromName((string)t), "Disappear", SpriteProperty.Types.Animation, "The animation displayed as the platform is disappearing."));
                ret.Add("Disappear", new SpriteProperty("Disappear", () => CanDisappear, (t, g) => CanDisappear = (bool)t, false, SpriteProperty.Types.Bool, "Whether the platform disappears or not."));
                ret.Add("Conveyor", new SpriteProperty("Conveyor", () => Conveyor, (t, g) => Conveyor = (float)t, 0f, SpriteProperty.Types.Float, "The speed of the conveyor belt."));
                ret.Add("SingleDirection", new SpriteProperty("SingleDirection", () => SingleDirection, (t, g) => SingleDirection = (bool)t, true, SpriteProperty.Types.Bool, "Whether the bottom of the conveyor pushes the same way or not."));
                ret.Add("XSpeed", new SpriteProperty("XSpeed", () => XVelocity, (t, g) => XVelocity = (float)t, 0f, SpriteProperty.Types.Float, "The X speed in pixels/frame of the platform."));
                ret.Add("YSpeed", new SpriteProperty("YSpeed", () => YVelocity, (t, g) => YVelocity = (float)t, 0f, SpriteProperty.Types.Float, "The Y speed in pixels/frame of the platform."));
                ret.Add("BoundsX", new SpriteProperty("BoundsX", () => _bounds.X, (t, g) => _bounds.X = (int)t, 0, SpriteProperty.Types.Int, "The left edge of the platform's bounds."));
                ret.Add("BoundsY", new SpriteProperty("BoundsY", () => _bounds.Y, (t, g) => _bounds.Y = (int)t, 0, SpriteProperty.Types.Int, "The top edge of the platform's bounds."));
                ret.Add("BoundsWidth", new SpriteProperty("BoundsWidth", () => _bounds.Width, (t, g) => _bounds.Width = (int)t, 0, SpriteProperty.Types.Int, "The width of the platform's bounds."));
                ret.Add("BoundsHeight", new SpriteProperty("BoundsHeight", () => _bounds.Height, (t, g) => _bounds.Height = (int)t, 0, SpriteProperty.Types.Int, "The height of the platform's bounds."));
                ret.Add("Length", new SpriteProperty("Length", () => Length, (t, g) => Length = (int)t, 4, SpriteProperty.Types.Int, "The length in tiles of the platform."));
                ret.Add("Height", new SpriteProperty("Height", () => VLength, (t, g) => VLength = (int)t, 1, SpriteProperty.Types.Int, "The height in tiles of the platform."));
                ret.Add("Sticky", new SpriteProperty("Sticky", () => Sticky, (t, g) => Sticky = (bool)t, false, SpriteProperty.Types.Bool, "Crewmen cannot flip/jump from sticky platforms."));
                ret.Add("SolidSide", new SpriteProperty("SolidSide", () => (int)State, (t, g) => State = (Tile.TileStates)(int)t, 0, SpriteProperty.Types.Int, "The one-way state of the platform.", false));
                ret.Add("BoardEvent", evBoard);
                ret.Add("LeaveEvent", evLeave);
                ret["Type"].GetValue = () => "Platform";
                return ret;
            }
        }

        public override JObject Save(Game game, bool isUniversal = false)
        {
            JObject ret = base.Save(game, isUniversal);
            if (OnTop.Count > 0)
            {
                JArray ot = new JArray();
                foreach (Sprite sprite in OnTop)
                {
                    ot.Add(sprite.Save(game, false));
                }
                ret.Add("Attached", ot);
            }
            return ret;
        }
    }
}
