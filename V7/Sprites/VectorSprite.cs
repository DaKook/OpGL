using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class RectangleSprite : Sprite
    {
        public static ProgramData BaseProgram;
        public static int BaseVAO;
        public static int BaseVBO;
        public override int VAO { get => BaseVAO; set { } }
        public override int TextureID => 0;
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
        public virtual void SetSize(float width, float height)
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
            LocMatrix = Matrix4.CreateTranslation(X, Y, 0);
            LocMatrix = Matrix4.CreateScale(w, h, 1) * LocMatrix;
        }
    }
}
