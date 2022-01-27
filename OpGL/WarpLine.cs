using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace V7
{
    class WarpLine : InstancedSprite
    {
        protected override bool AlwaysCollide => true;
        public PointF Offset;
        public bool Horizontal;
        public int Direction;
        private int _length;
        public int Length
        {
            get => _length;
            set
            {
                _length = value;
                bufferData = new float[_length * 4];
                SetBuffer();
            }
        }

        public void SetBuffer()
        {
            bufferData = new float[_length * 4];

            float curL = 0;
            int index = 0;
            while (index < bufferData.Length)
            {
                bufferData[index++] = Horizontal ? curL * Texture.TileSizeX : 0;
                bufferData[index++] = Horizontal ? 0 : curL * Texture.TileSizeY;
                bufferData[index++] = _length == 1 ? 1 : (curL == 0 ? 0 : (curL == _length - 1 ? 2 : 1));
                bufferData[index++] = 0;
                curL += 1;
            }
            instances = _length;
            updateBuffer = true;
        }

        public override float Width => Horizontal ? _length * Texture.TileSizeX : base.Width;
        public override float Height => Horizontal ? base.Height : _length * Texture.TileSizeY;

        public List<Sprite> Warping = new List<Sprite>();
        private List<int> warpingDatas = new List<int>();

        public WarpLine(float x, float y, Texture texture, Animation animation, int length, bool horizontal, float offsetX, float offsetY, int direction) : base(x, y, texture, animation)
        {
            Visible = false;
            Horizontal = horizontal;
            Length = length;
            Solid = SolidState.NonSolid;
            Immovable = true;
            Offset = new PointF(offsetX, offsetY);
            Direction = direction;
        }

        public override void Process()
        {
            for (int i = Warping.Count - 1; i >= 0; i--)
            {
                Sprite sprite = Warping[i];
                if (!sprite.IsOverlapping(this))
                {
                    bool warp;
                    if (!Horizontal)
                    {
                        if (warpingDatas[i] == -1)
                            warp = sprite.X >= Right;
                        else
                            warp = sprite.Right <= X;
                    }
                    else
                    {
                        if (warpingDatas[i] == -1)
                            warp = sprite.Y >= Bottom;
                        else
                            warp = sprite.Bottom <= Y;
                    }
                    if (warp)
                    {
                        sprite.X += Offset.X;
                        sprite.Y += Offset.Y;
                        sprite.PreviousX += Offset.X;
                        sprite.PreviousY += Offset.Y;
                    }
                    sprite.Offsets.Remove(Offset);
                    if (sprite.Offsets.Count == 0) sprite.MultiplePositions = false;
                    Warping.RemoveAt(i);
                    warpingDatas.RemoveAt(i);
                    if (Horizontal) sprite.IsWarpingV = false;
                    else sprite.IsWarpingH = false;
                }
            }
        }

        public override void Collide(CollisionData cd)
        {
            Sprite s = cd.CollidedWith;
            if (s is Enemy && s.Solid == SolidState.NonSolid) return;
            if (!Warping.Contains(s))
            {
                if (Horizontal && s.IsWarpingV) return;
                else if (!Horizontal && s.IsWarpingH) return;
                s.Offsets.Add(Offset);
                s.MultiplePositions = true;
                Warping.Add(s);
                if (Horizontal) s.IsWarpingV = true;
                else s.IsWarpingH = true;
                if (Direction == 0)
                {
                    warpingDatas.Add(Math.Sign(cd.Distance));
                }
                else
                {
                    warpingDatas.Add(-Math.Sign(Direction));
                }
            }
        }

        public override bool CollideWith(CollisionData data)
        {
            Collide(data);
            return true;
        }

        public override CollisionData TestCollision(Sprite testFor)
        {
            CollisionData ret = null;
            if (IsOverlapping(testFor))
                ret = GetCollisionData(testFor);
            return ret;
        }

        protected override CollisionData GetCollisionData(Sprite testFor)
        {
            for (int j = -1; j < testFor.Offsets.Count; j++)
            {
                float ofXO = j > -1 ? testFor.Offsets[j].X : 0;
                float ofYO = j > -1 ? testFor.Offsets[j].Y : 0;
                if (!testFor.Within(X, Y, Width, Height, ofXO, ofYO)) continue;
                // check for vertical collision first
                // top
                if (Math.Round(PreviousY + PreviousHeight, 4) <= Math.Round(testFor.PreviousY, 4) + ofYO)
                    return new CollisionData(true, Bottom - (testFor.Y + ofYO), testFor);
                // bottom
                else if (Math.Round(PreviousY, 4) >= Math.Round(testFor.PreviousY + testFor.Height + ofYO, 4))
                    return new CollisionData(true, Y - (testFor.Bottom + ofYO), testFor);
                // right
                else if (Math.Round(PreviousX + PreviousWidth, 4) <= Math.Round(testFor.PreviousX + ofXO, 4))
                    return new CollisionData(false, Right - (testFor.X + ofXO), testFor);
                // left
                else if (Math.Round(PreviousX, 4) >= Math.Round(testFor.PreviousX + testFor.Width + ofXO, 4))
                    return new CollisionData(false, X - (testFor.Right + ofXO), testFor);
                if (!testFor.MultiplePositions)
                    break;
            }
            return new CollisionData(!Horizontal, 0, testFor);
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "WarpLine");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Length", Length);
        //    ret.Add("Horizontal", Horizontal);
        //    ret.Add("OffsetX", Offset.X);
        //    ret.Add("OffsetY", Offset.Y);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("Horizontal", new SpriteProperty("Horizontal", () => Horizontal, (t, g) => Horizontal = (bool)t, true, SpriteProperty.Types.Bool, "Whether the warp line is horizontal or not."));
                ret.Add("Length", new SpriteProperty("Length", () => Length, (t, g) => Length = (int)t, 1, SpriteProperty.Types.Int, "The length in tiles of the warp line."));
                ret.Add("OffsetX", new SpriteProperty("OffsetX", () => Offset.X, (t, g) => Offset.X = (float)t, 0f, SpriteProperty.Types.Float, "The X offset where the warp line warps to."));
                ret.Add("OffsetY", new SpriteProperty("OffsetY", () => Offset.Y, (t, g) => Offset.Y = (float)t, 0f, SpriteProperty.Types.Float, "The Y offset where the warp line warps to."));
                ret.Add("Direction", new SpriteProperty("Direction", () => Direction, (t, g) => Direction = (int)t, 0, SpriteProperty.Types.Int, "-1 is for lines on the top and left, 1 is for lines on the bottom or right."));
                ret["Type"].GetValue = () => "WarpLine";
                return ret;
            }
        }
    }
}
