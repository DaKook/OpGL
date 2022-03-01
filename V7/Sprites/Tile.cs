using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenTK;
using OpenTK.Mathematics;

namespace V7
{
    public class Tile : Sprite, ISolidObject
    {
        public override Pushability Pushability => Pushability.Immovable;
        public enum TileStates { Normal, SpikeR, SpikeL, SpikeU, SpikeD, OneWayU, OneWayD, OneWayR, OneWayL }
        public TileStates State { get; set; } = TileStates.Normal;
        public override float Width => 8;
        public override float Height => 8;
        public new TileTexture Texture => base.Texture as TileTexture;
        public Tile(int x, int y, TileTexture texture, int tileX, int tileY) : base(x, y, texture, tileX, tileY)
        {
            if (Texture.TileSolidStates.Length > 0 && Texture.TileSolidStates.GetLength(0) > tileX && Texture.TileSolidStates.GetLength(1) > tileY)
            {
                int ss = Texture.TileSolidStates[tileX, tileY];
                switch (ss)
                {
                    case 0:
                        Solid = SolidState.Ground;
                        break;
                    case 1:
                        Solid = SolidState.NonSolid;
                        break;
                    case 2:
                        Solid = SolidState.Entity;
                        State = TileStates.SpikeU;
                        KillCrewmen = true;
                        break;
                    case 3:
                        Solid = SolidState.Entity;
                        State = TileStates.SpikeD;
                        KillCrewmen = true;
                        break;
                    case 4:
                        Solid = SolidState.Entity;
                        State = TileStates.SpikeR;
                        KillCrewmen = true;
                        break;
                    case 5:
                        Solid = SolidState.Entity;
                        State = TileStates.SpikeL;
                        KillCrewmen = true;
                        break;
                    case 6:
                        Solid = SolidState.Ground;
                        State = TileStates.OneWayU;
                        break;
                    case 7:
                        Solid = SolidState.Ground;
                        State = TileStates.OneWayD;
                        break;
                    case 8:
                        Solid = SolidState.Ground;
                        State = TileStates.OneWayL;
                        break;
                    case 9:
                        Solid = SolidState.Ground;
                        State = TileStates.OneWayR;
                        break;
                    case 10:
                        Solid = SolidState.Entity;
                        KillCrewmen = true;
                        break;
                    default:
                        break;
                }
            }
            if (Texture.TileSizeX != 8 || Texture.TileSizeY != 8)
            {
                Size = 8f / Texture.TileSizeX;
                
            }
            Static = true;
            Layer = -2;
            FramePush = new PushData(Pushability.Immovable);
        }

        

        public void ChangeTexture(TileTexture texture)
        {
            int tileX = TextureX;
            int tileY = TextureY;
            base.ChangeTexture(texture);
            Animation = Animation.Static(tileX, tileY, texture);
            TexMatrix *= Matrix4.CreateTranslation(tileX, tileY, 0);
            if (Texture.TileSolidStates.Length > 0 && Texture.TileSolidStates.GetLength(0) > tileX && Texture.TileSolidStates.GetLength(1) > tileY)
            {
                int ss = (int)Texture.TileSolidStates[tileX, tileY];
                if (ss > (int)SolidState.NonSolid)
                {
                    ss -= (int)SolidState.NonSolid + 1;
                    KillCrewmen = true;
                }
                Solid = (SolidState)ss;
            }
            if (Texture.TileSizeX != 8 / Size || Texture.TileSizeY != 8 / Size)
            {
                Size = 8f / Texture.TileSizeX;
            }
            ResetAnimation();
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "Tile");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("TileX", TextureX);
        //    ret.Add("TileY", TextureY);
        //    ret.Add("Tag", Tag);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Remove("Animation");
                ret.Add("TileX", new SpriteProperty("TileX", () => Animation.GetFrame(0).X, (t, g) => Animation = Animation.Static((int)t, Animation.GetFrame(0).Y, Texture), 0, SpriteProperty.Types.Int, "The X position in the texture used by the tile."));
                ret.Add("TileY", new SpriteProperty("TileY", () => Animation.GetFrame(0).Y, (t, g) => Animation = Animation.Static(Animation.GetFrame(0).X, (int)t, Texture), 0, SpriteProperty.Types.Int, "The Y position in the texture used by the tile."));
                ret["Type"].GetValue = () => "Tile";
                return ret;
            }
        }
    }
}
