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
        // texture location, in pixels
        private Point[] frames;
        private Texture texture;
        public Rectangle Hitbox { get; set; }

        public int FrameCount { get => frames.Length; }

        public Animation(Point[] frames, Rectangle hitbox, Texture texture)
        {
            this.frames = new Point[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                this.frames[i] = new Point(frames[i].X * texture.TileSize, frames[i].Y * texture.TileSize);

            this.texture = texture;
            Hitbox = hitbox;
        }
        public static Animation EmptyAnimation
        {

            get => new Animation(new Point[] { }, new Rectangle(0, 0, 0, 0), null);
        }

        public Point GetFrame(int frameId)
        {
            return frames[frameId];
        }
    }
}
