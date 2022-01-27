using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using OpenTK;

namespace V7
{
    class VButton : SpriteGroup
    {
        private RectangleSprite border;
        private Color _color;
        private RectangleSprite rect;
        private StringDrawable text;

        private bool isClicking;

        public Color Color
        {
            get => _color;
            set
            {
                _color = Color.FromArgb(255, value.R, value.G, value.B);
                int brightness = Math.Max(Math.Max(value.R, value.G), value.B);
                Color tc = brightness > 150 ? Color.Black : Color.White;
                if (!isClicking)
                    rect.Color = _color;
                else
                    rect.Color = Color.FromArgb(255, 255 - (255 - value.R) / 2, 255 - (255 - value.G) / 2, 255 - (255 - value.B) / 2);
                text.Color = tc;
            }
        }
        public string Text
        {
            get => text.Text;
            set
            {
                text.Text = value;
                text.CenterX = rect.CenterX;
                text.CenterY = rect.CenterY;
            }
        }
        public float Right
        {
            get => border.Right;
            set => X = value - border.Width;
        }
        public float Bottom
        {
            get => border.Bottom;
            set => Y = value - border.Height;
        }

        public bool IsTouching(Point p) => border.Within(p.X, p.Y, 1, 1);

        public Action OnClick;

        public VButton(float x, float y, FontTexture texture, float width, float height, string text, Color color, int layer = 2)
        {
            X = x;
            Y = y;
            border = new RectangleSprite(x, y, width, height) { Color = Color.Black, Layer = layer };
            rect = new RectangleSprite(x + 1, y + 1, width - 2, height - 2) { Layer = layer + 1 };
            this.text = new StringDrawable(0, 0, texture, "", Color.White) { Layer = layer + 2 };
            Color = color;
            Text = text;
            Add(border);
            Add(rect);
            Add(this.text);
        }

        public void Select()
        {
            if (isClicking) return;
            border.Color = Color.White;
        }
        public void Click()
        {
            isClicking = true;
            rect.Color = Color.FromArgb(255, 255 - (255 - _color.R) / 2, 255 - (255 - _color.G) / 2, 255 - (255 - _color.B) / 2);
            border.Color = Color.Black;
        }
        public void Unselect()
        {
            if (isClicking) return;
            border.Color = Color.Black;
        }
        public void UnClick()
        {
            isClicking = false;
            rect.Color = _color;
        }
    }
}
