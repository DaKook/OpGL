using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class BGSpriteCollection : SpriteCollection
    {
        private Matrix4x4f matrix;
        public PointF Location;
        private float[] buffer;
        private Matrix4x4f texMatrix;
        private bool firstRender = true;
        private uint ibo;
        private ProgramData program;
        public PointF MovementSpeed;
        private bool wrap = true;
        private bool random = false;
        private static Random r = new Random();
        //Animation[] randomOptions;

        public Texture Texture { get; private set; }

        public uint VAO { get; private set; }

        public BGSpriteCollection(Texture texture)
        {
            Color = Color.White;
            Texture = texture;
            program = texture.Program;
            buffer = new float[] { };
            texMatrix = Matrix4x4f.Scaled(texture.TileSizeX / texture.Width, texture.TileSizeY / texture.Height, 1f);
        }

        public void Fill(int textureX, int textureY)
        {
            Fill(new Animation(new Point[] { new Point(textureX, textureY) }, new Rectangle(0, 0, Texture.TileSizeX, Texture.TileSizeY), Texture));
        }
        public void Fill(Animation animation)
        {
            Clear();
            for (int i = 0; i < Game.RESOLUTION_WIDTH + Texture.TileSizeX; i += Texture.TileSizeX)
            {
                for (int j = 0; j < Game.RESOLUTION_HEIGHT + Texture.TileSizeY; j += Texture.TileSizeY)
                {
                    Add(new Sprite(i, j, Texture, animation));
                }
            }
        }
        public void Populate(int count, Animation[] options, float layer, PointF speed)
        {
            if (wrap)
            {
                Clear();
                wrap = !(random = true);
            }
            MovementSpeed = new PointF(0, 0);
            //randomOptions = options;
            float s = (float)(Game.RESOLUTION_WIDTH + 2 * Texture.TileSizeX) / count;
            for (int j = 0; j < count; j++)
            {
                Animation a = options[r.Next(options.Length)];
                float x = j * s - Texture.TileSizeX;
                float y = r.Next(-Texture.TileSizeY, Game.RESOLUTION_HEIGHT);
                Add(new BackgroundSprite(x, y, Texture, a) { MovementSpeed = new PointF(speed.X * layer, speed.Y * layer) });
            }
        }

        public new void Clear()
        {
            base.Clear();
            buffer = new float[] { };
        }

        public void RenderPrep(int viewLoc, Matrix4x4f baseCamera)
        {
            matrix = baseCamera;
            Gl.UniformMatrix4f(viewLoc, 1, false, matrix);
        }

        public void Process()
        {
            for (int i = 0; i < Count; i++)
            {
                this[i].Process();
                this[i].X += MovementSpeed.X;
                this[i].Y += MovementSpeed.Y;
                if (wrap)
                {
                    if (this[i].X > Location.X + Game.RESOLUTION_WIDTH) this[i].X -= Game.RESOLUTION_WIDTH + Texture.TileSizeX;
                    else if (this[i].Right < Location.X) this[i].X += Game.RESOLUTION_WIDTH + Texture.TileSizeY;
                    if (this[i].Y > Location.Y + Game.RESOLUTION_HEIGHT) this[i].Y -= Game.RESOLUTION_HEIGHT + Texture.TileSizeX;
                    else if (this[i].Bottom < Location.Y) this[i].Y += Game.RESOLUTION_HEIGHT + Texture.TileSizeY;
                }
                else if (random)
                {
                    if (this[i].X > Location.X + Game.RESOLUTION_WIDTH)
                    {
                        this[i].X -= Game.RESOLUTION_WIDTH + Texture.TileSizeX;
                        this[i].Y = r.Next(-Texture.TileSizeY, Game.RESOLUTION_HEIGHT);
                    }
                    else if (this[i].Right < Location.X)
                    {
                        this[i].X += Game.RESOLUTION_WIDTH + Texture.TileSizeY;
                        this[i].Y = r.Next(-Texture.TileSizeY, Game.RESOLUTION_HEIGHT);
                    }
                    if (this[i].Y > Location.Y + Game.RESOLUTION_HEIGHT)
                    {
                        this[i].Y -= Game.RESOLUTION_HEIGHT + Texture.TileSizeX;
                        this[i].X = r.Next(-Texture.TileSizeX, Game.RESOLUTION_WIDTH);
                    }
                    else if (this[i].Bottom < Location.Y)
                    {
                        this[i].Y += Game.RESOLUTION_HEIGHT + Texture.TileSizeY;
                        this[i].X = r.Next(-Texture.TileSizeX, Game.RESOLUTION_WIDTH);
                    }
                }
                buffer[i * 4] = this[i].X;
                buffer[i * 4 + 1] = this[i].Y;
                buffer[i * 4 + 2] = this[i].TextureX;
                buffer[i * 4 + 3] = this[i].TextureY;
            }
        }

        public override void Render()
        {
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(Texture.baseVAO);
            Gl.UseProgram(program.ID);
            UpdateBuffer();
            Gl.UniformMatrix4f(program.ModelLocation, 1, false, Matrix4x4f.Identity);
            Gl.UniformMatrix4f(program.TexLocation, 1, false, texMatrix);
            Gl.Uniform4f(program.MasterColorLocation, 1, new Vertex4f(Color.R / 255f, Color.G / 255f, Color.B / 255f, Color.A / 255f));
            Gl.Uniform4f(program.ColorLocation, 1, new Vertex4f(1, 1, 1, 1));
            Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, buffer.Length / 4);
        }

        public void UpdateBuffer()
        {
            if (firstRender)
            {
                firstRender = false;

                VAO = Texture.baseVAO;
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
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)buffer.Length * sizeof(float), buffer, BufferUsage.DynamicDraw);
        }

        public new void Add(Sprite sprite)
        {
            if (sprite.Texture != Texture) return;
            base.Add(sprite);
            int i = buffer.Length;
            Array.Resize(ref buffer, buffer.Length + 4);
            buffer[i++] = sprite.X;
            buffer[i++] = sprite.Y;
            buffer[i++] = sprite.TextureX;
            buffer[i++] = sprite.TextureY;
        }
    }
}
