using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace V7
{
    public class ProgramData
    {
        public int ID;
        public int MasterColorLocation;
        public int ColorLocation;
        public int ModelLocation;
        public int IsTextureLocation;

        //private int lastColor;
        private int isTexture = -1;

        public virtual void Reset()
        {
            isTexture = -1;
        }

        public ProgramData(int id)
        {
            ID = id;
            ModelLocation = GL.GetUniformLocation(id, "model");
            ColorLocation = GL.GetUniformLocation(id, "color");
            IsTextureLocation = GL.GetUniformLocation(id, "isTexture");
            MasterColorLocation = GL.GetUniformLocation(id, "masterColor");
        }

        public virtual void Prepare(Sprite sprite, int frame)
        {
            GL.UniformMatrix4(ModelLocation, false, ref sprite.LocMatrix);
            GL.BindVertexArray(sprite.VAO);
            if (sprite.Texture is null && isTexture != 0)
            {
                GL.Uniform1(IsTextureLocation, 0);
                isTexture = 0;
            }
            else if (sprite.Texture is object && isTexture != 1)
            {
                GL.Uniform1(IsTextureLocation, 1);
                isTexture = 1;
            }
            //if (sprite.Color.ToArgb() != lastColor || sprite.ColorModifier is object)
            {
                //lastColor = sprite.Color.ToArgb();
                float r = (float)sprite.Color.R / 255;
                float g = (float)sprite.Color.G / 255;
                float b = (float)sprite.Color.B / 255;
                float a = (float)sprite.Color.A / 255;
                if (sprite.ColorModifier is object)
                {
                    Color c = sprite.ColorModifier.GetFrame(frame);
                    r *= (float)c.R / 255;
                    g *= (float)c.G / 255;
                    b *= (float)c.B / 255;
                    a *= (float)c.A / 255;
                }
                GL.Uniform4(ColorLocation, new Vector4(r, g, b, a));
            }
        }
    }

    public class VectorProgram : ProgramData
    {
        public VectorProgram(int id) : base(id)
        {
        }
    }

    public class TextureProgram : ProgramData
    {
        public int TexLocation;

        private Texture lastTex;
        public override void Reset()
        {
            base.Reset();
            lastTex = null;
        }

        public TextureProgram(int id) : base(id)
        {
            TexLocation = GL.GetUniformLocation(id, "texMatrix");
        }

        public override void Prepare(Sprite sprite, int frame)
        {
            base.Prepare(sprite, frame);
            if (sprite.Texture != null && lastTex != sprite.Texture)
            {
                lastTex = sprite.Texture;
                GL.BindTexture(TextureTarget.Texture2D, lastTex.ID);
            }
            GL.UniformMatrix4(ModelLocation, false, ref sprite.LocMatrix);
            GL.UniformMatrix4(TexLocation, false, ref sprite.TexMatrix);
        }
    }
}
