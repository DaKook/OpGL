using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Platform : InstancedSprite, IPlatform, IBoundSprite
    {
        public override Pushability Pushability => Pushability.Solid;
        public float XSpeed;
        public float YSpeed;
        public float XVel { get; set; }
        public float YVel { get; set; }
        public float Conveyor { get; set; }
        public bool CanDisappear;
        public Animation NormalAnimation;
        public Animation DisappearAnimation;
        public bool IsDisappearing = false;
        private int DisappearFrames = -1;
        public bool MultiTexture = true;
        private int _length = 4;
        public int Length
        {
            get => _length;
            set
            {
                _length = value;
                SetBuffer();
            }
        }
        public override float Width => base.Width * _length;
        /// <summary>
        /// Determines whether or not the platform pushes crewmen the opposite direction on the bottom. Set to true to disable this.
        /// </summary>
        public bool SingleDirection { get; set; }
        public List<Sprite> OnTop { get; set; } = new List<Sprite>();
        private Rectangle _bounds;
        public Rectangle Bounds { get => _bounds; set => _bounds = value; }
        public static SoundEffect DisappearSound;
        public Platform(float x, float y, Texture texture, Animation animation, float xSpeed = 0, float ySpeed = 0, float conveyor = 0, bool disappear = false, Animation disappearAnimation = null, int length = 4) : base(x, y, texture, animation)
        {
            NormalAnimation = animation;
            XVel = xSpeed;
            YVel = ySpeed;
            Conveyor = conveyor;
            CanDisappear = disappear;
            DisappearAnimation = disappearAnimation ?? Animation.EmptyAnimation;
            Length = length;
            Solid = SolidState.Ground;
        }

        public void SetBuffer()
        {
            instances = _length;
            bufferData = new float[_length * 4];
            int index = 0;
            int curX = 0;
            for (int i = 0; i < instances; i++)
            {
                bufferData[index++] = curX;
                bufferData[index++] = 0;
                bufferData[index++] = i == 0 ? 0 : (i == _length - 1 ? 2 : 1);
                bufferData[index++] = 0;
                curX += Texture.TileSizeX;
            }
            updateBuffer = true;
        }

        public void Reappear()
        {
            Animation = NormalAnimation;
            ResetAnimation();
            DisappearFrames = -1;
            Visible = true;
            Solid = SolidState.Ground;IsDisappearing = false;
        }

        public override void Process()
        {
            //Do not process if disappeared
            if (DisappearFrames == 0) return;
            base.Process();
            if (XVel == 0)
            {
                FramePush.Left = FramePush.Right = Pushability.Immovable;
            }
            if (YVel == 0)
            {
                FramePush.Up = FramePush.Down = Pushability.Immovable;
            }
            Move(XVel, YVel);
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
                    XVel *= -1;
                }
                else if (X < _bounds.X + InitialX)
                {
                    float x = X - (_bounds.X + InitialX);
                    Move(-x, 0);
                    XVel *= -1;
                }
                else if (Bottom > _bounds.Y + _bounds.Height + InitialY)
                {
                    float y = Bottom - (_bounds.Y + _bounds.Height + InitialY);
                    Move(0, -y);
                    YVel *= -1;
                }
                else if (Y < _bounds.Y + InitialY)
                {
                    float y = Y - (_bounds.Y + InitialY);
                    Move(0, -y);
                    YVel *= -1;
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
            if (XVel != 0)
            {
                base.CollideX(distance, collision);
                foreach (Sprite d in OnTop)
                {
                    d.DX -= distance;
                }
                if (Math.Sign(distance) == Math.Sign(XVel))
                    XVel *= -1;
            }
            else if (collision != null && !collision.Static && collision.Solid == SolidState.Ground)
                collision.DX += distance;

            if (collision is Platform)
                collision.CollideX(-distance, null);

            CheckBounds();
        }

        public override void CollideY(double distance, Sprite collision)
        {
            if (YVel != 0)
            {
                base.CollideY(distance, collision);
                foreach (Sprite d in OnTop)
                {
                    if (!d.Static && d != collision)
                        d.DY -= distance;
                }
                if (Math.Sign(distance) == Math.Sign(YVel))
                    YVel *= -1;
            }
            else if (!collision.Static && collision.Solid == SolidState.Ground)
                collision.DY += distance;

            if (collision is Platform)
                (collision as Platform).YVel *= -1;

            //CheckBounds();
        }

        public override bool CollideWith(CollisionData data)
        {
            if (Static || Immovable) return false;
            if (data.CollidedWith is WarpLine) data.CollidedWith.Collide(new CollisionData(data.Vertical, -data.Distance, this));
            if (data.CollidedWith.Solid == SolidState.Entity && (data.CollidedWith.Static || data.CollidedWith.Immovable)) return true;
            if (data.Vertical)
            {
                if (YVel == 0) return false;
                CollideY(data.Distance, data.CollidedWith);
            }
            else
            {
                if (XVel == 0) return false;
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

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("DisappearAnimation", new SpriteProperty("DisappearAnimation", () => DisappearAnimation.Name, (t, g) => DisappearAnimation = Texture.AnimationFromName((string)t), "Disappear", SpriteProperty.Types.Animation, "The animation displayed as the platform is disappearing."));
                ret.Add("Disappear", new SpriteProperty("Disappear", () => CanDisappear, (t, g) => CanDisappear = (bool)t, false, SpriteProperty.Types.Bool, "Whether the platform disappears or not."));
                ret.Add("Conveyor", new SpriteProperty("Conveyor", () => Conveyor, (t, g) => Conveyor = (float)t, 0f, SpriteProperty.Types.Float, "The speed of the conveyor belt."));
                ret.Add("SingleDirection", new SpriteProperty("SingleDirection", () => SingleDirection, (t, g) => SingleDirection = (bool)t, true, SpriteProperty.Types.Bool, "Whether the bottom of the conveyor pushes the same way or not."));
                ret.Add("XSpeed", new SpriteProperty("XSpeed", () => XVel, (t, g) => XVel = (float)t, 0f, SpriteProperty.Types.Float, "The X speed in pixels/frame of the platform."));
                ret.Add("YSpeed", new SpriteProperty("YSpeed", () => YVel, (t, g) => YVel = (float)t, 0f, SpriteProperty.Types.Float, "The Y speed in pixels/frame of the platform."));
                ret.Add("BoundsX", new SpriteProperty("BoundsX", () => _bounds.X, (t, g) => _bounds.X = (int)t, 0, SpriteProperty.Types.Int, "The left edge of the platform's bounds."));
                ret.Add("BoundsY", new SpriteProperty("BoundsY", () => _bounds.Y, (t, g) => _bounds.Y = (int)t, 0, SpriteProperty.Types.Int, "The top edge of the platform's bounds."));
                ret.Add("BoundsWidth", new SpriteProperty("BoundsWidth", () => _bounds.Width, (t, g) => _bounds.Width = (int)t, 0, SpriteProperty.Types.Int, "The width of the platform's bounds."));
                ret.Add("BoundsHeight", new SpriteProperty("BoundsHeight", () => _bounds.Height, (t, g) => _bounds.Height = (int)t, 0, SpriteProperty.Types.Int, "The height of the platform's bounds."));
                ret.Add("Length", new SpriteProperty("Length", () => Length, (t, g) => Length = (int)t, 4, SpriteProperty.Types.Int, "The length in tiles of the platform."));
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
