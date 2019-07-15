using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Texture
    {
        public List<Animation> Animations { get; set; }
        public string Name { get; internal set; }
        public uint ID { get; internal set; }
        public float Width { get; internal set; }
        public float Height { get; internal set; }
        public int TileSize { get; set; }
        public uint Program { get; internal set; }
        public uint VAO { get; private set; }
        public Texture(uint id, float width, float height, int tileSize, string name, uint program, uint vao)
        {
            ID = id;
            Width = width;
            Height = height;
            TileSize = tileSize;
            Name = name;
            Program = program;
            VAO = vao;
        }
    }
}
