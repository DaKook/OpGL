using OpenTK.Graphics.OpenGL;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class Texture
    {
        public Sprite.SolidState[,] TileSolidStates { get; set; }
        public SortedList<int, int> CharacterWidths { get; set; }
        public SortedList<string,Animation> Animations { get; set; }
        public string Name { get; private set; }
        public int ID { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public int TileSizeX { get; set; }
        public int TileSizeY { get; set; }
        public TextureProgram Program { get; private set; }
        public int baseVAO { get; private set; }
        public int baseVBO { get; private set; }
        public Matrix4 BaseTexMatrix { get; private set; }
        public Color TextBoxColor { get; set; }
        public int Updated = 0;
        public string Squeak = "";

        public int GetCharacterWidth(int character)
        {
            if (CharacterWidths != null && CharacterWidths.ContainsKey(character))
                return CharacterWidths[character];
            else
                return TileSizeX;
        }

        public Animation AnimationFromName(string name)
        {
            if (Animations is null) return null;
            Animations.TryGetValue(name, out Animation anim);
            return anim;
        }

        public void Update(float width, float height, int tileSize, int tileSize2, int vao, int vbo)
        {
            Width = width;
            Height = height;
            TileSizeX = tileSize;
            TileSizeY = tileSize2;
            baseVAO = vao;
            baseVBO = vbo;
            BaseTexMatrix = Matrix4.CreateScale(tileSize / width, tileSize2 / height, 1f);
            Updated += 1;
        }

        public Texture(int id, float width, float height, int tileSize, int tileSize2, string name, TextureProgram program, int vao, int vbo)
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
            BaseTexMatrix = Matrix4.CreateScale(tileSize / width, tileSize2 / height, 1f);
        }
    }
}
