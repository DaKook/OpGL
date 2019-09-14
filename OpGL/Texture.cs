using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Texture
    {
        public Sprite.SolidState[,] TileSolidStates { get; set; }
        public SortedList<int, int> CharacterWidths { get; set; }
        public SortedList<string,Animation> Animations { get; set; }
        public string Name { get; private set; }
        public uint ID { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public int TileSizeX { get; set; }
        public int TileSizeY { get; set; }
        public ProgramData Program { get; private set; }
        public uint baseVAO { get; private set; }
        public uint baseVBO { get; private set; }

        public int GetCharacterWidth(int character)
        {
            if (CharacterWidths != null && CharacterWidths.ContainsKey(character))
                return CharacterWidths[character];
            else
                return TileSizeX;
        }

        public Animation AnimationFromName(string name)
        {
            Animations.TryGetValue(name, out Animation anim);
            return anim;
        }

        public Texture(uint id, float width, float height, int tileSize, int tileSize2, string name, ProgramData program, uint vao, uint vbo)
        {
            ID = id;
            Width = width;
            Height = height;
            TileSizeX = tileSize;
            TileSizeY = tileSize2;
            Name = name;
            Program = program;
            baseVAO = vao;
            baseVBO = vbo;
        }
    }
}
