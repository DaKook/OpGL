using System;
using System.Collections.Generic;
//using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace V7
{
    public class VTextBox : StringDrawable
    {
        private float appearSpeed;

        public int frames = -1;

        int tilesW;
        int tilesH;

        public delegate void DisappearedDelegate(VTextBox textBox);
        public event DisappearedDelegate Disappeared;

        public override string Text
        {
            get => _Text;
            set
            {
                base.Text = value;

                // box
                w = (int)Math.Ceiling((float)w / Texture.TileSizeX) * Texture.TileSizeX;
                w += 2 * Texture.TileSizeX;
                h += 2 * Texture.TileSizeY;
                tilesW = w / Texture.TileSizeX;
                if (tilesW == 2) tilesW = 3;
                tilesH = h / Texture.TileSizeY;
                // textbox must be rendered first
                int stringLength = bufferData.Length;
                int boxLength = tilesW * tilesH * 4;
                Array.Resize(ref bufferData, bufferData.Length + boxLength);
                Array.Copy(bufferData, 0, bufferData, boxLength, stringLength);
                // this is used to determine how many instances to draw
                instances = bufferData.Length / 4;

                int index = 0;
                for (int i = -1; i < tilesH - 1; i++)
                {
                    for (int j = -1; j < tilesW - 1; j++)
                    {
                        bufferData[index++] = j * Texture.TileSizeX;
                        bufferData[index++] = i * Texture.TileSizeY;
                        //       ((j + tilesW - 2) / (tilesW - 2)) = 0 on first loop, 2 on last, 1 on others
                        int tx = ((j + tilesW - 2) / (tilesW - 2)) + ((i + tilesH - 2) / (tilesH - 2) * 3);
                        bufferData[index++] = tx + BaseStyle * 16;
                        bufferData[index++] = 0;
                    }
                }

                updateBuffer = true;
            }
        }

        public override void AlignToCenter()
        {
            int resetW = w;
            w -= Texture.TileSizeX * 2;
            AlignToCenter(tilesW * tilesH * 4);
            w = resetW;
        }

        public VTextBox(float x, float y, FontTexture texture, string text, Color color) : base(x, y, texture, text, color)
        {
            Visible = false;
            Color = Color.FromArgb(0, Color.R, Color.G, Color.B);
        }

        public override void RenderPrep()
        {
            // Offset render location because the textbox top-left is -TileSize of location
            X += Texture.TileSizeX;
            Y += Texture.TileSizeY;
            base.RenderPrep();
            X -= Texture.TileSizeX;
            Y -= Texture.TileSizeY;
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
            if (frames > -1)
            {
                frames -= 1;
                if (frames <= -1)
                    Disappear();
            }
            if (appearSpeed > 0 && Color.A < 255)
            {
                Visible = true;
                Color = Color.FromArgb((int)Math.Min(Color.A + appearSpeed, 255), Color.R, Color.G, Color.B);
            }
            else if (appearSpeed < 0 && Color.A > 0)
            {
                Color = Color.FromArgb((int)Math.Max(Color.A + appearSpeed, 0), Color.R, Color.G, Color.B);
                if (Color.A == 0)
                {
                    Visible = false;
                    Disappeared?.Invoke(this);
                }
            }
        }
    }
}
