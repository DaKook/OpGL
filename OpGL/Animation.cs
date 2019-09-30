using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Animation
    {
        public string Name;
        // texture location, in pixels
        private Point[] frames;
        private Texture texture;
        public Rectangle Hitbox { get; set; }

        public int FrameCount { get => frames.Length; }
        public int LoopStart { get; set; } = 0;

        public static Animation Static(int x, int y, Texture texture) => new Animation(new Point[] { new Point(x, y) }, new Rectangle(0, 0, texture.TileSizeX, texture.TileSizeY), texture);

        public Animation(Point[] frames, Rectangle hitbox, Texture texture)
        {
            this.frames = new Point[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                this.frames[i] = new Point(frames[i].X, frames[i].Y);

            this.texture = texture;
            Hitbox = hitbox;
        }
        public static Animation EmptyAnimation
        {
            get => new Animation(new Point[] { }, new Rectangle(0, 0, 0, 0), null) { Name = "" };
        }

        public Point GetFrame(int frameId) => frames[frameId];
    }
}
