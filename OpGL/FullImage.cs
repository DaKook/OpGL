using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace OpGL
{
    public class FullImage : Sprite
    {
        public float Size = 1;

        public override float Width => base.Width * Size;
        public override float Height => base.Height * Size;

        public FullImage(float x, float y, Texture texture)
        {
            X = x;
            Y = y;
            Texture = texture;
            Animation = new Animation(new Point[] { new Point(0, 0) }, new Rectangle(0, 0, (int)texture.Width, (int)texture.Height), texture);
            Solid = SolidState.NonSolid;
            Static = true;
            TexMatrix = Matrix4x4f.Identity;
        }

        internal override void RenderPrep()
        {
            LocMatrix = Matrix4x4f.Translated((int)X, (int)Y, 0);
            LocMatrix.Scale(Texture.Width * Size / Texture.TileSize, Texture.Height * Size / Texture.TileSize, 1);
            if (flipX)
            {
                LocMatrix.Scale(-1, 1, 1);
            }
            if (flipY)
            {
                LocMatrix.Scale(1, -1, 1);
            }
        }
    }
}
