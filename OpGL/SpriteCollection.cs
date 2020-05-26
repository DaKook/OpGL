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
    public class SpriteCollection : List<Sprite>, IDisposable
    {
        public void Dispose()
        {
            if (!firstRender)
            {
                //if (Gl.IsBuffer(ibo))
                //{
                //    Gl.DeleteBuffers(ibo);
                //    Gl.DeleteVertexArrays(vao);
                //}
            }
        }

        // A smaller group size results in more tiles to check; a larger group size results in more sprites per tile.
        // I expect that group size equal to the smallest tile size is ideal, but I have not done any tests.
        const int GROUP_SIZE = 8;
        public Color Color = Color.White;
        SortedList<Point, List<Sprite>> perTile;
        public static Comparer<Point> pointComparer = Comparer<Point>.Create(TileCompare);
        private uint ibo;
        private float[] tilesBuffer;
        private bool firstRender = true;
        private bool updateBuffer = false;
        private bool setBuffer = true;
        private uint vao;
        private List<Tile> tiles = new List<Tile>();
        private Texture tileTexture;
        public bool Visible = true;
        public List<Sprite> ToProcess = new List<Sprite>();
        public Color TileColor = Color.White;

        public SpriteCollection() : base() { }
        public SpriteCollection(int capacity) : base(capacity) { }
        public SpriteCollection(IEnumerable<Sprite> drawables) : base()
        {
            AddRange(drawables);
        }

        public void CheckBuffer()
        {
            if (setBuffer) SetBuffer();
        }

        public virtual void Render(int frame, bool showInvisible = false)
        {
            if (!Visible) return;
            ProgramData lastProgram = null;

            if (updateBuffer)
                UpdateBuffer(tileTexture);
            if (tiles.Count > 0 && tileTexture is object)
            {
                Tile n = new Tile(0, 0, tileTexture, 0, 0);
                n.Color = TileColor;
                n.RenderPrep();
                n.ResetAnimation();
                tileTexture.Program.Reset();
                tileTexture.Program.Prepare(n, frame);
                Gl.BindVertexArray(vao);
                Gl.Uniform4f(tileTexture.Program.MasterColorLocation, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));
                Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, tiles.Count);
            }
            for (int i = 0; i < Count; i++)
            {
                Sprite d = this[i];
                if (!showInvisible && !d.Visible)
                    continue;
                if (d is Tile && d.Layer < 0)
                    continue;

                d.RenderPrep();
                if (d.Program != lastProgram)
                {
                    lastProgram = d.Program;
                    lastProgram.Reset();
                    int masterColorLoc = lastProgram.MasterColorLocation;
                    Gl.UseProgram(lastProgram.ID);
                    Gl.Uniform4f(masterColorLoc, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));
                }
                lastProgram.Prepare(d, frame);

                d.UnsafeDraw();
            }
        }



        private void SetBuffer()
        {
            tiles.Clear();
            tileTexture = null;
            List<Sprite> tls = this.FindAll((tl) => tl is Tile && tl.Layer < 0);
            foreach (Tile t in tls)
            {
                if (tileTexture is null || t.Texture == tileTexture)
                {
                    tiles.Add(t);
                    tileTexture = t.Texture;
                    continue;
                }
            }
            if (tiles.Count > 0)
            {
                tilesBuffer = new float[tiles.Count * 4];
                int i = 0;
                foreach (Tile tile in tiles)
                {
                    tilesBuffer[i++] = tile.X / tile.Size;
                    tilesBuffer[i++] = tile.Y / tile.Size;
                    tilesBuffer[i++] = tile.TextureX;
                    tilesBuffer[i++] = tile.TextureY;
                }
                updateBuffer = true;
            }
            setBuffer = false;
        }

        private void UpdateBuffer(Texture texture)
        {
            if (texture is null) return;
            if (firstRender)
            {
                firstRender = false;

                vao = Gl.CreateVertexArray();
                Gl.BindVertexArray(vao);

                Gl.BindBuffer(BufferTarget.ArrayBuffer, texture.baseVBO);
                Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
                Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);

                ibo = Gl.CreateBuffer();
                Gl.BindBuffer(BufferTarget.ArrayBuffer, ibo);
                Gl.VertexAttribPointer(2, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
                Gl.VertexAttribPointer(3, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                Gl.EnableVertexAttribArray(2);
                Gl.EnableVertexAttribArray(3);
                Gl.VertexAttribDivisor(2, 1);
                Gl.VertexAttribDivisor(3, 1);
            }

            Gl.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)tilesBuffer.Length * sizeof(float), tilesBuffer, BufferUsage.DynamicDraw);
            updateBuffer = false;
        }

        private class TileEnumerator : IEnumerator<Point>
        {
            int minX, maxX, minY, maxY;
            int cX, cY;
            public TileEnumerator(RectangleF d)
            {
                minX = (int)Math.Floor(d.X / GROUP_SIZE);
                minY = (int)Math.Floor(d.Y / GROUP_SIZE);
                // don't include a tile if the Drawable only extends exactly to the tile boundary
                float xw = d.X + d.Width;
                maxX = (int)Math.Floor(xw / GROUP_SIZE) - (xw % GROUP_SIZE == 0 ? 1 : 0);
                float yh = d.Y + d.Height;
                maxY = (int)Math.Floor(yh / GROUP_SIZE) - (yh % GROUP_SIZE == 0 ? 1 : 0);

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
        public bool IsSorting { get; private set; } = false;
        SortedList<Point, List<Sprite>> allStatic;
        public void SortForCollisions()
        {
            if (IsSorting) return;
            IsSorting = true;
            if (allStatic is null)
            {
                allStatic = new SortedList<Point, List<Sprite>>(pointComparer);
                List<Sprite> st = FindAll((s) => s.Static);
                SortInto(allStatic, st);
            }
            perTile = new SortedList<Point, List<Sprite>>(pointComparer);
            List<Sprite> list = FindAll((s) => !s.Static);
            SortInto(perTile, list);
            IsSorting = false;
        }
        private void SortInto(SortedList<Point, List<Sprite>> into, List<Sprite> toSort)
        {
            for (int i1 = 0; i1 < toSort.Count; i1++)
            {
                Sprite d = toSort[i1];
                TileEnumerator te = new TileEnumerator(new RectangleF(d.X, d.Y, d.Width, d.Height));
                do
                {
                    if (!into.ContainsKey(te.Current))
                        into.Add(te.Current, new List<Sprite>());
                    into[te.Current].Add(d);
                } while (te.MoveNext());
                if (d.MultiplePositions)
                {
                    for (int i = 0; i < d.Offsets.Count; i++)
                    {
                        te = new TileEnumerator(new RectangleF(d.X + d.Offsets[i].X, d.Y + d.Offsets[i].Y, d.Width, d.Height));
                        do
                        {
                            if (!into.ContainsKey(te.Current))
                                into.Add(te.Current, new List<Sprite>());
                            into[te.Current].Add(d);
                        } while (te.MoveNext());
                    }
                }
            }
        }
        public List<Sprite> GetPotentialColliders(Sprite d)
        {
            List<Sprite> colliders = new List<Sprite>();
            if (perTile is null) SortForCollisions();

            SortedList<Point, List<Sprite>> lookIn = perTile;
            TileEnumerator te = new TileEnumerator(new RectangleF(d.X, d.Y, d.Width, d.Height));
            for (int i2 = 0; i2 < 2; i2++)
            {
                do
                {
                    if (lookIn.ContainsKey(te.Current))
                        colliders.AddRange(lookIn[te.Current].Where((item) => item != d && !colliders.Contains(item)));
                } while (te.MoveNext());
                if (d.MultiplePositions)
                {
                    for (int i = 0; i < d.Offsets.Count; i++)
                    {
                        te = new TileEnumerator(new RectangleF(d.X + d.Offsets[i].X, d.Y + d.Offsets[i].Y, d.Width, d.Height));
                        do
                        {
                            if (lookIn.ContainsKey(te.Current))
                                colliders.AddRange(lookIn[te.Current].Where((item) => item != d && !colliders.Contains(item)));
                        } while (te.MoveNext());
                    }
                }
                te = new TileEnumerator(new RectangleF(d.X, d.Y, d.Width, d.Height));
                lookIn = allStatic;
            }
            

            return colliders;
        }
        public List<Sprite> GetPotentialColliders(float x, float y, float w = 8, float h = 8)
        {
            if (perTile == null) SortForCollisions();

            List<Sprite> colliders = new List<Sprite>();

            SortedList<Point, List<Sprite>> lookIn = perTile;
            TileEnumerator te = new TileEnumerator(new RectangleF(x, y, w, h));
            for (int i = 0; i < 2; i++)
            {
                do
                {
                    if (lookIn.ContainsKey(te.Current))
                        colliders.AddRange(lookIn[te.Current].Where((item) => !colliders.Contains(item)));
                } while (te.MoveNext());
                te.Reset();
                lookIn = allStatic;
            }
            return colliders;
        }

        private int RenderCompare(Sprite d1, Sprite d2)
        {
            if (d1 is null || d2 is null) return 0;
            int c = d1.Layer.CompareTo(d2.Layer);
            if (c == 0)
            {
                int t = d1.TextureID.CompareTo(d2.TextureID);
                return t;
            }
            else
                return c;
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
            int min = 0, max = Count;
            int index = (max - min) / 2 + min;
            while (min < max)
            {
                int r = RenderCompare(d, this[index]);
                if (r == -1)
                {
                    max = index;
                    index = (max - min) / 2 + min;
                }
                else if (r == 1)
                {
                    min = index + 1;
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
            if (d is null) return -1;
            // index in the range at which d would be, by render sort
            int index = AddIndex(d);
            // check from this index to the end of that range
            int i = index;
            while (i < Count && RenderCompare(this[i], d) == 0)
            {
                if (this[i] == d)
                    return i;
                i++;
            }
            // check to the beginning of that range
            i = index - 1;
            while (i >= 0 && RenderCompare(this[i], d) == 0)
            {
                if (this[i] == d)
                    return i;
                i--;
            }
            // not found
            return -1;
        }

        public void AddForCollisions(Sprite d)
        {
            Add(d);
            if (!d.Static)
                AddToCollisions(d);
        }

        private void AddToCollisions(Sprite d)
        {
            SortedList<Point, List<Sprite>> list = d.Static ? allStatic : perTile;
            if (list == null)
            {
                SortForCollisions();
                return;
            }
            TileEnumerator te = new TileEnumerator(new RectangleF(d.X, d.Y, d.Width, d.Height));
            do
            {
                if (!list.ContainsKey(te.Current))
                    list.Add(te.Current, new List<Sprite>());
                list[te.Current].Add(d);
            } while (te.MoveNext());
            if (d.MultiplePositions)
            {
                for (int i = 0; i < d.Offsets.Count; i++)
                {
                    te = new TileEnumerator(new RectangleF(d.X + d.Offsets[i].X, d.Y + d.Offsets[i].Y, d.Width, d.Height));
                    do
                    {
                        if (!list.ContainsKey(te.Current))
                            list.Add(te.Current, new List<Sprite>());
                        list[te.Current].Add(d);
                    } while (te.MoveNext());
                }
            }
        }

        public void RemoveFromCollisions(Sprite d)
        {
            Remove(d);
            SortedList<Point, List<Sprite>> list = d.Static ? allStatic : perTile;
            if (list == null)
            {
                SortForCollisions();
                return;
            }
            TileEnumerator te = new TileEnumerator(new RectangleF(d.X, d.Y, d.Width, d.Height));
            do
            {
                if (list.ContainsKey(te.Current))
                    list[te.Current].Remove(d);
            } while (te.MoveNext());
            if (d.MultiplePositions)
            {
                for (int i = 0; i < d.Offsets.Count; i++)
                {
                    te = new TileEnumerator(new RectangleF(d.X + d.Offsets[i].X, d.Y + d.Offsets[i].Y, d.Width, d.Height));
                    do
                    {
                        if (list.ContainsKey(te.Current))
                            list[te.Current].Remove(d);
                    } while (te.MoveNext());
                }
            }
        }

        public void MoveForCollisions(Sprite d, RectangleF position)
        {
            SortedList<Point, List<Sprite>> list = d.Static ? allStatic : perTile;
            if (list == null)
            {
                SortForCollisions();
                return;
            }
            TileEnumerator te = new TileEnumerator(position);
            do
            {
                if (list.ContainsKey(te.Current))
                    list[te.Current].Remove(d);
            } while (te.MoveNext());
            if (d.MultiplePositions)
            {
                for (int i = 0; i < d.Offsets.Count; i++)
                {
                    te = new TileEnumerator(new RectangleF(d.X + d.Offsets[i].X, d.Y + d.Offsets[i].Y, d.Width, d.Height));
                    do
                    {
                        if (list.ContainsKey(te.Current))
                            list[te.Current].Remove(d);
                    } while (te.MoveNext());
                }
            }
            te = new TileEnumerator(new RectangleF(d.X, d.Y, d.Width, d.Height));
            do
            {
                if (!list.ContainsKey(te.Current))
                    list.Add(te.Current, new List<Sprite>());
                list[te.Current].Add(d);
            } while (te.MoveNext());
            if (d.MultiplePositions)
            {
                for (int i = 0; i < d.Offsets.Count; i++)
                {
                    te = new TileEnumerator(new RectangleF(d.X + d.Offsets[i].X, d.Y + d.Offsets[i].Y, d.Width, d.Height));
                    do
                    {
                        if (!list.ContainsKey(te.Current))
                            list.Add(te.Current, new List<Sprite>());
                        list[te.Current].Add(d);
                    } while (te.MoveNext());
                }
            }
        }

        public new void Add(Sprite d)
        {
            base.Insert(AddIndex(d), d);
            if (!d.Static) ToProcess.Add(d);
            else AddToCollisions(d);
            if (d is Tile) setBuffer = true;
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
            {
                RemoveAt(index);
                if (!d.Static) ToProcess.Remove(d);
                if (d is Tile)
                    setBuffer = true;
            }
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
