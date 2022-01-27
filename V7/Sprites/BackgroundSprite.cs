using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class BackgroundSprite : Sprite
    {
        public PointF MovementSpeed;
        public int AnimationFrame
        {
            get => animFrame;
            set
            {
                animFrame = value % (Animation.FrameCount * Animation.BaseSpeed);
            }
        }
        public BackgroundSprite(float x, float y, Texture texture, Animation animation) : base(x, y, texture, animation)
        {

        }
        public BackgroundSprite(float x, float y, Texture texture, int textureX, int textureY) : base(x, y, texture, textureX, textureY)
        {

        }
        public override void Process()
        {
            base.Process();
            X += MovementSpeed.X;
            Y += MovementSpeed.Y;
        }
    }
}
