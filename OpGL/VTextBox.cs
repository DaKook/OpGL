using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace OpGL
{
    public class VTextBox : Drawable
    {
        private StringDrawable stringDraw;
        public string Text { get => stringDraw.Text; }
        private int boxTiles;

        private float[] bufferData;
        private uint ibo;
        private bool updateBuffer = true;
        private bool firstRender = true;

        internal override uint VAO { get; set; }

        public VTextBox(float x, float y, Texture boxTexture, Texture textTexture, Color color, string text) : base(x, y, boxTexture, 0, 0)
        {
            Texture = boxTexture;
            Color = color;
            if (textTexture.TileSize != boxTexture.TileSize) return;
            stringDraw = new StringDrawable(x + 8, y + 8, textTexture, text, color);
            int w = stringDraw.GetWidth() / boxTexture.TileSize + 2;
            int h = stringDraw.GetHeight() / boxTexture.TileSize + 2;
            bufferData = new float[w * h * 4];
            int index = 0;
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    bufferData[index++] = i * boxTexture.TileSize;
                    bufferData[index++] = j * boxTexture.TileSize;
                    bufferData[index++] = i == 0 ? 0 : (i == w - 1 ? 2 : 1);
                    bufferData[index++] = j == 0 ? 0 : (j == h - 1 ? 2 : 1);
                }
            }
            boxTiles = index / 4;
        }
        internal override void UnsafeDraw()
        {
            //if (updateBuffer)
                UpdateBuffer();

            Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, boxTiles);
            stringDraw.SafeDraw();
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
    }
}
