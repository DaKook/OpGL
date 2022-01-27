using System;
using System.Collections.Generic;
//using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL;
using System.Drawing;

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

        public override float Width => w * Size;
        public override float Height => h * Size;

        public bool OnlyHighlight
        {
            get => _onlyHighlight;
            set
            {
                _onlyHighlight = value;
                Text = Text;
            }
        }

        public void ParseStyles()
        {
            string text = _Text;
            FontTexture tex = texture;
            SortedList<int, int> styles = new SortedList<int, int>();
            int i = text.IndexOf("\\");
            while (i > -1)
            {
                if (int.TryParse(text[i + 1].ToString(), out int style) && style < tex.Width / tex.TileSizeX / 16)
                {
                    text = text.Remove(i, 2);
                    styles.Add(i, style);
                }
                else
                {
                    if (text[i + 1] == '\\')
                        text = text.Remove(i++, 1);
                    i++;
                }
                i = text.IndexOf('\\', i);
            }
            Markers = styles;
            Text = text;
        }

        public void UnparseStyles()
        {
            string text = _Text;
            int offset = 0;
            int i;
            int marker = 0;
            while (marker < Markers.Count)
            {
                i = Markers.Keys[marker];
                text = text.Insert(i + offset, "\\" + Markers.Values[marker]);
                offset += 2;
            }
            Markers.Clear();
        }

        protected string _Text;
        private bool _onlyHighlight;
        private FontTexture texture => Texture as FontTexture;

        public int GetIndexFromPoint(float x, float y, float zoom)
        {
            string[] lines = Text.Split('\n');
            int lineY = (int)((y - 8 * zoom) / (8 * zoom));
            lineY = Math.Min(Math.Max(0, lineY), lines.Length - 1);
            string line = lines[lineY];
            int lineX = (int)Math.Round((x - 8 * zoom) / (8 * zoom));
            lineX = Math.Min(Math.Max(0, lineX), line.Length);
            Array.Resize(ref lines, lineY);
            return lines.Sum((s) => s.Length) + lineX + lineY;
        }

        public PointF GetCharacterLocation(int ch)
        {
            string s = Text.Substring(0, ch);
            string s2 = s.Replace("\n", "");
            int dif = s.Length - s2.Length;
            if (SelectionStart + SelectionLength <= ch)
                dif -= 1;
            if (SelectionStart <= ch)
            {
                if (SelectionStart + SelectionLength <= ch)
                    dif -= SelectionLength;
                else
                    dif -= ch - SelectionStart;
            }
            int index = (ch - dif) * 4;
            return new PointF(bufferData[index], bufferData[index + 1]);
        }

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
                byte[] bytes = Encoding.Unicode.GetBytes(_Text);
                bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, bytes);
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
                        uint c = bytes[i];
                        if ((c & 0b11111000) == 0b11111000)
                        {
                            c *= 0b1000000000000000000000000;
                            c += (uint)bytes[++i] * 0b1000000000000000;
                            c += (uint)bytes[++i] * 0b10000000;
                            c += bytes[++i];
                        }
                        else if ((c & 0b11110000) == 0b11110000)
                        {
                            c *= 0b10000000000000000;
                            c += (uint)bytes[++i] * 0b10000000;
                            c += bytes[++i];
                        }
                        else if ((c & 0b11100000) == 0b11100000)
                        {
                            c *= 256;
                            c += bytes[++i];
                        }
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
                                cx += texture.GetCharacterWidth(s[cc++]);
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
                            uint x = c % 16;
                            uint y = (c - x) / 16;
                            bufferData[index++] = curX;
                            bufferData[index++] = curY;
                            bufferData[index++] = x + currentStyle * 16;
                            bufferData[index++] = y;
                            curX += texture.GetCharacterWidth((int)c);
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
                        bufferData[index++] = -Texture.TileSizeX;
                        bufferData[index++] = 0;
                        bufferData[index++] = 9;
                        bufferData[index++] = 0;
                        SelectionX = -Texture.TileSizeX;
                        SelectionY = 0;
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

        public virtual void AlignToCenter()
        {
            AlignToCenter(0);
        }

        protected void AlignToCenter(int start)
        {
            List<int> lines = new List<int>();
            float y = -1;
            float lineStartX = 0;
            int x = 0;
            for (int i = start; i < bufferData.Length; i += 4)
            {
                if (bufferData[i + 1] != y)
                {
                    if (lines.Count > 0 && lines.Last() < w)
                    {
                        //lines[lines.Count - 1] += texture.GetCharacterWidth((int)(bufferData[i + 2] + bufferData[i + 3] * 16));
                        float width = (w - lines.Last()) / 2 - lineStartX;
                        for (int j = x; j < i; j += 4)
                        {
                            bufferData[j] += width;
                        }
                    }
                    lines.Add(texture.GetCharacterWidth((int)(bufferData[i + 2] + bufferData[i + 3] * 16)));
                    y = bufferData[i + 1];
                    lineStartX = bufferData[i];
                    x = i;
                }
                else
                {
                    lines[lines.Count - 1] += texture.GetCharacterWidth((int)(bufferData[i + 2] + bufferData[i + 3] * 16));
                }
            }
            if (lines.Last() < w)
            {
                float width = (w - lines.Last()) / 2 - lineStartX;
                for (int j = x; j < bufferData.Length; j += 4)
                {
                    bufferData[j] += width;
                }
            }
            lines.Add(0);
        }

        public int GetCtrlIndex(bool left = true)
        {
            if (SelectionStart < 0) return -1;
            int from = SelectingFromLeft ? SelectionStart + SelectionLength : SelectionStart;
            bool isLetter = false;
            int pos = from;
            int stride = left ? -1 : 1;
            if (!left)
                pos--;
            while (left ? pos > 0 : pos < Text.Length)
            {
                pos += stride;
                if (pos == Text.Length)
                    break;
                char c = Text[pos];
                if (char.IsLetterOrDigit(c))
                {
                    if (!isLetter && Math.Abs(from - pos) > 2)
                    {
                        if (left)
                            pos -= stride;
                        break;
                    }
                    isLetter = true;
                }
                else if (isLetter)
                {
                    if (left)
                        pos -= stride;
                    break;
                }
            }
            return pos;
        }

        public StringDrawable(float x, float y, FontTexture texture, string text, Color? color = null) : base(x, y, texture)
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
                    curX += texture.GetCharacterWidth(c);
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
