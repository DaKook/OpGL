using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace V7
{
    public class Enemy : Sprite, IBoundSprite, IMovingSprite
    {
        public float XVelocity { get; set; }
        public float YVelocity { get; set; }
        public bool IsPushable = true;
        private Rectangle _bounds;
        public Rectangle Bounds { get => _bounds; set => _bounds = value; }
        public Enemy(float x, float y, Texture texture, Animation animation, float xSpeed, float ySpeed, Color? color = null) : base(x, y, texture, animation)
        {
            XVelocity = xSpeed;
            YVelocity = ySpeed;
            KillCrewmen = true;
            Solid = SolidState.Entity;
            Color = color ?? Color.White;
            ColorModifier = AnimatedColor.Default;
        }

        public override void Process()
        {
            base.Process();
            X += XVelocity;
            Y += YVelocity;
            CheckBounds();
        }

        public void CheckBounds()
        {
            if (Bounds.Width > 0 && Bounds.Height > 0)
            {
                if (Right > Bounds.X + Bounds.Width + InitialX)
                {
                    Right = Bounds.X + Bounds.Width + InitialX;
                    XVelocity *= -1;
                }
                else if (X < Bounds.X + InitialX)
                {
                    X = Bounds.X + InitialX;
                    XVelocity *= -1;
                }
                else if (Bottom > Bounds.Y + Bounds.Height + InitialY)
                {
                    Bottom = Bounds.Y + Bounds.Height + InitialY;
                    YVelocity *= -1;
                }
                else if (Y < Bounds.Y + InitialY)
                {
                    Y = Bounds.Y + InitialY;
                    YVelocity *= -1;
                }
            }
        }

        public override void CollideX(double distance, Sprite collision)
        {
            if (XVelocity != 0 || IsPushable)
            {
                base.CollideX(distance, collision);
                XVelocity *= -1;
            }
            //CheckBounds();
        }

        public override void CollideY(double distance, Sprite collision)
        {
            if (YVelocity != 0 || IsPushable)
            {
                base.CollideY(distance, collision);
                if (Math.Sign(distance) == Math.Sign(YVelocity))
                    YVelocity *= -1;
            }
            //CheckBounds();
        }

        public override void Collide(CollisionData cd)
        {
            base.Collide(cd);
            if (!IsPushable)
                cd.CollidedWith.Collide(new CollisionData(cd.Vertical, -cd.Distance, this));
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "Enemy");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Animation", Animation.Name);
        //    ret.Add("XSpeed", XVel);
        //    ret.Add("YSpeed", YVel);
        //    ret.Add("Name", Name);
        //    ret.Add("Color", Color.ToArgb());
        //    ret.Add("BoundsX", Bounds.X);
        //    ret.Add("BoundsY", Bounds.Y);
        //    ret.Add("BoundsWidth", Bounds.Width);
        //    ret.Add("BoundsHeight", Bounds.Height);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("XSpeed", new SpriteProperty("XSpeed", () => XVelocity, (t, g) => XVelocity = (float)t, 0f, SpriteProperty.Types.Float, "The X speed in pixels/frame of the enemy."));
                ret.Add("YSpeed", new SpriteProperty("YSpeed", () => YVelocity, (t, g) => YVelocity = (float)t, 0f, SpriteProperty.Types.Float, "The Y speed in pixels/frame of the enemy."));
                ret.Add("BoundsX", new SpriteProperty("BoundsX", () => Bounds.X, (t, g) => _bounds.X = (int)t, 0, SpriteProperty.Types.Int, "The left edge of the enemy's bounds."));
                ret.Add("BoundsY", new SpriteProperty("BoundsY", () => Bounds.Y, (t, g) => _bounds.Y = (int)t, 0, SpriteProperty.Types.Int, "The top edge of the enemy's bounds."));
                ret.Add("BoundsWidth", new SpriteProperty("BoundsWidth", () => Bounds.Width, (t, g) => _bounds.Width = (int)t, 0, SpriteProperty.Types.Int, "The width of the enemy's bounds."));
                ret.Add("BoundsHeight", new SpriteProperty("BoundsHeight", () => Bounds.Height, (t, g) => _bounds.Height = (int)t, 0, SpriteProperty.Types.Int, "The height of the enemy's bounds."));
                ret["Type"].GetValue = () => "Enemy";
                return ret;
            }
        }
    }
}
