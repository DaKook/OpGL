using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    class MapSprite : RectangleSprite
    {
        public override void Dispose()
        {
            //Gl.DeleteBuffers(ibo);
            //Gl.DeleteVertexArrays(VAO);
        }

        public PointF Target;
        public bool GoToTarget = false;
        private float width;
        private float height;
        public override float Width => width;
        public override float Height => height;
        public MapSprite() : base(0, 0, 1, 1)
        {

        }

        public void SetTarget(float x, float y)
        {
            Target = new PointF(x, y);
            GoToTarget = true;
        }

        public override void Process()
        {
            if (GoToTarget)
            {
                bool xg = X < Target.X;
                bool yg = Y < Target.Y;
                bool xc = false;
                bool yc = false;
                if (xg)
                {
                    X += (int)Math.Ceiling((Target.X - X) / 2);
                    if (X >= Target.X)
                    {
                        X = Target.X;
                        xc = true;
                    }
                }
                else
                {
                    X += (int)Math.Floor((Target.X - X) / 2);
                    if (X <= Target.X)
                    {
                        X = Target.X;
                        xc = true;
                    }
                }
                if (yg)
                {
                    Y += (int)Math.Ceiling((Target.Y - Y) / 2);
                    if (Y >= Target.Y)
                    {
                        Y = Target.Y;
                        yc = true;
                    }
                }
                else
                {
                    Y += (int)Math.Floor((Target.Y - Y) / 2);
                    if (Y <= Target.Y)
                    {
                        Y = Target.Y;
                        yc = true;
                    }
                }
                if (xc && yc)
                {
                    GoToTarget = false;
                }
            }
        }

        public static MapSprite FromRoom(JToken room, float width, Game game)
        {
            MapSprite ret = new MapSprite();
            JArray tArr = room["Tiles"] as JArray;
            Texture texture = game.TextureFromName((string)room["TileTexture"]);
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
                        int layer = (int)tArr[i++];
                        Tile layeredTile = new Tile(x, y, texture, (int)tArr[i++], (int)tArr[i++]);
                        layeredTile.Layer = layer;
                        layeredTile.Tag = (string)tArr[i++];
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
                        tile.Tag = (string)tArr[i++];
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
            ret.Color = Color.FromArgb(clr);
            return ret;
        }

        public new void SetSize(float width, float height)
        {
            this.width = width;
            this.height = height;
            base.SetSize(width / 40, width / 40);
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

            GL.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, instances);
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
    }
}
