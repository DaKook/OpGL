using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    public class GravityLine : Sprite
    {
        private List<Crewman> touching = new List<Crewman>();

        protected int length = 0;

        public float XSpeed;
        public float YSpeed;
        public Rectangle Bounds;

        public override float Width => Horizontal ? length * Texture.TileSize : base.Width;
        public override float Height => Horizontal ? base.Height : length * Texture.TileSize;

        public bool Horizontal;

        public int LengthTiles
        {
            get => length;
            set
            {
                length = value;
                SetBuffer();
            }
        }

        public GravityLine(float x, float y, Texture texture, Animation animation, bool horizontal, int lengthTiles) : base(x, y, texture, animation)
        {
            Horizontal = horizontal;
            LengthTiles = lengthTiles;
            Solid = SolidState.NonSolid;
        }

        private void SetBuffer()
        {
            bufferData = new float[length * 4];

            float curL = 0;
            int index = 0;
            while (index < bufferData.Length)
            {
                bufferData[index++] = Horizontal ? curL * Texture.TileSize : 0;
                bufferData[index++] = Horizontal ? 0 : curL * Texture.TileSize;
                bufferData[index++] = curL == 0 ? 0 : (curL == length - 1 ? 2 : 1);
                bufferData[index++] = 0;
                curL += 1;
            }
            length = index / 4;
            Array.Resize(ref bufferData, index);

            updateBuffer = true;
        }

        protected float[] bufferData;
        protected uint ibo;
        protected bool updateBuffer = true;
        protected bool firstRender = true;

        public override uint VAO { get; set; }

        /// <summary>
        /// Performs OpenGL bindings and uniform gets/updates before drawing.
        /// </summary>
        public override void SafeDraw()
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
        // Just the render call and any set-up StringDrawable requires but a regular Drawable doesn't.
        public override void UnsafeDraw()
        {
            if (updateBuffer)
                UpdateBuffer();

            Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, length);
        }

        protected void UpdateBuffer()
        {
            if (firstRender)
            {
                firstRender = false;

                VAO = Gl.CreateVertexArray();
                Gl.BindVertexArray(VAO);

                Gl.BindBuffer(BufferTarget.ArrayBuffer, Texture.baseVBO);
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
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)bufferData.Length * sizeof(float), bufferData, BufferUsage.DynamicDraw);
            updateBuffer = false;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (!touching.Contains(crewman))
            {
                touching.Add(crewman);
                crewman.Gravity *= -1;
                crewman.YVelocity = 0;
                Color = Color.Gray;
            }
        }

        public override void Process()
        {
            base.Process();
            for (int i = 0; i < touching.Count; i++)
            {
                if (!IsOverlapping(touching[i]))
                {
                    touching.RemoveAt(i);
                    if (touching.Count == 0)
                        Color = Color.White;
                }
            }
            X += XSpeed;
            Y += YSpeed;
            CheckBounds();
        }

        public void CheckBounds()
        {
            if (Bounds.Width > 0 && Bounds.Height > 0)
            {
                if (Right > Bounds.X + Bounds.Width)
                {
                    Right = Bounds.X + Bounds.Width;
                    XSpeed *= -1;
                }
                else if (X < Bounds.X)
                {
                    X = Bounds.X;
                    XSpeed *= -1;
                }
                else if (Bottom > Bounds.Y + Bounds.Height)
                {
                    Bottom = Bounds.Y + Bounds.Height;
                    YSpeed *= -1;
                }
                else if (Y < Bounds.Y)
                {
                    Y = Bounds.Y;
                    YSpeed *= -1;
                }
            }
        }

        public override JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "GravityLine");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            ret.Add("Horizontal", Horizontal);
            ret.Add("Length", LengthTiles);
            ret.Add("Animation", Animation.Name);
            ret.Add("XSpeed", XSpeed);
            ret.Add("YSpeed", YSpeed);
            ret.Add("BoundsX", Bounds.X);
            ret.Add("BoundsY", Bounds.Y);
            ret.Add("BoundsWidth", Bounds.Width);
            ret.Add("BoundsHeight", Bounds.Height);
            return ret;
        }
    }
}
