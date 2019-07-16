using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Game
    {
        public List<Texture> Textures = new List<Texture>();
        public List<Drawable> Entities = new List<Drawable>();
        public Tile[,] Tiles;
        public float CameraX;
        public float CameraY;
    }
}
