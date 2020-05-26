using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class RectangleSprite : Sprite
    {
        public static ProgramData BaseProgram;
        public static uint BaseVAO;
        public static uint BaseVBO;
        public override uint VAO { get => BaseVAO; set { } }
        public override uint TextureID => 0;
        public override ProgramData Program => BaseProgram;

        private float w;
        private float h;
        public override float Width => w;
        public override float Height => h;

        public void SetWidth(float width)
        {
            w = width;
        }
        public void SetHeight(float height)
        {
            h = height;
        }
        public void SetSize(float width, float height)
        {
            w = width;
            h = height;
        }

        public RectangleSprite(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;
            SetSize(width, height);
            PreviousWidth = width;
            PreviousHeight = height;
        }

        public override void Process()
        {
            //do nothing
        }

        public override void RenderPrep()
        {
            LocMatrix = Matrix4x4f.Translated(X, Y, 0);
            LocMatrix.Scale(w, h, 1);
        }
    }
}
