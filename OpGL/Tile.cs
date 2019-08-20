using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    public class Tile : Sprite
    {
        public string Tag;
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
            Layer = -2;
        }

        public override JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "Tile");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            ret.Add("TileX", TextureX);
            ret.Add("TileY", TextureY);
            return ret;
        }
    }
}
