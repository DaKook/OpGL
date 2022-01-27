using System;
using System.Drawing;
using OpenTK;
using System.Collections.Generic;
using System.Text;

namespace V7
{
    class MapImageSprite : Sprite, IMapSprite
    {

        public PointF Target;
        public bool GoToTarget = false;
        public bool IsWhite { get; set; }

        public float FadeSpeed;
        private float fadePosition = 100;
        private float MaxSize;
        private int delayFrames = 0;
        private int div;
        public Action FinishFading { get; set; }

        public void SetTarget(float x, float y)
        {
            Target = new PointF(x, y);
            GoToTarget = true;
        }

        public void FadeIn()
        {
            fadePosition = 0;
            FadeSpeed = 1;
        }

        public void FadeOut()
        {
            fadePosition = 100;
            FadeSpeed = -1;
        }

        public void EnterFrom(PointF location)
        {
            div = 4;
            SetTarget(X, Y);
            X += location.X;
            Y += location.Y;
        }

        public void Delay(int frames)
        {
            delayFrames = frames;
        }

        public override void Process()
        {
            if (delayFrames <= 0)
            {
                if (fadePosition > 0 && FadeSpeed < 0)
                {
                    fadePosition -= (int)Math.Ceiling(fadePosition / 5);
                    if (fadePosition <= 0)
                    {
                        fadePosition = 0;
                        FadeSpeed = 0;
                        FinishFading?.Invoke();
                        FinishFading = null;
                    }
                }
                else if (fadePosition < 100 && FadeSpeed > 0)
                {
                    fadePosition += (int)Math.Ceiling((100 - fadePosition) / 5);
                    if (fadePosition >= 100)
                    {
                        fadePosition = 100;
                        FadeSpeed = 0;
                        FinishFading?.Invoke();
                        FinishFading = null;
                    }
                }
                if (GoToTarget)
                {
                    bool xg = X < Target.X;
                    bool yg = Y < Target.Y;
                    bool xc = false;
                    bool yc = false;
                    if (xg)
                    {
                        X += (int)Math.Ceiling((Target.X - X) / div);
                        if (X >= Target.X)
                        {
                            X = Target.X;
                            xc = true;
                        }
                    }
                    else
                    {
                        X += (int)Math.Floor((Target.X - X) / div);
                        if (X <= Target.X)
                        {
                            X = Target.X;
                            xc = true;
                        }
                    }
                    if (yg)
                    {
                        Y += (int)Math.Ceiling((Target.Y - Y) / div);
                        if (Y >= Target.Y)
                        {
                            Y = Target.Y;
                            yc = true;
                        }
                    }
                    else
                    {
                        Y += (int)Math.Floor((Target.Y - Y) / div);
                        if (Y <= Target.Y)
                        {
                            Y = Target.Y;
                            yc = true;
                        }
                    }
                    if (xc && yc)
                    {
                        GoToTarget = false;
                        div = 2;
                        FinishFading?.Invoke();
                        FinishFading = null;
                    }
                }
            }
            else
                delayFrames -= 1;
            Color = Color.FromArgb((int)(fadePosition * 255 / 100), Color.R, Color.G, Color.B);
            Size = (0.5f + (0.5f * (fadePosition / 100))) * MaxSize;
        }
        public MapImageSprite(float x, float y, Texture texture, int mx, int my) : base(x, y, texture, mx, my)
        {
            
        }
        public void SetSize(float width, float height)
        {
            Size = width / Texture.TileSizeX;
            MaxSize = Size;
        }

        public override void RenderPrep()
        {
            double x = DX, y = DY;
            if (Size != MaxSize)
            {
                DX += (Texture.TileSizeX * MaxSize - Texture.TileSizeX * Size) / 2;
                DY += (Texture.TileSizeY * MaxSize - Texture.TileSizeY * Size) / 2;
            }
            base.RenderPrep();
            DX = x;
            DY = y;
        }

        public IMapSprite Clone()
        {
            MapImageSprite mis = new MapImageSprite(X, Y, Texture, TextureX, TextureY);
            return mis;
        }

        public override void Dispose()
        {
            
        }
    }
}
