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
                if (Texture.Width / Texture.TileSize != 16 || Texture.Height / Texture.TileSize != 16) return;
                characters = new Drawable[value.Length];
                float curX = X;
                float curY = Y;
                for (int i = 0; i < value.Length; i++)
                {
                    int c = value[i];
                    int x = c % 16;
                    int y = (c - x) / 16;
                    if (c == '\r')
                    {
                        
                    }
                    else if (c == '\n')
                    {
                        curX = X;
                        curY += Texture.TileSize;
                    }
                    else
                    {
                        Drawable d = new Drawable(curX, curY, Texture, x, y, Color);
                        characters[i] = d;
                        curX += Texture.TileSize;
                    }

                }
            }
        }
        public StringDrawable(float x, float y, Texture texture, string text, Color? color = null)
        {
            X = x;
            Y = y;
            Texture = texture;
            Color = color ?? Color.White;
            Text = text;
        }

        public override void Draw()
        {
            foreach (Drawable chr in characters)
            {
                if (chr != null)
                    chr.Draw();
            }
        }
    }
}
