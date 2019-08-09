using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace OpGL
{
    public class VTextBox : StringDrawable
    {
        private float appearSpeed;
        private int height;
        private int width;
        public override float Width => width;
        public override float Height => height;

        public delegate void DisappearedDelegate();
        public event DisappearedDelegate Disappeared;

        public override string Text
        {
            get => _Text;
            set
            {
                _Text = value;
                float[] textData = new float[_Text.Length * 4];
                float curX = Texture.TileSize, curY = Texture.TileSize;
                int index = 0;
                h = Texture.TileSize;
                //Text
                for (int i = 0; i < _Text.Length; i++)
                {
                    int c = _Text[i];
                    if (c == '\n')
                    {
                        curX = Texture.TileSize;
                        if (curY + Texture.TileSize > h) h = (int)curY + Texture.TileSize;
                        curY += Texture.TileSize;
                    }
                    else if (c != '\r')
                    {
                        int x = c % 16;
                        int y = (c - x) / 16;
                        textData[index++] = curX;
                        textData[index++] = curY;
                        textData[index++] = x;
                        textData[index++] = y;
                        if (curX + Texture.TileSize > w) w = (int)curX;
                        curX += Texture.TileSize;
                    }
                }
                visibleCharacters = index / 4;
                Array.Resize(ref textData, index);
                index = 0;
                //Box
                int wch = w / Texture.TileSize;
                int hch = h / Texture.TileSize;
                width = w + 2 * Texture.TileSize;
                height = h + 2 * Texture.TileSize;

                float[] boxData = new float[(wch + 2) * (hch + 2) * 4];

                for (int i = 0; i < hch + 2; i++)
                {
                    for (int j = 0; j < wch + 2; j++)
                    {
                        boxData[index++] = j * Texture.TileSize;
                        boxData[index++] = i * Texture.TileSize;
                        int tx = 0;
                        if (j > 0) tx += 1;
                        if (j == wch + 1) tx += 1;
                        if (i > 0) tx += 3;
                        if (i == hch + 1) tx += 3;

                        boxData[index++] = tx;
                        boxData[index++] = 0;
                    }
                }
                visibleCharacters += index / 4;

                bufferData = new float[boxData.Length + textData.Length];
                boxData.CopyTo(bufferData, 0);
                textData.CopyTo(bufferData, boxData.Length);
            }
        }

        public VTextBox(float x, float y, Texture texture, string text, Color color) : base(x, y, texture, text, color)
        {
            Visible = false;
            Color = Color.FromArgb(0, Color);
        }

        public void Appear(int speed = 51)
        {
            appearSpeed = speed;
        }

        public void Disappear(int speed = 51)
        {
            appearSpeed = -speed;
        }

        public override void Process()
        {
            if (appearSpeed > 0 && Color.A < 255)
            {
                Visible = true;
                Color = Color.FromArgb((int)Math.Min(Color.A + appearSpeed, 255), Color.R, Color.G, Color.B);
            }
            else if (appearSpeed < 0 && Color.A > 0)
            {
                Color = Color.FromArgb((int)Math.Max(Color.A + appearSpeed, 0), Color.R, Color.G, Color.B);
                if (Color.A == 0) Visible = false;

            }
        }
    }
}
