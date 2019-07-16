using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class StringDrawable : Drawable
    {
        private Drawable[] characters;
        private string _Text;
        public string Text
        {
            get => _Text;
            set
            {
                _Text = value;
                if (characters.Length < _Text.Length)
                    Array.Resize(ref characters, _Text.Length);

                float curX = X;
                float curY = Y;
                int drawableID = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    int c = value[i];
                    if (c == '\n')
                    {
                        curX = X;
                        curY += Texture.TileSize;
                    }
                    else if (c != '\r')
                    {
                        int x = c % 16;
                        int y = (c - x) / 16;
                        Drawable d = new Drawable(curX, curY, Texture, x, y, Color);
                        characters[drawableID] = d;
                        drawableID++;
                        curX += Texture.TileSize;
                    }
                }

                Array.Resize(ref characters, drawableID);
            }
        }
        public StringDrawable(float x, float y, Texture texture, string text, Color? color = null)
        {
            if (texture.Width / texture.TileSize != 16 || texture.Height / texture.TileSize != 16)
                throw new InvalidOperationException("A StringDrawable's texture must be 16x16 tiles.");

            X = x;
            Y = y;
            Texture = texture;
            Color = color ?? Color.White;

            characters = new Drawable[text.Length];
            Text = text;
        }

        public override void Draw()
        {
            foreach (Drawable chr in characters)
                chr.Draw();
        }

        public override void Process()
        {
            // do nothing
        }
    }
}
