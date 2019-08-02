using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenGL;

namespace OpGL
{
    class DrawableCollection : List<Drawable>
    {
        public DrawableCollection() : base() { }
        public DrawableCollection(int capacity) : base(capacity) { }
        public DrawableCollection(IEnumerable<Drawable> drawables) : base()
        {
            AddRange(drawables);
        }

        public void Render()
        {
            int modelLoc = -1;
            int texLoc = -1;
            int colorLoc = -1;

            Texture lastTex = null;
            uint lastProgram = uint.MaxValue;
            long lastColor = long.MinValue;
            for (int i = 0; i < Count; i++)
            {
                Drawable d = this[i];
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
                        Gl.UseProgram(lastProgram);
                    }
                }
                if (lastColor != d.Color.ToArgb())
                    Gl.Uniform4f(colorLoc, 1, new Vertex4f((float)d.Color.R / 255, (float)d.Color.G / 255, (float)d.Color.B / 255, (float)d.Color.A / 255));

                Gl.UniformMatrix4f(modelLoc, 1, false, d.LocMatrix);
                Gl.UniformMatrix4f(texLoc, 1, false, d.TexMatrix);
                d.UnsafeDraw();
            }
        }

        private int DrawableCompare(Drawable d1, Drawable d2)
        {
            int t = d1.Texture.ID.CompareTo(d2.Texture.ID);
            if (t == 0)
                return d1.Color.ToArgb().CompareTo(d2.Color.ToArgb());
            else
                return t;
        }

        /// <summary>
        /// Returns an index at which a drawable can be inserted while keeping the list sorted.
        /// </summary>
        private int AddIndex(Drawable d)
        {
            int min = 0, max = Count - 1;
            int index = (max - min) / 2 + min;
            while (min < max)
            {
                int r = DrawableCompare(d, this[index]);
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

        public new void Add(Drawable d)
        {
            base.Insert(AddIndex(d), d);
        }
        public new void AddRange(IEnumerable<Drawable> drawables)
        {
            foreach (Drawable d in drawables)
                Add(d);
        }

        /// <summary>
        /// Do not support setting an element.
        /// </summary>
        public new Drawable this[int index]
        {
            get => base[index];
        }

        // TODO: Implement Remove and IndexOf methods using a binary search.

        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Insert(int index, Drawable d)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void InsertRange(int index, IEnumerable<Drawable> drawables)
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
            base.Sort(DrawableCompare);
        }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Sort(Comparison<Drawable> comparison)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Sort(IComparer<Drawable> comparer)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        public new void Sort(int index, int count, IComparer<Drawable> comparer)
        { throw new NotSupportedException("DrawableCollection is a sorted list."); }
    }
}
