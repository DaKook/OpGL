using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class BoxSprite : InstancedSprite
    {
        protected int w;
        protected int h;

        public override float Width => w * Texture.TileSizeX;
        public override float Height => h * Texture.TileSizeY;
        public int WidthTiles => w;
        public int HeightTiles => h;

        public void SetWidth(int tiles)
        {
            w = tiles;
            SetBuffer();
        }

        public void SetHeight(int tiles)
        {
            h = tiles;
            SetBuffer();
        }

        public void SetSize(int widthTiles, int heightTiles)
        {
            w = widthTiles;
            h = heightTiles;
            SetBuffer();
        }
        private void SetBuffer()
        {
            bufferData = new float[w * h * 4];
            float curX = 0, curY = 0;
            int index = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bufferData[index++] = curX;
                    bufferData[index++] = curY;
                    bufferData[index++] = x == 0 ? (w == 1 ? 0 : 1) : (x == w - 1 ? 3 : 2);
                    bufferData[index++] = y == 0 ? (h == 1 ? 0 : 1) : (y == h - 1 ? 3 : 2);
                    curX += Texture.TileSizeX;
                }
                curX = 0;
                curY += Texture.TileSizeY;
            }
            instances = index / 4;
            Array.Resize(ref bufferData, index);
            if (curX > w) w = (int)curX;

            updateBuffer = true;
        }

        public BoxSprite(float x, float y, Texture texture, int widthTiles, int heightTiles, Color? color = null) : base(x, y, texture)
        {
            if (texture.Width / texture.TileSizeX != 4 || texture.Height / texture.TileSizeY != 4)
                throw new InvalidOperationException("A BoxSprite's texture must be 4x4 tiles.");

            Color = color ?? Color.White;

            w = widthTiles;
            h = heightTiles;
            SetBuffer();
        }

        public override void Process()
        {
            // do nothing
        }
    }
}
