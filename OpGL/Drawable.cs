﻿using System;
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
        protected bool flipX;
        protected bool flipY;

        public string Name { get; set; } = "";

        public Color Color { get; set; } = Color.White;

        public Animation Animation { get; set; } = Animation.EmptyAnimation;
        private int _animFrame;
        private Point _old = new Point(0, 0);
        private int animFrame
        {
            get => _animFrame;
            set
            {
                Point n = Animation.GetFrame(_animFrame = value);
                if (n != _old)
                {
                    TexMatrix.Translate((n.X - _old.X), (n.Y - _old.Y), 0f);
                    _old = n;
                }
            }
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float PreviousX { get; private set; }
        public float PreviousY { get; private set; }
        public virtual bool IsCrewman { get => false; }

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
            /// Goes through everything. Typically will be static.
            /// </summary>
            NonSolid = 2 }
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
        public bool KillCrewmen { get; set; } = false;
        public Texture Texture { get; internal set; }
        internal virtual uint VAO { get => Texture.baseVAO; set { } }

        internal Matrix4x4f LocMatrix;
        internal Matrix4x4f TexMatrix;

        internal Drawable()
        {

        }

        public Drawable(float x, float y, Texture texture, int texX, int texY, Color? color = null)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;

            Texture = texture;
            TexMatrix = Matrix4x4f.Scaled(texture.TileSize / texture.Width, texture.TileSize / texture.Height, 1f);
            TexMatrix.Translate(texX, texY, 0f);

            Animation = new Animation(new Point[] { new Point(texX, texY) }, new Rectangle(0, 0, texture.TileSize, texture.TileSize), texture);
            Color = color ?? Color.White;
        }

        public Drawable(float x, float y, Texture texture, Animation animation)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;

            Texture = texture;
            float dw = 1f / texture.Width;
            float dh = 1f / texture.Height;
            TexMatrix = Matrix4x4f.Scaled(texture.TileSize / texture.Width, texture.TileSize / texture.Height, 1f);

            Animation = animation;
            Point p = Animation.GetFrame(animFrame);
            TexMatrix.Translate(p.X, p.Y, 0f);
        }

        public bool Within(float x, float y, float width, float height)
        {
            //     this.right > o.left                  this.left < o.right
            return X + Animation.Hitbox.Width > x && X < x + width
                // this.bottom > o.top                   this.top < o.bottom
                && Y + Animation.Hitbox.Height > y && Y < y + height;
        }
        public bool IsOverlapping(Drawable other)
        {
            return Within(other.X, other.Y, other.Animation.Hitbox.Width, other.Animation.Hitbox.Height);
        }

        /// <summary>
        /// Performs OpenGL bindings and uniform gets/updates before drawing.
        /// </summary>
        public virtual void SafeDraw()
        {
            if (!Visible) return;
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(VAO);

            int modelLoc = Gl.GetUniformLocation(Texture.Program, "model");
            Gl.UniformMatrix4f(modelLoc, 1, false, LocMatrix);
            int texLoc = Gl.GetUniformLocation(Texture.Program, "texMatrix");
            Gl.UniformMatrix4f(texLoc, 1, false, TexMatrix);
            int colorLoc = Gl.GetUniformLocation(Texture.Program, "color");
            Gl.Uniform4f(colorLoc, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));

            UnsafeDraw();
        }
        // update model matrix
        internal virtual void RenderPrep()
        {
            LocMatrix = Matrix4x4f.Translated(X - Animation.Hitbox.X, Y - Animation.Hitbox.Y, 0);
            if (flipX)
            {
                LocMatrix.Scale(-1, 1, 1);
                LocMatrix.Translate(-Animation.Hitbox.X * 2 - Animation.Hitbox.Width, 0, 0);
            }
            if (flipY)
            {
                LocMatrix.Scale(1, -1, 1);
                LocMatrix.Translate(0, -Animation.Hitbox.Y * 2 - Animation.Hitbox.Height, 0);
            }
        }
        // Just the render call; everything should be set up before calling this.
        internal virtual void UnsafeDraw()
        {
            Gl.DrawArrays(PrimitiveType.Polygon, 0, 4);
        }

        public void ResetAnimation()
        {
            animFrame = 0;
        }

        public virtual void Process()
        {
            //Advance animation frame and change TextureX and TextureY accordingly
            if (animFrame + 1 >= Animation.FrameCount)
                animFrame = Animation.LoopStart;
            else
                animFrame += 1;

            PreviousX = X;
            PreviousY = Y;
        }

        public virtual void CollideY(float distance)
        {
            Y -= distance;
        }
        public virtual void CollideX(float distance)
        {
            X -= distance;
        }

        public virtual void TestCollision(Drawable testFor)
        {
            // do not collide with self
            if (testFor == this) return;

            if (IsOverlapping(testFor))
            {
                // entity colliding with ground
                if ((Solid == SolidState.Entity || (Solid == SolidState.Ground && testFor.Static)) && testFor.Solid == SolidState.Ground)
                {
                    // check for vertical collision, if none then horizontal collision
                    // collide with top
                    if (PreviousY + Animation.Hitbox.Height <= testFor.PreviousY)
                        CollideY(Y + Animation.Hitbox.Height - testFor.Y);
                    // collide with bottom
                    else if (PreviousY >= testFor.PreviousY + testFor.Animation.Hitbox.Height)
                        CollideY(Y - (testFor.Y + testFor.Animation.Hitbox.Height));
                    else
                    {
                        // collide with left side
                        if (PreviousX + Animation.Hitbox.Width <= testFor.PreviousX)
                            CollideX(X + Animation.Hitbox.Width - testFor.X);
                        // collide with right side
                        else if (PreviousX >= testFor.PreviousX + testFor.Animation.Hitbox.Width)
                            CollideX(X - (testFor.X + testFor.Animation.Hitbox.Width));
                    }
                }
                else if (Solid == SolidState.Ground && testFor.Solid == SolidState.Ground)
                {

                }
            }
        }

        public virtual void TestAllCollisions(IEnumerable<Drawable> process)
        {
            foreach (Drawable testFor in process)
            {
                TestCollision(testFor);
            }
        }
    }

}
