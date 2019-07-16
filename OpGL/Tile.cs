using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Tile : Drawable
    {
        public Tile(int x, int y, Texture texture, int tileX, int tileY)
        {
            X = x;
            Y = y;
            Texture = texture;
            Animation = new Animation(new Point[] { new Point(tileX, tileY) }, Rectangle.Empty, Texture);
            if (Texture.TileSolidStates.Length > 0)
            {
                Solid = Texture.TileSolidStates[tileX, tileY];
            }
            Static = true;
        }
    }
}
