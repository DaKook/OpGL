using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Texture
    {
        public Drawable.SolidState[,] TileSolidStates { get; internal set; }
        public List<Animation> Animations { get; set; }
        public string Name { get; internal set; }
        public uint ID { get; internal set; }
        public float Width { get; internal set; }
        public float Height { get; internal set; }
        public int TileSize { get; set; }
        public uint Program { get; internal set; }
        public uint VAO { get; private set; }
        public uint IBO { get; private set; }

        public Texture(uint id, float width, float height, int tileSize, string name, uint program, uint vao, uint ibo)
        {
            ID = id;
            Width = width;
            Height = height;
            TileSize = tileSize;
            Name = name;
            Program = program;
            VAO = vao;
            IBO = ibo;
        }
    }

    static class Textures
    {
        public const int FONT = 0;
        public const int SPRITES = 1;
        public const int TILES = 2;
        public const int TILES2 = 3;
    }
}
