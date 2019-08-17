﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenGL;

namespace OpGL
{
    public class Sprite
    {
        protected bool flipX;
        protected bool flipY;
        protected Platform onPlatform;

        public string Name { get; set; } = "";

        public Color Color { get; set; } = Color.White;

        public Animation Animation { get; set; } = Animation.EmptyAnimation;
        private int _animFrame;
        private Point _old = new Point(0, 0);
        protected Point animationOffset = new Point(0, 0);

        public int Layer = 0;
        private int animFrame
        {
            get => _animFrame;
            set
            {
                Point n = Animation.GetFrame(_animFrame = value);
                n.X += animationOffset.X;
                n.Y += animationOffset.Y;
                if (n != _old)
                {
                    TexMatrix.Translate((n.X - _old.X), (n.Y - _old.Y), 0f);
                    _old = n;
                }
            }
        }
        public float X { get; set; }
        public float Y { get; set; }
        public List<PointF> Offsets = new List<PointF>();
        public bool MultiplePositions;
        public float Right { get => X + Width; set => X = value - Width; }
        public float Bottom { get => Y + Height; set => Y = value - Height; }
        public float CenterX { get => X + Width / 2; set => X = value - Width / 2; }
        public float CenterY { get => Y + Height / 2; set => Y = value - Height / 2; }
        public float PreviousX { get; set; }
        public float PreviousY { get; set; }
        public virtual float Width { get => Animation.Hitbox.Width; }
        public virtual float Height { get => Animation.Hitbox.Height; }

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

        public bool Immovable { get; set; }
        /// <summary>
        /// Determines whether the Drawable will process even while out of the screen. Too many objects that always process could slow down the game.
        /// </summary>
        //public bool AlwaysProcess { get; set; } = false;
        public bool KillCrewmen { get; set; } = false;
        public Texture Texture { get; protected set; }
        public virtual uint VAO { get => Texture.baseVAO; set { } }

        public Matrix4x4f LocMatrix;
        public Matrix4x4f TexMatrix;

        public Sprite(float x, float y, Texture texture, int texX, int texY, Color? color = null)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;

            Texture = texture;
            TexMatrix = Matrix4x4f.Scaled(texture.TileSize / texture.Width, texture.TileSize / texture.Height, 1f);
            TexMatrix.Translate(texX, texY, 0f);

            Animation = new Animation(new Point[] { new Point(texX, texY) }, new Rectangle(0, 0, texture.TileSize, texture.TileSize), texture);
            _old = Animation.GetFrame(0);
            Color = color ?? Color.White;
        }

        public Sprite(float x, float y, Texture texture, Animation animation)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;

            Texture = texture;
            TexMatrix = Matrix4x4f.Scaled(texture.TileSize / texture.Width, texture.TileSize / texture.Height, 1f);

            Animation = animation;
            _old = Animation.GetFrame(0);
            Point p = Animation.GetFrame(animFrame);
            TexMatrix.Translate(p.X, p.Y, 0f);
        }

        public bool Within(float x, float y, float width, float height, float offsetX = 0, float offsetY = 0)
        {
            return Right + offsetX > x && X + offsetX < x + width
                && Bottom + offsetY > y && Y + offsetY < y + height;
        }
        public bool IsOverlapping(Sprite other)
        {
            bool ret = Within(other.X, other.Y, other.Width, other.Height);
            if (!ret && MultiplePositions)
            {
                foreach (PointF offset in Offsets)
                {
                    if (ret = Right + offset.X > other.X && X + offset.X < other.Right
                    && Bottom + offset.Y > other.Y && Y + offset.Y < other.Bottom) break;
                }
            }
            if (!ret && other.MultiplePositions)
            {
                foreach (PointF offsetO in other.Offsets)
                {
                    ret = Within(other.X + offsetO.X, other.Y + offsetO.Y, other.Width, other.Height);
                    if (!ret && MultiplePositions)
                    {
                        foreach (PointF offset in Offsets)
                        {
                            if (ret = Right + offset.X > other.X + offsetO.X && X + offset.X < other.Right + offsetO.X
                            && Bottom + offset.Y > other.Y + offsetO.Y && Y + offset.Y < other.Bottom + offsetO.Y) break;
                        }
                        if (ret) break;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Performs OpenGL bindings and uniform gets/updates before drawing.
        /// </summary>
        public virtual void SafeDraw()
        {
            if (!Visible) return;
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(VAO);

            int modelLoc = Texture.Program.ModelLocation;
            Gl.UniformMatrix4f(modelLoc, 1, false, LocMatrix);
            int texLoc = Texture.Program.TexLocation;
            Gl.UniformMatrix4f(texLoc, 1, false, TexMatrix);
            int colorLoc = Texture.Program.ColorLocation;
            Gl.Uniform4f(colorLoc, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));

            UnsafeDraw();
        }
        // update model matrix
        public virtual void RenderPrep()
        {
            LocMatrix = Matrix4x4f.Translated((int)X - Animation.Hitbox.X, (int)Y - Animation.Hitbox.Y, 0);
            if (flipX)
            {
                LocMatrix.Scale(-1, 1, 1);
                LocMatrix.Translate(-Animation.Hitbox.X * 2 - Width, 0, 0);
            }
            if (flipY)
            {
                LocMatrix.Scale(1, -1, 1);
                LocMatrix.Translate(0, -Animation.Hitbox.Y * 2 - Height, 0);
            }
        }
        // Just the render call; everything should be set up before calling this.
        public virtual void UnsafeDraw()
        {
            Gl.DrawArrays(PrimitiveType.Polygon, 0, 4);
            if (MultiplePositions)
            {
                foreach (PointF offset in Offsets)
                {
                    LocMatrix.Translate(offset.X * (flipX ? -1 : 1), offset.Y * (flipY ? -1 : 1), 0);
                    Gl.UniformMatrix4f(Texture.Program.ModelLocation, 1, false, LocMatrix);
                    Gl.DrawArrays(PrimitiveType.Polygon, 0, 4);
                }
            }
        }

        public void ResetAnimation()
        {
            animFrame = 0;
        }

        public virtual bool FlipX { get => flipX; set => flipX = value; }
        public virtual bool FlipY { get => flipY; set => flipY = value; }

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

        public virtual void CollideY(float distance, Sprite collision)
        {
            Y -= distance;
        }
        public virtual void CollideX(float distance, Sprite collision)
        {
            X -= distance;
        }

        public virtual CollisionData TestCollision(Sprite testFor)
        {
            if (testFor == this || Immovable) return null;

            CollisionData ret = null;
            if ((Solid == SolidState.Entity || Solid == SolidState.Ground) && testFor.Solid == SolidState.Ground)
            {
                if (IsOverlapping(testFor))
                    ret = GetCollisionData(testFor);
            }
            return ret;
        }
        protected CollisionData GetCollisionData(Sprite testFor)
        {
            for (int i = -1; i < Offsets.Count; i++)
            {
                float ofX = i > -1 ? Offsets[i].X : 0;
                float ofY = i > -1 ? Offsets[i].Y : 0;
                for (int j = -1; j < testFor.Offsets.Count; j++)
                {
                    float ofXO = j > -1 ? testFor.Offsets[j].X : 0;
                    float ofYO = j > -1 ? testFor.Offsets[j].Y : 0;
                    if (!testFor.Within(X + ofX, Y + ofY, Width, Height, ofXO, ofYO)) continue;
                    // check for vertical collision first
                    // top
                    if (PreviousY + Height + ofY <= testFor.PreviousY + ofYO)
                        return new CollisionData(true, Bottom + ofY - (testFor.Y + ofYO), testFor);
                    // bottom
                    else if (PreviousY + ofY >= testFor.PreviousY + testFor.Height + ofYO)
                        return new CollisionData(true, Y + ofY - (testFor.Bottom + ofYO), testFor);
                    // right
                    else if (PreviousX + Width + ofX <= testFor.PreviousX + ofXO)
                        return new CollisionData(false, Right + ofX - (testFor.X + ofXO), testFor);
                    // left
                    else if (PreviousX + ofX >= testFor.PreviousX + testFor.Width + ofXO)
                        return new CollisionData(false, X + ofX - (testFor.Right + ofXO), testFor);
                    if (!testFor.MultiplePositions) break;
                }
                if (!MultiplePositions) break;
            }

            return null;
        }

        /// <summary>
        /// Get the collision that should happen fist.
        /// That is the highest-distance horizontal collision; if none, the highest-distance vertical collision.
        /// </summary>
        public virtual CollisionData GetFirstCollision(List<CollisionData> collisions)
        {
            if (collisions.Count == 0)
                return null;

            CollisionData ret = collisions[0];
            for (int i = 1; i < collisions.Count; i++)
            {
                CollisionData dt = collisions[i];
                if (!dt.Vertical)
                {
                    if (ret.Vertical || Math.Abs(dt.Distance) > Math.Abs(ret.Distance))
                        ret = dt;
                }
                else if (ret.Vertical)
                {
                    if (Math.Abs(dt.Distance) > Math.Abs(ret.Distance))
                        ret = dt;
                }
            }
            return ret;
        }

        public virtual void Collide(CollisionData cd)
        {
            if (cd.Vertical)
            {
                CollideY(cd.Distance, cd.CollidedWith);
            }
            else
            {
                CollideX(cd.Distance, cd.CollidedWith);
            }
        }

        public virtual void HandleCrewmanCollision(Crewman crewman)
        {
            //Do nothing
        }

        protected void write(string name, object s, JTokenWriter writer)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(s);
        }

        public virtual JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "Sprite");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            return ret;
        }
    }

}
