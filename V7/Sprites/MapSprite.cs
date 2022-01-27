using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
//using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace V7
{
    class MapSprite : RectangleSprite, IMapSprite
    {
        public override void Dispose()
        {
            GL.DeleteBuffer(ibo);
            GL.DeleteVertexArray(VAO);
        }

        public PointF Target;
        public bool GoToTarget = false;
        private float width;
        private float height;
        public override float Width => width;
        public override float Height => height;
        private Color realColor;
        private bool isWhite;

        public float FadeSpeed;
        private float fadePosition = 100;
        private float MaxWidth;
        private float MaxHeight;
        private int delayFrames = 0;
        private int div = 2;
        public Action FinishFading { get; set; }
        public bool IsWhite
        {
            get => isWhite;
            set
            {
                if (!isWhite)
                    realColor = Color;
                isWhite = value;
                if (value)
                    Color = Color.White;
                else
                    Color = realColor;
            }
        }
        public MapSprite() : base(0, 0, 1, 1)
        {

        }

        public void SetTarget(float x, float y)
        {
            Target = new PointF(x, y);
            GoToTarget = true;
        }

        public void FadeIn()
        {
            fadePosition = 0;
            FadeSpeed = 2.5f;
        }

        public void FadeOut()
        {
            fadePosition = 100;
            FadeSpeed = -2.5f;
        }

        public void EnterFrom(PointF location)
        {
            div = 4;
            SetTarget(X, Y);
            X += location.X;
            Y += location.Y;
        }

        public void Delay(int frames)
        {
            delayFrames = frames;
        }

        public override void Process()
        {
            if (delayFrames <= 0)
            {
                if (fadePosition > 0 && FadeSpeed < 0)
                {
                    fadePosition -= (int)Math.Ceiling(fadePosition / 5);
                    if (fadePosition <= 0)
                    {
                        fadePosition = 0;
                        FadeSpeed = 0;
                        FinishFading?.Invoke();
                        FinishFading = null;
                    }
                }
                else if (fadePosition < 100 && FadeSpeed > 0)
                {
                    fadePosition += (int)Math.Ceiling((100 - fadePosition) / 5);
                    if (fadePosition >= 100)
                    {
                        fadePosition = 100;
                        FadeSpeed = 0;
                        FinishFading?.Invoke();
                        FinishFading = null;
                    }
                }
                if (GoToTarget)
                {
                    bool xg = X < Target.X;
                    bool yg = Y < Target.Y;
                    bool xc = false;
                    bool yc = false;
                    if (xg)
                    {
                        X += (int)Math.Ceiling((Target.X - X) / div);
                        if (X >= Target.X)
                        {
                            X = Target.X;
                            xc = true;
                        }
                    }
                    else
                    {
                        X += (int)Math.Floor((Target.X - X) / div);
                        if (X <= Target.X)
                        {
                            X = Target.X;
                            xc = true;
                        }
                    }
                    if (yg)
                    {
                        Y += (int)Math.Ceiling((Target.Y - Y) / div);
                        if (Y >= Target.Y)
                        {
                            Y = Target.Y;
                            yc = true;
                        }
                    }
                    else
                    {
                        Y += (int)Math.Floor((Target.Y - Y) / div);
                        if (Y <= Target.Y)
                        {
                            Y = Target.Y;
                            yc = true;
                        }
                    }
                    if (xc && yc)
                    {
                        GoToTarget = false;
                        div = 2;
                        FinishFading?.Invoke();
                        FinishFading = null;
                    }
                }
            }
            else
                delayFrames -= 1;
            Color = Color.FromArgb((int)(fadePosition * 255 / 100), Color.R, Color.G, Color.B);
            width = (0.5f + (0.5f * (fadePosition / 100))) * MaxWidth;
            height = (0.5f + (0.5f * (fadePosition / 100))) * MaxHeight;
            base.SetSize(width / 40, width / 40);
        }

        public static MapSprite FromRoom(JToken room, float width, Game game)
        {
            MapSprite ret = new MapSprite();
            JArray tArr = room["Tiles"] as JArray;
            TileTexture texture = game.TextureFromName((string)room["TileTexture"]) as TileTexture;
            int clr = (int)(room["Color"] ?? -1);
            List<float> bd = new List<float>();
            if (tArr != null)
            {
                int x = 0;
                int y = 0;
                for (int i = 0; i < tArr.Count;)
                {
                    int l = (int)tArr[i++];
                    if (l == -1)
                    {
                        i++;
                        Tile layeredTile = new Tile(x, y, texture, (int)tArr[i++], (int)tArr[i++]);
                        i++;
                        if (layeredTile.Solid == SolidState.Ground)
                        {
                            bd.Add(layeredTile.X);
                            bd.Add(layeredTile.Y);
                            bd.Add(0);
                            bd.Add(0);
                        }
                    }
                    else if (l == -2)
                    {
                        y += 1;
                        if (y >= Room.ROOM_HEIGHT / 8)
                        {
                            y = 0;
                            x += 1;
                        }
                    }
                    else if (l == -3)
                    {
                        int empty = (int)tArr[i++];
                        for (int j = 0; j < empty; j++)
                        {
                            y += 1;
                            if (y >= Room.ROOM_HEIGHT / 8)
                            {
                                y = 0;
                                x += 1;
                            }
                        }
                    }
                    else
                    {
                        i -= 1;
                        Tile tile = new Tile(x, y, texture, (int)tArr[i++], (int)tArr[i++]);
                        i++;
                        if (tile.Solid == SolidState.Ground)
                        {
                            bd.Add(tile.X);
                            bd.Add(tile.Y);
                            bd.Add(0);
                            bd.Add(0);
                        }
                        y += 1;
                        if (y >= Room.ROOM_HEIGHT / 8)
                        {
                            y = 0;
                            x += 1;
                        }
                    }
                }
            }
            ret.width = width;
            ret.height = width * 3 / 4;
            ret.bufferData = bd.ToArray();
            ret.instances = bd.Count / 4;
            ret.SetSize(width / 40, width / 40);
            System.Drawing.Color c = System.Drawing.Color.FromArgb(clr);
            ret.Color = Color.FromArgb(c.A, c.R, c.G, c.B);
            return ret;
        }

        public override void RenderPrep()
        {
            double x = DX, y = DY;
            if (width != MaxWidth)
            {
                DX += (MaxWidth - width) / 2;
                DY += (MaxHeight - height) / 2;
            }
            base.RenderPrep();
            DX = x;
            DY = y;
        }

        public override void SetSize(float width, float height)
        {
            this.width = width;
            this.height = height;
            base.SetSize(width / 40, width / 40);
            MaxWidth = this.width;
            MaxHeight = this.height;
        }

        protected int instances = 0;

        protected float[] bufferData;
        protected uint ibo;
        protected bool updateBuffer = true;
        protected bool firstRender = true;

        public override int VAO { get; set; }

        public override void UnsafeDraw()
        {
            if (updateBuffer)
                UpdateBuffer();

            GL.DrawArraysInstanced(PrimitiveType.TriangleFan, 0, 4, instances);
        }

        protected void UpdateBuffer()
        {
            if (firstRender)
            {
                firstRender = false;

                int vao;
                GL.CreateVertexArrays(1, out vao);
                VAO = vao;
                GL.BindVertexArray(VAO);

                GL.BindBuffer(BufferTarget.ArrayBuffer, BaseVBO);
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)0);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                GL.EnableVertexAttribArray(0);
                GL.EnableVertexAttribArray(1);

                GL.CreateBuffers(1, out ibo);
                GL.BindBuffer(BufferTarget.ArrayBuffer, ibo);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)0);
                GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                GL.EnableVertexAttribArray(2);
                GL.EnableVertexAttribArray(3);
                GL.VertexAttribDivisor(2, 1);
                GL.VertexAttribDivisor(3, 1);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ArrayBuffer, bufferData.Length * sizeof(float), bufferData, BufferUsageHint.DynamicDraw);
            updateBuffer = false;
        }

        public IMapSprite Clone()
        {
            MapSprite ms = new MapSprite();
            ms.width = width;
            ms.height = height;
            ms.bufferData = bufferData;
            ms.instances = instances;
            ms.SetSize(width / 40, width / 40);
            ms.Color = Color;
            ms.VAO = VAO;
            ms.ibo = ibo;
            ms.firstRender = firstRender;
            ms.updateBuffer = updateBuffer;
            return ms;
        }
    }
}
