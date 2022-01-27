using System;
using System.Collections.Generic;
using System.Text;

namespace V7
{
    public interface IMapSprite : IDisposable
    {
        float X { get; set; }
        float Y { get; set; }
        bool IsWhite { get; set; }
        int Layer { get; set; }
        float Width { get; }
        float Height { get; }

        void SetSize(float w, float h);
        void SetTarget(float x, float y);
        void FadeIn();
        void FadeOut();
        void Delay(int frames);
        void EnterFrom(System.Drawing.PointF location);

        IMapSprite Clone();
        Action FinishFading { get; set; }
    }
}
