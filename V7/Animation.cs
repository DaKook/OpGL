using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace V7
{
    public class Animation
    {
        private static Random r = new Random();
        public string Name;
        // texture location, in pixels
        private Point[] frames;
        private Texture texture;
        public Rectangle Hitbox { get; set; }
        public Rectangle[] CurrentHitboxes(int frame)
        {
            if (FrameHitboxes is null || FrameHitboxes.Length < frame)
                return ExtraHitboxes;
            Rectangle[] ret = new Rectangle[FrameHitboxes[frame].Length];
            for (int i = 0; i < FrameHitboxes[frame].Length; i++)
            {
                ret[i] = ExtraHitboxes[FrameHitboxes[frame][i]];
            }
            return ret;
        }
        public Rectangle[] ExtraHitboxes { get; set; }
        public int[][] FrameHitboxes { get; set; }
        public bool IsCircle { get; set; }
        public bool Random;
        public int BaseSpeed = 1;

        public int FrameCount { get => frames.Length; }
        public int LoopStart { get; set; } = 0;
        private int randomFrame;

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

        public Point GetFrame(int frameId)
        {
            if (Random)
            {
                if (frameId % BaseSpeed == 0)
                    randomFrame = r.Next(frames.Length);
                return frames[randomFrame];
            }
            else
                return frames[frameId / BaseSpeed];
        }

        public int GetFrameNumber(int frameId)
        {
            if (Random)
            {
                return randomFrame;
            }
            else
                return frameId / BaseSpeed;
        }

        public JObject Save()
        {
            JObject animation = new JObject();
            animation.Add("Name", Name);
            animation.Add("Speed", BaseSpeed);
            animation.Add("Frames", new JArray(frames));
            animation.Add("Hitbox", new JArray() { Hitbox.X, Hitbox.Y, Hitbox.Width, Hitbox.Height });
            if (LoopStart != 0)
                animation.Add("LoopStart", LoopStart);
            return animation;
        }

        public Animation Clone(Texture tex = null)
        {
            if (tex is null) tex = texture;
            Point[] fr = new Point[frames.Length];
            frames.CopyTo(fr, 0);
            return new Animation(fr, Hitbox, tex) { Name = Name };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
