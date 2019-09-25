using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    class WarpLine : Sprite
    {
        public override uint VAO { get => 0; set { } }

        public PointF Offset;
        public bool Horizontal;
        public int Length;
        public override float Width => Horizontal ? Length : 1;
        public override float Height => Horizontal ? 1 : Length;

        public List<Sprite> Warping = new List<Sprite>();
        private List<int> warpingDatas = new List<int>();

        public WarpLine(float x, float y, int length, bool horizontal, float offsetX, float offsetY) : base(x, y, null, null)
        {
            Visible = false;
            Horizontal = horizontal;
            Length = length;
            Solid = SolidState.NonSolid;
            Immovable = true;
            Offset = new PointF(offsetX, offsetY);
        }

        public override void SafeDraw()
        {
            //do nothing
        }
        public override void UnsafeDraw()
        {
            //do nothing
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
            if (!Warping.Contains(s))
            {
                if (Horizontal && s.IsWarpingV) return;
                else if (!Horizontal && s.IsWarpingH) return;
                s.Offsets.Add(Offset);
                s.MultiplePositions = true;
                Warping.Add(s);
                if (Horizontal) s.IsWarpingV = true;
                else s.IsWarpingH = true;
                warpingDatas.Add(Math.Sign(cd.Distance));
            }
        }

        public override CollisionData TestCollision(Sprite testFor)
        {
            CollisionData ret = null;
            if (IsOverlapping(testFor))
                ret = GetCollisionData(testFor);
            return ret;
        }

        public override uint TextureID => 0;

        public override JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "WarpLine");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Length", Length);
            ret.Add("Horizontal", Horizontal);
            ret.Add("OffsetX", Offset.X);
            ret.Add("OffsetY", Offset.Y);
            return ret;
        }
    }
}
