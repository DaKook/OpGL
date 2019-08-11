﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Texture
    {
        public Sprite.SolidState[,] TileSolidStates { get; internal set; }
        public SortedList<string,Animation> Animations { get; set; }
        public string Name { get; internal set; }
        public uint ID { get; internal set; }
        public float Width { get; internal set; }
        public float Height { get; internal set; }
        public int TileSize { get; set; }
        public uint Program { get; internal set; }
        public uint baseVAO { get; private set; }
        public uint baseVBO { get; private set; }

        public Animation AnimationFromName(string name)
        {
            Animations.TryGetValue(name, out Animation anim);
            return anim;
        }

        public Texture(uint id, float width, float height, int tileSize, string name, uint program, uint vao, uint vbo)
        {
            ID = id;
            Width = width;
            Height = height;
            TileSize = tileSize;
            Name = name;
            Program = program;
            baseVAO = vao;
            baseVBO = vbo;
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
