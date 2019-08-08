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

        private int w;
        private int h;
        public int GetWidth() => w;
        public int GetHeight() => h;

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
                        if (curY + Texture.TileSize > h) h = (int)curY + Texture.TileSize;
                    }
                    else if (c != '\r')
                    {
                        int x = c % 16;
                        int y = (c - x) / 16;
                        bufferData[index++] = curX;
                        bufferData[index++] = curY;
                        bufferData[index++] = x;
                        bufferData[index++] = y;
                        curX += Texture.TileSize;
                        if (curX + Texture.TileSize > w) w = (int)curX + Texture.TileSize;
                    }
                }
                visibleCharacters = index / 4;
                Array.Resize(ref bufferData, index);
            }
        }

        private float[] bufferData;
        private uint ibo;
        private bool updateBuffer = true;
        private bool firstRender = true;

        internal override uint VAO { get; set; }

        public StringDrawable(float x, float y, Texture texture, string text, Color? color = null) : base(x, y, texture, 0, 0)
        {
            if (texture.Width / texture.TileSize != 16 || texture.Height / texture.TileSize != 16)
                throw new InvalidOperationException("A StringDrawable's texture must be 16x16 tiles.");

            Color = color ?? Color.White;

            Text = text;
        }

        /// <summary>
        /// Performs OpenGL bindings and uniform gets/updates before drawing.
        /// </summary>
        public override void SafeDraw()
        {
            if (!Visible) return;
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(VAO);

            int modelLoc = Gl.GetUniformLocation(Texture.Program, "model");
            Gl.UniformMatrix4f(modelLoc, 1, false, LocMatrix);
            int texLoc = Gl.GetUniformLocation(Texture.Program, "texMatrix");
            Gl.UniformMatrix4f(texLoc, 1, false, TexMatrix);
            int colorLoc = Gl.GetUniformLocation(Texture.Program, "color");
            Gl.Uniform4f(colorLoc, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));

            UnsafeDraw();
        }
        // Just the render call and any set-up StringDrawable requires but a regular Drawable doesn't.
        internal override void UnsafeDraw()
        {
            //if (updateBuffer)
                UpdateBuffer();

            Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, visibleCharacters);
        }

        private void UpdateBuffer()
        {
            if (firstRender)
            {
                firstRender = false;

                VAO = Gl.CreateVertexArray();
                Gl.BindVertexArray(VAO);

                Gl.BindBuffer(BufferTarget.ArrayBuffer, Texture.baseVBO);
                Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
                Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);

                ibo = Gl.CreateBuffer();
                Gl.BindBuffer(BufferTarget.ArrayBuffer, ibo);
                Gl.VertexAttribPointer(2, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
                Gl.VertexAttribPointer(3, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                Gl.EnableVertexAttribArray(2);
                Gl.EnableVertexAttribArray(3);
                Gl.VertexAttribDivisor(2, 1);
                Gl.VertexAttribDivisor(3, 1);
            }

            Gl.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)bufferData.Length * sizeof(float), bufferData, BufferUsage.DynamicDraw);
        }

        public override void Process()
        {
            // do nothing
        }
    }
}
