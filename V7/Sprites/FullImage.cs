using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Mathematics;

namespace V7
{
    public class FullImage : Sprite
    {

        public override float Width => Texture.Width * Size;
        public override float Height => Texture.Height * Size;

        public FullImage(float x, float y, Texture texture) :
            base(x, y, texture, new Animation(new Point[] { new Point(0, 0) }, new Rectangle(0, 0, (int)texture.Width, (int)texture.Height), texture))
        {
            Solid = SolidState.NonSolid;
            Static = true;
            TexMatrix = Matrix4.Identity;
        }

        public override void Process()
        {
            //Do nothing
        }

        public override void ChangeTexture(Texture texture)
        {
            base.ChangeTexture(texture);
            TexMatrix = Matrix4.Identity;
        }

        public override void RenderPrep()
        {
            LocMatrix = Matrix4.CreateTranslation((int)X, (int)Y, 0);
            LocMatrix *= Matrix4.CreateScale(Texture.Width * Size / Texture.TileSizeX, Texture.Height * Size / Texture.TileSizeY, 1);
            if (flipX)
            {
                LocMatrix *= Matrix4.CreateScale(-1, 1, 1);
            }
            if (flipY)
            {
                LocMatrix *= Matrix4.CreateScale(1, -1, 1);
            }
        }
    }
}
