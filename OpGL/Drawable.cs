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
        private bool _flipX;
        protected bool flipX
        {
            get => _flipX;
            set
            {
                if (value != _flipX)
                {
                    _flipX = value;
                    TexMatrix.Scale(-1, 1, 1);
                    TexMatrix.Translate((float)(-2 * TextureX) - 1 - ((float)(Animation.Hitbox.X - (Texture.TileSize - Animation.Hitbox.X - Animation.Hitbox.Width)) / Texture.TileSize), 0, 0);
                }
            }
        }
        private bool _flipY;
        protected bool flipY
        {
            get => _flipY;
            set
            {
                if (value != _flipY)
                {
                    _flipY = value;
                    TexMatrix.Scale(1, -1, 1);
                    TexMatrix.Translate(0, (float)(-2 * TextureY) - 1 - ((float)(Animation.Hitbox.Y - (Texture.TileSize - Animation.Hitbox.Y - Animation.Hitbox.Height)) / Texture.TileSize), 0);
                }
            }
        }
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
                    if ((flipX ? -1 : 1) * (n.X - _old.X) > 1 || (flipY ? -1 : 1) * (n.Y - _old.Y) > 1)
                        ;
                    TexMatrix.Translate((flipX ? -1 : 1) * (n.X - _old.X), (flipY ? -1 : 1) * (n.Y - _old.Y), 0f);
                    _old = n;
                }
            }
        }

        private float _X;
        public float X
        {
            get { return _X; }
            set
            {
                PreviousX = _X;
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
                PreviousY = _Y;
                LocMatrix.Translate(0f, value - _Y, 0f);
                _Y = value;
            }
        }
        public float PreviousX { get; private set; }
        public float PreviousY { get; private set; }
        public float HitX { get => X + Animation.Hitbox.X; }
        public float HitY { get => Y + Animation.Hitbox.Y; }
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

        internal Matrix4x4f LocMatrix = Matrix4x4f.Identity;
        internal Matrix4x4f TexMatrix = Matrix4x4f.Identity;

        internal Drawable()
        {

        }

        public Drawable(float x, float y, Texture texture, int texX, int texY, Color? color = null)
        {
            _X = x;
            _Y = y;
            PreviousX = x;
            PreviousY = y;
            LocMatrix = Matrix4x4f.Translated(x, y, 0);

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
            return HitX + Animation.Hitbox.Width > x && HitX < x + width
                // this.bottom > o.top                   this.top < o.bottom
                && HitY + Animation.Hitbox.Height > y && HitY < y + height;
        }
        public bool IsOverlapping(Drawable other)
        {
            return Within(other.HitX, other.HitY, other.Animation.Hitbox.Width, other.Animation.Hitbox.Height);
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
        }

        public virtual void CollideY(float distance)
        {
            Y -= distance;
        }
        public virtual void CollideX(float distance)
        {
            X -= distance;
        }
    }

}
