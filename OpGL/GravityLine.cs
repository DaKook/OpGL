using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace V7
{
    public class GravityLine : InstancedSprite, IBoundSprite
    {
        protected override bool AlwaysCollide => true;
        private List<Crewman> touching = new List<Crewman>();

        protected int length = 0;

        public float XVel { get; set; }
        public float YVel { get; set; }
        private Rectangle _bounds;
        public Rectangle Bounds { get => _bounds; set => _bounds = value; }
        public static SoundEffect Sound;

        public override float Width => Horizontal ? length * Texture.TileSizeX : base.Width;
        public override float Height => Horizontal ? base.Height : length * Texture.TileSizeY;

        public bool Horizontal;

        public int LengthTiles
        {
            get => length;
            set
            {
                length = value;
                SetBuffer();
            }
        }

        public GravityLine(float x, float y, Texture texture, Animation animation, bool horizontal, int lengthTiles) : base(x, y, texture, animation)
        {
            Horizontal = horizontal;
            LengthTiles = lengthTiles;
            Solid = SolidState.NonSolid;
        }

        private void SetBuffer()
        {
            bufferData = new float[length * 4];

            float curL = 0;
            int index = 0;
            while (index < bufferData.Length)
            {
                bufferData[index++] = Horizontal ? curL * Texture.TileSizeX : 0;
                bufferData[index++] = Horizontal ? 0 : curL * Texture.TileSizeY;
                bufferData[index++] = length == 1 ? 1 : (curL == 0 ? 0 : (curL == length - 1 ? 2 : 1));
                bufferData[index++] = 0;
                curL += 1;
            }

            updateBuffer = true;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (!touching.Contains(crewman))
            {
                Sound?.Play();
                touching.Add(crewman);
                crewman.Gravity *= -1;
                crewman.YVelocity = 0;
                Color = Color.Gray;
            }
        }

        public override void Process()
        {
            base.Process();
            for (int i = 0; i < touching.Count; i++)
            {
                if (!IsOverlapping(touching[i]))
                {
                    touching.RemoveAt(i);
                    if (touching.Count == 0)
                        Color = Color.White;
                }
            }
            X += XVel;
            Y += YVel;
            CheckBounds();
        }

        public void CheckBounds()
        {
            if (_bounds.Width > 0 && _bounds.Height > 0)
            {
                if (Right % Room.ROOM_WIDTH > _bounds.X + _bounds.Width)
                {
                    float x = Right % Room.ROOM_WIDTH - (_bounds.X + _bounds.Width);
                    X -= x;
                    XVel *= -1;
                }
                else if (X % Room.ROOM_WIDTH < _bounds.X)
                {
                    float x = X % Room.ROOM_WIDTH - _bounds.X;
                    X -= x;
                    XVel *= -1;
                }
                else if (Bottom % Room.ROOM_HEIGHT > _bounds.Y + _bounds.Height)
                {
                    float y = Bottom % Room.ROOM_HEIGHT - (_bounds.Y + _bounds.Height);
                    Y -= y;
                    YVel *= -1;
                }
                else if (Y % Room.ROOM_HEIGHT < _bounds.Y)
                {
                    float y = Y % Room.ROOM_HEIGHT - _bounds.Y;
                    Y -= y;
                    YVel *= -1;
                }
            }
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "GravityLine");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Horizontal", Horizontal);
        //    ret.Add("Length", LengthTiles);
        //    ret.Add("Animation", Animation.Name);
        //    ret.Add("XSpeed", XSpeed);
        //    ret.Add("YSpeed", YSpeed);
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
                ret.Add("Horizontal", new SpriteProperty("Horizontal", () => Horizontal, (t, g) => Horizontal = (bool)t, true, SpriteProperty.Types.Bool, "Whether the gravity line is horizontal or not."));
                ret.Add("Length", new SpriteProperty("Length", () => LengthTiles, (t, g) => LengthTiles = (int)t, 1, SpriteProperty.Types.Int, "The length in tiles of the gravity line."));
                ret.Add("XSpeed", new SpriteProperty("XSpeed", () => XVel, (t, g) => XVel = (float)t, 0f, SpriteProperty.Types.Float, "The X speed in pixels/frame of the gravity line."));
                ret.Add("YSpeed", new SpriteProperty("YSpeed", () => YVel, (t, g) => YVel = (float)t, 0f, SpriteProperty.Types.Float, "The Y speed in pixels/frame of the gravity line."));
                ret.Add("BoundsX", new SpriteProperty("BoundsX", () => _bounds.X, (t, g) => _bounds.X = (int)t, 0, SpriteProperty.Types.Int, "The left edge of the gravity line's bounds."));
                ret.Add("BoundsY", new SpriteProperty("BoundsY", () => _bounds.Y, (t, g) => _bounds.Y = (int)t, 0, SpriteProperty.Types.Int, "The top edge of the gravity line's bounds."));
                ret.Add("BoundsWidth", new SpriteProperty("BoundsWidth", () => _bounds.Width, (t, g) => _bounds.Width = (int)t, 0, SpriteProperty.Types.Int, "The width of the gravity line's bounds."));
                ret.Add("BoundsHeight", new SpriteProperty("BoundsHeight", () => _bounds.Height, (t, g) => _bounds.Height = (int)t, 0, SpriteProperty.Types.Int, "The height of the gravity line's bounds."));
                ret["Type"].GetValue = () => "GravityLine";
                return ret;
            }
        }
    }
}
