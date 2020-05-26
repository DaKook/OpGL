using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace OpGL
{
    public class ProgramData
    {
        public uint ID;
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

        public ProgramData(uint id)
        {
            ID = id;
            ModelLocation = Gl.GetUniformLocation(id, "model");
            ColorLocation = Gl.GetUniformLocation(id, "color");
            IsTextureLocation = Gl.GetUniformLocation(id, "isTexture");
            MasterColorLocation = Gl.GetUniformLocation(id, "masterColor");
        }

        public virtual void Prepare(Sprite sprite, int frame)
        {
            Gl.UniformMatrix4f(ModelLocation, 1, false, sprite.LocMatrix);
            Gl.BindVertexArray(sprite.VAO);
            if (sprite.Texture is null && isTexture != 0)
            {
                Gl.Uniform1(IsTextureLocation, 0);
                isTexture = 0;
            }
            else if (sprite.Texture is object && isTexture != 1)
            {
                Gl.Uniform1(IsTextureLocation, 1);
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
                Gl.Uniform4f(ColorLocation, 1, new Vertex4f(r, g, b, a));
            }
        }
    }

    public class VectorProgram : ProgramData
    {
        public VectorProgram(uint id) : base(id)
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

        public TextureProgram(uint id) : base(id)
        {
            TexLocation = Gl.GetUniformLocation(id, "texMatrix");
        }

        public override void Prepare(Sprite sprite, int frame)
        {
            base.Prepare(sprite, frame);
            if (sprite.Texture != null && lastTex != sprite.Texture)
            {
                lastTex = sprite.Texture;
                Gl.BindTexture(TextureTarget.Texture2d, lastTex.ID);
            }
            Gl.UniformMatrix4f(ModelLocation, 1, false, sprite.LocMatrix);
            Gl.UniformMatrix4f(TexLocation, 1, false, sprite.TexMatrix);
        }
    }
}
