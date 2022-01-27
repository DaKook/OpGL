using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    class BackgroundSprite : Sprite
    {
        public PointF MovementSpeed;
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
