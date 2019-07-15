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
        public int AnimFrame { get; internal set; }
        public Point[] Frames { get; set; }
        public Point CurrentFrame { get => Frames[AnimFrame]; }
        public Rectangle Hitbox { get; set; }
        public void AdvanceFrame()
        {
            if (Frames.Length <= 1) return;
            AnimFrame += 1;
            if (AnimFrame >= Frames.Length)
            {
                while (AnimFrame >= Frames.Length)
                {
                    AnimFrame -= Frames.Length;
                }
            }
        }
        public void ResetAnimation()
        {
            AnimFrame = 0;
        }
        public Animation(Point[] frames, Rectangle hitbox)
        {
            Frames = frames;
            Hitbox = hitbox;
        }
        public static Animation EmptyAnimation
        {
            get => new Animation(new Point[] { }, new Rectangle(0, 0, 0, 0));
        }
    }
}
