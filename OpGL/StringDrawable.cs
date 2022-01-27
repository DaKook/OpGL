using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL;

namespace V7
{
    public class StringDrawable : InstancedSprite
    {
        protected int w;
        protected int h;

        public int SelectionStart = -1;
        public int SelectionLength;
        public bool SelectingFromLeft = true;

        public float SelectionY { get; private set; }
        public float SelectionX { get; private set; }

        public int MaxWidth = -1;

        public int BaseStyle;
        public SortedList<int, int> Markers = new SortedList<int, int>();

        public override float Width => w;
        public override float Height => h;

        public bool OnlyHighlight
        {
            get => _onlyHighlight;
            set
            {
                _onlyHighlight = value;
                Text = Text;
            }
        }

        protected string _Text;
        private bool _onlyHighlight;

        public virtual string Text
        {
            get => _Text;
            set
            {
                if (value is object)
                    _Text = value.Replace(Environment.NewLine, "\n");
                else
                    _Text = "";
                int l = _Text.Length;
                if (SelectionStart > -1)
                {
                    l += 1;
                    if (SelectionLength > 0)
                        l += SelectionLength;
                }
                bufferData = new float[l * 4];
                w = 0;
                float curX = 0, curY = 0;
                int index = 0;
                int currentStyle = BaseStyle;
                for (int i = 0; i < _Text.Length; i++)
                {
                    if (Markers.ContainsKey(i))
                    {
                        currentStyle = Markers[i];
                    }
                    if (i >= SelectionStart && i < SelectionStart + SelectionLength)
                    {
                        bufferData[index++] = curX;
                        bufferData[index++] = curY;
                        bufferData[index++] = 10;
                        bufferData[index++] = 0;
                    }
                    if (!OnlyHighlight)
                    {
                        int c = _Text[i];
                        if (MaxWidth > -1 && (c == ' '))
                        {
                            int ln = _Text.IndexOf(' ', i + 1);
                            string s;
                            if (ln > -1)
                            {
                                ln -= i;
                                s = _Text.Substring(i, ln);
                            }
                            else
                                s = _Text.Substring(i);
                            int cx = (int)curX;
                            int cc = 0;
                            while (cx <= MaxWidth && cc < s.Length)
                            {
                                cx += Texture.GetCharacterWidth(s[cc++]);
                            }
                            if (cx > MaxWidth)
                                c = '\n';
                        }
                        if (c == '\n')
                        {
                            if (curX > w) w = (int)curX;
                            curX = 0;
                            curY += Texture.TileSizeY;
                        }
                        else
                        {
                            int x = c % 16;
                            int y = (c - x) / 16;
                            bufferData[index++] = curX;
                            bufferData[index++] = curY;
                            bufferData[index++] = x + currentStyle * 16;
                            bufferData[index++] = y;
                            curX += Texture.GetCharacterWidth(c);
                            if (curX > w) w = (int)curX;
                        }
                    }
                    if (i == SelectionStart + SelectionLength - 1)
                    {
                        bufferData[index++] = curX - Texture.TileSizeX;
                        bufferData[index++] = curY;
                        bufferData[index++] = 9;
                        bufferData[index++] = 0;
                        SelectionX = curX - Texture.TileSizeX;
                        SelectionY = curY;
                    }
                    else if (i == 0 && SelectionStart + SelectionLength == 0)
                    {
                        bufferData[index++] = curX - (2 * Texture.TileSizeX);
                        bufferData[index++] = curY;
                        bufferData[index++] = 9;
                        bufferData[index++] = 0;
                        SelectionX = curX - (2 * Texture.TileSizeX);
                        SelectionY = curY;
                    }
                }
                if (_Text.Length == 0 && SelectionStart > -1)
                {
                    bufferData[index++] = curX - Texture.TileSizeX;
                    bufferData[index++] = curY;
                    bufferData[index++] = 9;
                    bufferData[index++] = 0;
                    SelectionX = curX - Texture.TileSizeX;
                    SelectionY = curY;
                }
                instances = index / 4;
                Array.Resize(ref bufferData, index);

                h = (int)curY + Texture.TileSizeY;

                updateBuffer = true;
            }
        }

        public StringDrawable(float x, float y, Texture texture, string text, Color? color = null) : base(x, y, texture)
        {
            if ((texture.Width / texture.TileSizeX) % 16 != 0)
                throw new InvalidOperationException("A font texture must have a width divisible by 16.");
            Solid = SolidState.NonSolid;

            Color = color ?? Color.White;
            ColorModifier = AnimatedColor.Default;

            Text = text;
        }

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Remove("Animation");
                ret.Add("Text", new SpriteProperty("Text", () => _Text, (t, g) => Text = (string)t, "", SpriteProperty.Types.String, "The text displayed by the sprite."));
                ret["Type"].GetValue = () => "Text";
                return ret;
            }
        }

        public void SetBuffer(float[] buffer, int w, int h)
        {
            bufferData = buffer;
            this.w = w;
            this.h = h;
            updateBuffer = true;
        }

        public Point AddText(int xPos, int yPos, string text)
        {
            float curX = xPos, curY = yPos;
            int index = bufferData.Length;
            int currentStyle = BaseStyle;
            Array.Resize(ref bufferData, bufferData.Length + (text.Length * 4));
            for (int i = 0; i < text.Length; i++)
            {
                int c = text[i];
                if (c == '\n')
                {
                    if (curX > w) w = (int)curX;
                    curX = 0;
                    curY += Texture.TileSizeY;
                }
                else
                {
                    int x = c % 16;
                    int y = (c - x) / 16;
                    bufferData[index++] = curX;
                    bufferData[index++] = curY;
                    bufferData[index++] = x + currentStyle * 16;
                    bufferData[index++] = y;
                    curX += Texture.GetCharacterWidth(c);
                    if (curX > w) w = (int)curX;
                }
            }
            instances = index / 4;
            Array.Resize(ref bufferData, index);

            h = (int)curY + Texture.TileSizeY;

            updateBuffer = true;
            return new Point((int)curX, (int)curY);
        }

        public override void Process()
        {
            // do nothing
        }
    }
}
