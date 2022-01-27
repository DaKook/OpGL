using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class AnimatedColor
    {
        public List<Color> BaseColors = new List<Color>();
        public float Darkness;
        public float Lightness;
        public int Randomness;
        public int Length;
        public bool RandomFrames;
        private static Random rand;

        public AnimatedColor(List<Color> frames, float dark, float light, int random, int length, bool randomFrames = true)
        {
            BaseColors = frames;
            Darkness = dark;
            Lightness = light;
            Randomness = random;
            Length = length;
            RandomFrames = randomFrames;
        }

        public Color GetFrame(int frame)
        {
            rand = new Random(frame);
            Color ret;
            if (BaseColors.Count == 0)
                ret = Color.White;
            else
            {
                if (RandomFrames)
                {
                    ret = BaseColors[rand.Next(BaseColors.Count)];
                }
                else
                {
                    ret = BaseColors[frame % BaseColors.Count];
                }
            }
            frame %= Length * 4;
            float r = ret.R;
            float g = ret.G;
            float b = ret.B;
            if (frame < Length * 2)
            {
                if (frame > Length) frame = Length - (frame % Length);
                float a = rand.Next(-Randomness, Randomness + 1) / 255f;
                float m = 1f - ((Darkness) * ((float)frame / Length) * (1 - a));
                r *= m;
                g *= m;
                b *= m;
            }
            else
            {
                frame -= Length * 2;
                if (frame > Length) frame = Length - (frame % Length);
                float a = rand.Next(-Randomness, Randomness + 1) / 255f;
                float m = ((Lightness) * ((float)frame / Length)) * (1 - a);
                r += (255 - r) * m;
                g += (255 - g) * m;
                b += (255 - b) * m;
            }
            ret = Color.FromArgb(ret.A, Math.Min((int)r, 255), Math.Min((int)g, 255), Math.Min((int)b, 255));
            return ret;
        }

        public static AnimatedColor Default => new AnimatedColor(new List<Color>() { Color.FromArgb(255, 230, 230, 230) }, 0.07f, 1f, 10, 60);
        public static AnimatedColor Flashy => new AnimatedColor(new List<Color>() { Color.FromArgb(255, 165, 20, 255), Color.FromArgb(255, 155, 15, 240), Color.FromArgb(255, 255, 255, 255), Color.FromArgb(255, 200, 255, 205) }, 0.05f, 0.05f, 2, 60);
        
        public static implicit operator AnimatedColor(Color c)
        {
            return new AnimatedColor(new List<Color>() { c }, 0, 0, 0, 1);
        }
        public static implicit operator Color(AnimatedColor c)
        {
            return c.BaseColors.FirstOrDefault();
        }
    }
}
