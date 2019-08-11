using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Point = System.Drawing.Point;

using OpenGL;
using System.Drawing;

namespace OpGL
{
    public class SpriteCollection : List<Sprite>
    {
        // A smaller group size results in more tiles to check; a larger group size results in more drawables per tile.
        // I expect that group size equal to the smallest tile size is ideal, but I have not done any tests.
        const int GROUP_SIZE = 8;
        public Color Color = Color.White;
        SortedList<Point, List<Sprite>> perTile;
        static Comparer<Point> pointComparer = Comparer<Point>.Create(TileCompare);

        public SpriteCollection() : base() { }
        public SpriteCollection(int capacity) : base(capacity) { }
        public SpriteCollection(IEnumerable<Sprite> drawables) : base()
        {
            AddRange(drawables);
        }

        public void Render()
        {
            int modelLoc = -1;
            int texLoc = -1;
            int colorLoc = -1;
            int masterColorLoc = -1;

            Texture lastTex = null;
            uint lastProgram = uint.MaxValue;
            long lastColor = long.MinValue;
            for (int i = 0; i < Count; i++)
            {
                Sprite d = this[i];
                if (!d.Visible)
                    continue;

                d.RenderPrep();
                Gl.BindVertexArray(d.VAO);
                if (lastTex != d.Texture)
                {
                    lastTex = d.Texture;
                    Gl.BindTexture(TextureTarget.Texture2d, lastTex.ID);
                    if (lastProgram != lastTex.Program)
                    {
                        lastProgram = lastTex.Program;
                        modelLoc = Gl.GetUniformLocation(lastProgram, "model");
                        texLoc = Gl.GetUniformLocation(lastProgram, "texMatrix");
                        colorLoc = Gl.GetUniformLocation(lastProgram, "color");
                        masterColorLoc = Gl.GetUniformLocation(lastProgram, "masterColor");
                        Gl.UseProgram(lastProgram);
                        Gl.Uniform4f(masterColorLoc, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));
                    }
                }
                if (lastColor != d.Color.ToArgb())
                {
                    Gl.Uniform4f(colorLoc, 1, new Vertex4f((float)d.Color.R / 255, (float)d.Color.G / 255, (float)d.Color.B / 255, (float)d.Color.A / 255));
                    lastColor = d.Color.ToArgb();
                }

                Gl.UniformMatrix4f(modelLoc, 1, false, d.LocMatrix);
                Gl.UniformMatrix4f(texLoc, 1, false, d.TexMatrix);
                d.UnsafeDraw();
            }
        }

        private class TileEnumerator : IEnumerator<Point>
        {
            int minX, maxX, minY, maxY;
            int cX, cY;
            public TileEnumerator(Sprite d)
            {
                minX = (int)d.X / GROUP_SIZE;
                minY = (int)d.Y / GROUP_SIZE;
                // don't include a tile if the Drawable only extends exactly to the tile boundary
                float xw = d.X + d.Width;
                maxX = (int)(xw) / GROUP_SIZE - (xw % GROUP_SIZE == 0 ? 1 : 0);
                float yh = d.Y + d.Height;
                maxY = (int)(yh) / GROUP_SIZE - (yh % GROUP_SIZE == 0 ? 1 : 0);

                cX = minX;
                cY = minY;
            }

            public Point Current => new Point(cX, cY);

            object IEnumerator.Current => Current;

            public void Dispose() { } // do nothing

            public bool MoveNext()
            {
                cY++;
                if (cY > maxY)
                {
                    cY = minY;
                    cX++;
                    return cX <= maxX;
                }
                else // for special case where the Drawable has width 0 and is grid aligned; maxX will be less than minX
                    return true;
            }

            public void Reset()
            {
                cX = minX;
                cY = minY;
            }
        }
        public void SortForCollisions()
        {
            perTile = new SortedList<Point, List<Sprite>>(pointComparer);
            foreach (Sprite d in this)
            {
                TileEnumerator te = new TileEnumerator(d);
                do
                {
                    if (!perTile.ContainsKey(te.Current))
                        perTile.Add(te.Current, new List<Sprite>());
                    perTile[te.Current].Add(d);
                } while (te.MoveNext());
            }
        }
        public List<Sprite> GetPotentialColliders(Sprite d)
        {
            List<Sprite> colliders = new List<Sprite>();

            TileEnumerator te = new TileEnumerator(d);
            do
            {
                if (perTile.ContainsKey(te.Current))
                    colliders.AddRange(perTile[te.Current].Where((item) => item != d && !colliders.Contains(item)));
            } while (te.MoveNext());

            return colliders;
        }

        private int RenderCompare(Sprite d1, Sprite d2)
        {
            int t = d1.Texture.ID.CompareTo(d2.Texture.ID);
            if (t == 0)
                return d1.Color.ToArgb().CompareTo(d2.Color.ToArgb());
            else
                return t;
        }
        static int TileCompare(Point p1, Point p2)
        {
            int x = p1.X.CompareTo(p2.X);
            if (x == 0)
                return p1.Y.CompareTo(p2.Y);
            else
                return x;
        }

        /// <summary>
        /// Returns an index at which a drawable can be inserted while keeping the list sorted.
        /// </summary>
        private int AddIndex(Sprite d)
        {
            int min = 0, max = Count - 1;
            int index = (max - min) / 2 + min;
            while (min < max)
            {
                int r = RenderCompare(d, this[index]);
                if (r == -1)
                {
                    min = index + 1;
                    index = (max - min) / 2 + min;
                }
                else if (r == 1)
                {
                    max = index - 1;
                    index = (max - min) / 2 + min;
                }
                else
                    break;
            }
            return index;
        }
        /// <summary>
        /// Searches for the specified Drawable and returns the zero-based index of an occurence within the entire DrawableCollection.
        /// </summary>
        public new int IndexOf(Sprite d)
        {
            // index in the range at which d would be, by render sort
            int index = AddIndex(d);
            // check from this index to the end of that range
            int i = index;
            while (RenderCompare(this[i], d) == 0)
            {
                if (this[i] == d)
                    return i;
                i++;
            }
            // check to the beginning of that range
            i = index - 1;
            while (RenderCompare(this[i], d) == 0)
            {
                if (this[i] == d)
                    return i;
                i--;
            }
            // not found
            return -1;
        }

        public new void Add(Sprite d)
        {
            base.Insert(AddIndex(d), d);
        }
        public new void AddRange(IEnumerable<Sprite> drawables)
        {
            foreach (Sprite d in drawables)
                Add(d);
        }

        /// <summary>
        /// Do not support setting an element.
        /// </summary>
        public new Sprite this[int index]
        {
            get => base[index];
        }

        public new void Remove(Sprite d)
        {
            int index = IndexOf(d);
            if (index != -1)
                base.RemoveAt(index);
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Insert(int index, Sprite d)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void InsertRange(int index, IEnumerable<Sprite> drawables)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Reverse()
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }

        /// <summary>
        /// The list can end up not sorted if an element in the list has its texture or color modified.
        /// Call this method after modifying an element and before the next add or remove.
        /// </summary>
        public new void Sort()
        {
            base.Sort(RenderCompare);
        }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Sort(Comparison<Sprite> comparison)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Sort(IComparer<Sprite> comparer)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Sort(int index, int count, IComparer<Sprite> comparer)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
    }
}
