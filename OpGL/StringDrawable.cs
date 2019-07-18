using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenGL;

namespace OpGL
{
    public class StringDrawable : Drawable
    {
        private int visibleCharacters = 0;

        private float[] bufferData;
        private string _Text;
        public string Text
        {
            get => _Text;
            set
            {
                _Text = value;
                bufferData = new float[_Text.Length * 4];
                float curX = 0, curY = 0;
                int index = 0;
                for (int i = 0; i < _Text.Length; i++)
                {
                    int c = _Text[i];
                    if (c == '\n')
                    {
                        curX = 0;
                        curY += Texture.TileSize;
                    }
                    else if (c != '\r')
                    {
                        int x = c % 16;
                        int y = (c - x) / 16;
                        bufferData[index++] = curX;
                        bufferData[index++] = curY;
                        bufferData[index++] = x * Texture.TileSize;
                        bufferData[index++] = y * Texture.TileSize;
                        curX += Texture.TileSize;
                    }
                }
                visibleCharacters = index / 4;
                Array.Resize(ref bufferData, index);
            }
        }
        public StringDrawable(float x, float y, Texture texture, string text, Color? color = null) : base(x, y, texture, 0, 0)
        {
            if (texture.Width / texture.TileSize != 16 || texture.Height / texture.TileSize != 16)
                throw new InvalidOperationException("A StringDrawable's texture must be 16x16 tiles.");

            Color = color ?? Color.White;

            Text = text;
        }

        public override void Draw()
        {
            if (!Visible) return;
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(Texture.VAO);

            int modelLoc = Gl.GetUniformLocation(Texture.Program, "model");
            Gl.UniformMatrix4f(modelLoc, 1, false, LocMatrix);
            int texLoc = Gl.GetUniformLocation(Texture.Program, "texMatrix");
            Gl.UniformMatrix4f(texLoc, 1, false, TexMatrix);
            int colorLoc = Gl.GetUniformLocation(Texture.Program, "color");
            Gl.Uniform4f(colorLoc, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));

            Gl.BindBuffer(BufferTarget.ArrayBuffer, Texture.IBO);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)bufferData.Length * sizeof(float), bufferData, BufferUsage.DynamicDraw);
            Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, visibleCharacters);
        }

        public override void Process()
        {
            // do nothing
        }
    }
}
