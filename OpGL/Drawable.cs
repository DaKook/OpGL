using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenGL;

namespace OpGL
{
    public class Drawable
    {
        public string Name { get; set; } = "";

        public Color Color { get; set; }

        public Animation Animation { get; set; } = Animation.EmptyAnimation;
        private int animFrame = 0;

        private float _X;
        public float X
        {
            get { return _X; }
            set
            {
                LocMatrix.Translate(value - _X, 0f, 0f);
                _X = value;
            }
        }
        private float _Y;
        public float Y
        {
            get { return _Y; }
            set
            {
                LocMatrix.Translate(0f, value - _Y, 0f);
                _Y = value;
            }
        }

        public int TextureX
        {
            get
            {
                return Animation.GetFrame(animFrame).X;
            }
        }
        public int TextureY
        {
            get
            {
                return Animation.GetFrame(animFrame).Y;
            }
        }

        public bool Visible { get; set; } = true;
        public enum SolidState {
            /// <summary>
            /// Solid for anything except NonSolid.
            /// </summary>
            Ground = 0,
            /// <summary>
            /// Goes through other entities, but is stopped by Ground.
            /// </summary>
            Entity = 1,
            /// <summary>
            /// Stops entities, and kills crewmen.
            /// </summary>
            KillSolid = 2,
            /// <summary>
            /// Goes through entities, but is stopped by Ground, and kills crewmen.
            /// </summary>
            KillEntity = 3,
            /// <summary>
            /// Goes through everything. Typically will be static.
            /// </summary>
            NonSolid = 4 }
        /// <summary>
        /// Gets or sets the solid state of the Drawable.
        /// </summary>
        public SolidState Solid { get; set; } = SolidState.Entity;
        /// <summary>
        /// Gets or sets the magnitude of gravity to be applied to the Drawable. Negative values will cause it to fall up.
        /// </summary>
        public float Gravity { get; set; }
        /// <summary>
        /// When set to true, the Drawable will not have its own collision detection. Other Drawables will still test for collision with Static Drawables.
        /// </summary>
        public bool Static { get; set; }
        /// <summary>
        /// Determines whether the Drawable will process even while out of the screen. Too many objects that always process could slow down the game.
        /// </summary>
        public bool AlwaysProcess { get; set; } = false;
        public Texture Texture { get; internal set; }

        private Matrix4x4f LocMatrix = Matrix4x4f.Identity;
        private Matrix4x4f TexMatrix = Matrix4x4f.Identity;

        internal Drawable()
        {

        }

        public Drawable(float x, float y, Texture texture, int texX, int texY, Color? color = null)
        {
            _X = x;
            _Y = y;
            LocMatrix = Matrix4x4f.Translated(x, y, 0);
            Texture = texture;
            float dw = 1f / texture.Width;
            float dh = 1f / texture.Height;
            TexMatrix.Scale(dw, dh, 1f);
            TexMatrix.Translate(texX * texture.TileSize, texY * texture.TileSize, 0f);
            Animation = new Animation(new Point[] { new Point(texX, texY) }, Rectangle.Empty, texture);
            Color = color ?? Color.White;
        }

        public Drawable(float x, float y, Texture texture, Animation animation)
        {
            X = x;
            Y = y;

            Texture = texture;
            float dw = 1f / texture.Width;
            float dh = 1f / texture.Height;
            TexMatrix.Scale(dw, dh, 1f);

            Animation = animation;
            Point p = Animation.GetFrame(animFrame);
            TexMatrix.Translate(p.X, p.Y, 0f);
        }

        public virtual void Draw()
        {
            if (!Visible) return;
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(Texture.VAO);

            int modelLoc = Gl.GetUniformLocation(Texture.Program, "model");
            Gl.UniformMatrix4f(modelLoc, 1, false, LocMatrix);
            int texLoc = Gl.GetUniformLocation(Texture.Program, "texMatrix");
            Gl.UniformMatrix4f(texLoc, 1, false, TexMatrix);
            int colorLoc = Gl.GetUniformLocation(Texture.Program, "color");
            Gl.Uniform4f(colorLoc, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));
            
            Gl.DrawArrays(PrimitiveType.Polygon, 0, 4);
        }

        public virtual void Process()
        {
            //Advance animation frame and change TextureX and TextureY accordingly
            Point old = Animation.GetFrame(animFrame);
            if (++animFrame >= Animation.FrameCount) animFrame = Animation.LoopStart;
            Point n = Animation.GetFrame(animFrame);
            if (old != n)
                TexMatrix.Translate((n.X - old.X), (n.Y - old.Y), 0f);
        }
    }

}
