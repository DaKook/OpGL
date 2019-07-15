using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenGL;

namespace OpGL
{
    public class Drawable
    {
        public Animation Animation { get; set; } = Animation.EmptyAnimation;
        private float _X;
        public float X
        {
            get { return _X; }
            set
            {
                LocMatrix.Translate(value - _X, 0f, 0f);
                _X = value;
            }
        }
        private float _Y;
        public float Y
        {
            get { return _Y; }
            set
            {
                LocMatrix.Translate(0f, value - _Y, 0f);
                _Y = value;
            }
        }
        private float _TexX;
        public float TextureX
        {
            get
            {
                return _TexX;
            }
            set
            {
                TexMatrix.Translate((value - _TexX) * Texture.TileSize, 0, 0);
                _TexX = value;
            }
        }
        private float _TexY;
        public float TextureY
        {
            get
            {
                return _TexY;
            }
            set
            {
                TexMatrix.Translate(0, (value - _TexY) * Texture.TileSize, 0);
                _TexY = value;
            }
        }

        public bool Visible { get; set; } = true;
        public readonly Texture Texture;

        private Matrix4x4f LocMatrix = Matrix4x4f.Identity;
        private Matrix4x4f TexMatrix = Matrix4x4f.Identity;

        public Drawable(float x, float y, Texture texture, float texX, float texY)
        {
            _X = x;
            _Y = y;
            LocMatrix = Matrix4x4f.Translated(x, y, 0);
            Texture = texture;
            float dw = 1f / texture.Width;
            float dh = 1f / texture.Height;
            TexMatrix.Scale(dw, dh, 1f);
            TextureX = texX;
            TextureY = texY;
            Animation.Frames = new Point[] { new Point((int)texX, (int)texY) };
        }

        public Drawable(float x, float y, Texture texture, Animation animation)
        {
            X = x;
            Y = y;
            Texture = texture;
            Animation = animation;
        }

        public void Draw()
        {
            if (!Visible) return;
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(Texture.VAO);

            int modelLoc = Gl.GetUniformLocation(Texture.Program, "model");
            Gl.UniformMatrix4f(modelLoc, 1, false, LocMatrix);
            int texLoc = Gl.GetUniformLocation(Texture.Program, "texMatrix");
            Gl.UniformMatrix4f(texLoc, 1, false, TexMatrix);
            
            Gl.DrawArrays(PrimitiveType.Polygon, 0, 4);
        }

        public virtual void Process()
        {
            Animation.AdvanceFrame();
            TextureX = Animation.CurrentFrame.X;
            TextureY = Animation.CurrentFrame.Y;
        }
    }

}
