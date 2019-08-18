using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                            warp = sprite.X > Right;
                        else
                            warp = sprite.Right < X;
                    }
                    else
                    {
                        if (warpingDatas[i] == -1)
                            warp = sprite.Y > Bottom;
                        else
                            warp = sprite.Bottom < Y;
                    }
                    if (warp)
                    {
                        sprite.X += Offset.X;
                        sprite.Y += Offset.Y;
                    }
                    sprite.Offsets.Remove(Offset);
                    if (sprite.Offsets.Count == 0) sprite.MultiplePositions = false;
                    Warping.RemoveAt(i);
                    warpingDatas.RemoveAt(i);
                    sprite.IsWarping -= 1;
                }
            }
        }

        public override void Collide(CollisionData cd)
        {
            Sprite s = cd.CollidedWith;
            s.Offsets.Add(Offset);
            s.MultiplePositions = true;
            Warping.Add(s);
            s.IsWarping += 1;
            warpingDatas.Add(Math.Sign(cd.Distance));
        }

        public override CollisionData TestCollision(Sprite testFor)
        {
            CollisionData ret = null;
            if (IsOverlapping(testFor))
                ret = GetCollisionData(testFor);
            return ret;
        }

        public override uint TextureID => 0;
    }
}
