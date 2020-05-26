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
    public class GravityLine : Sprite, IBoundSprite
    {
        protected override bool AlwaysCollide => true;
        private List<Crewman> touching = new List<Crewman>();

        protected int length = 0;

        public float XVel { get; set; }
        public float YVel { get; set; }
        private Rectangle _bounds;
        public Rectangle Bounds { get => _bounds; set => _bounds = value; }
        public static SoundEffect Sound;

        public override float Width => Horizontal ? length * Texture.TileSizeX : base.Width;
        public override float Height => Horizontal ? base.Height : length * Texture.TileSizeY;

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
                bufferData[index++] = Horizontal ? curL * Texture.TileSizeX : 0;
                bufferData[index++] = Horizontal ? 0 : curL * Texture.TileSizeY;
                bufferData[index++] = length == 1 ? 1 : (curL == 0 ? 0 : (curL == length - 1 ? 2 : 1));
                bufferData[index++] = 0;
                curL += 1;
            }

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

            int modelLoc = Texture.Program.ModelLocation;
            Gl.UniformMatrix4f(modelLoc, 1, false, LocMatrix);
            int texLoc = Texture.Program.TexLocation;
            Gl.UniformMatrix4f(texLoc, 1, false, TexMatrix);
            int colorLoc = Texture.Program.ColorLocation;
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
                Sound?.Play();
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
            X += XVel;
            Y += YVel;
            CheckBounds();
        }

        public void CheckBounds()
        {
            if (_bounds.Width > 0 && _bounds.Height > 0)
            {
                if (Right % Room.ROOM_WIDTH > _bounds.X + _bounds.Width)
                {
                    float x = Right % Room.ROOM_WIDTH - (_bounds.X + _bounds.Width);
                    X -= x;
                    XVel *= -1;
                }
                else if (X % Room.ROOM_WIDTH < _bounds.X)
                {
                    float x = X % Room.ROOM_WIDTH - _bounds.X;
                    X -= x;
                    XVel *= -1;
                }
                else if (Bottom % Room.ROOM_HEIGHT > _bounds.Y + _bounds.Height)
                {
                    float y = Bottom % Room.ROOM_HEIGHT - (_bounds.Y + _bounds.Height);
                    Y -= y;
                    YVel *= -1;
                }
                else if (Y % Room.ROOM_HEIGHT < _bounds.Y)
                {
                    float y = Y % Room.ROOM_HEIGHT - _bounds.Y;
                    Y -= y;
                    YVel *= -1;
                }
            }
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "GravityLine");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Horizontal", Horizontal);
        //    ret.Add("Length", LengthTiles);
        //    ret.Add("Animation", Animation.Name);
        //    ret.Add("XSpeed", XSpeed);
        //    ret.Add("YSpeed", YSpeed);
        //    ret.Add("BoundsX", Bounds.X);
        //    ret.Add("BoundsY", Bounds.Y);
        //    ret.Add("BoundsWidth", Bounds.Width);
        //    ret.Add("BoundsHeight", Bounds.Height);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("Horizontal", new SpriteProperty("Horizontal", () => Horizontal, (t, g) => Horizontal = (bool)t, true, SpriteProperty.Types.Bool, "Whether the gravity line is horizontal or not."));
                ret.Add("Length", new SpriteProperty("Length", () => LengthTiles, (t, g) => LengthTiles = (int)t, 1, SpriteProperty.Types.Int, "The length in tiles of the gravity line."));
                ret.Add("XSpeed", new SpriteProperty("XSpeed", () => XVel, (t, g) => XVel = (float)t, 0f, SpriteProperty.Types.Float, "The X speed in pixels/frame of the gravity line."));
                ret.Add("YSpeed", new SpriteProperty("YSpeed", () => YVel, (t, g) => YVel = (float)t, 0f, SpriteProperty.Types.Float, "The Y speed in pixels/frame of the gravity line."));
                ret.Add("BoundsX", new SpriteProperty("BoundsX", () => _bounds.X, (t, g) => _bounds.X = (int)t, 0, SpriteProperty.Types.Int, "The left edge of the gravity line's bounds."));
                ret.Add("BoundsY", new SpriteProperty("BoundsY", () => _bounds.Y, (t, g) => _bounds.Y = (int)t, 0, SpriteProperty.Types.Int, "The top edge of the gravity line's bounds."));
                ret.Add("BoundsWidth", new SpriteProperty("BoundsWidth", () => _bounds.Width, (t, g) => _bounds.Width = (int)t, 0, SpriteProperty.Types.Int, "The width of the gravity line's bounds."));
                ret.Add("BoundsHeight", new SpriteProperty("BoundsHeight", () => _bounds.Height, (t, g) => _bounds.Height = (int)t, 0, SpriteProperty.Types.Int, "The height of the gravity line's bounds."));
                ret["Type"].GetValue = () => "GravityLine";
                return ret;
            }
        }
    }
}
