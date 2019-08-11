using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Tile : Sprite
    {
        public Tile(int x, int y, Texture texture, int tileX, int tileY) : base(x, y, texture, tileX, tileY)
        {
            if (Texture.TileSolidStates.Length > 0)
            {
                int ss = (int)Texture.TileSolidStates[tileX, tileY];
                if (ss > (int)SolidState.NonSolid)
                {
                    ss -= (int)SolidState.NonSolid + 1;
                    KillCrewmen = true;
                }
                Solid = (SolidState)ss;
            }
            Static = true;
        }
    }
}
