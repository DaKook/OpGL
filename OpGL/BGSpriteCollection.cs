using Newtonsoft.Json.Linq;
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
        private TextureProgram program;
        public PointF MovementSpeed;
        private bool wrap = true;
        private bool random = false;
        private static Random r = new Random();
        public Color BaseColor;
        public Color BackgroundColor;
        public bool InheritRoomColor;
        public AnimatedColor ColorModifier;
        public string Name;
        public int Width;
        public int Height;
        private Game owner;
        //Animation[] randomOptions;
        private JArray save;
        public JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Name", Name);
            ret.Add("Texture", Texture.Name);
            ret.Add("Color", BaseColor.ToArgb());
            ret.Add("BackgroundColor", BackgroundColor.ToArgb());
            ret.Add("InheritColor", InheritRoomColor);
            ret.Add("XSpeed", MovementSpeed.X);
            ret.Add("YSpeed", MovementSpeed.Y);
            ret.Add("Objects", save.DeepClone());
            return ret;
        }

        public Texture Texture { get; private set; }

        public uint VAO { get; private set; }

        public BGSpriteCollection(Texture texture, Game game)
        {
            Color = Color.White;
            BaseColor = Color.White;
            BackgroundColor = Color.Black;
            Texture = texture;
            program = texture.Program;
            buffer = new float[] { };
            texMatrix = Matrix4x4f.Scaled(texture.TileSizeX / texture.Width, texture.TileSizeY / texture.Height, 1f);
            save = new JArray();
            owner = game;
            Width = Game.RESOLUTION_WIDTH;
            Height = Game.RESOLUTION_HEIGHT;
        }
        public void Fill(Animation animation)
        {
            Clear();
            save.Clear();
            JObject add = new JObject();
            add.Add("Type", "Fill");
            add.Add("Animation", animation.Name);
            save.Add(add);
            wrap = !(random = false);
            for (int i = 0; i < Game.RESOLUTION_WIDTH + animation.Hitbox.Width; i += animation.Hitbox.Width)
            {
                for (int j = 0; j < Game.RESOLUTION_HEIGHT + animation.Hitbox.Height; j += animation.Hitbox.Height)
                {
                    Sprite s = new Sprite(i, j, Texture, animation);
                    s.Layer = -2;
                    Add(s);
                    Height = j;
                }
                Width = i;
            }
        }
        public void Distribute(Animation animation, PointF start, PointF stride, Point amount)
        {
            JObject add = new JObject();
            add.Add("Type", "Distribute");
            add.Add("Animation", animation.Name);
            add.Add("StartX", start.X);
            add.Add("StartY", start.Y);
            add.Add("StrideX", stride.X);
            add.Add("StrideY", stride.Y);
            add.Add("AmountX", amount.X);
            add.Add("AmountY", amount.Y);
            save.Add(add);
            for (int y = 0; y < amount.X; y++)
            {
                for (int x = 0; x < amount.X; x++)
                {
                    Sprite s = new Sprite((x * stride.X) + start.X, (y * stride.Y) + start.Y, Texture, animation);
                    s.Layer = -1;
                    Add(s);
                }
            }
        }
        public void Populate(int count, Animation[] options, float layer, PointF speed, bool evenX)
        {
            if (wrap)
            {
                Clear();
                save.Clear();
                wrap = !(random = true);
            }
            JObject add = new JObject();
            add.Add("Type", "Populate");
            add.Add("Count", count);
            add.Add("Layer", layer);
            add.Add("XSpeed", speed.X);
            add.Add("YSpeed", speed.Y);
            add.Add("Even", evenX);
            JArray anims = new JArray();
            foreach (Animation animation in options)
            {
                anims.Add(animation.Name);
            }
            add.Add("Options", anims);
            save.Add(add);
            MovementSpeed = new PointF(0, 0);
            float s = (float)(Game.RESOLUTION_WIDTH + 2 * Texture.TileSizeX) / count;
            for (int j = 0; j < count; j++)
            {
                Animation a = options[r.Next(options.Length)];
                float x = j * s - Texture.TileSizeX;
                float y = r.Next(-Texture.TileSizeY, Game.RESOLUTION_HEIGHT);
                Add(new BackgroundSprite(x, y, Texture, a) { MovementSpeed = new PointF(speed.X * layer, speed.Y * layer) });
            }
        }
        public void Scatter(int count, Animation animation, float speed)
        {
            wrap = true;
            random = false;
            JObject add = new JObject();
            add.Add("Type", "Scatter");
            add.Add("Count", count);
            add.Add("Animation", animation.Name);
            add.Add("Speed", speed);
            save.Add(add);
            for (int i = 0; i < count; i++)
            {
                BackgroundSprite s = new BackgroundSprite(r.Next(-Texture.TileSizeX, Game.RESOLUTION_WIDTH), r.Next(-Texture.TileSizeY, Game.RESOLUTION_HEIGHT), Texture, animation);
                double angle = r.NextDouble() * 2 * Math.PI;
                float x = speed * (float)Math.Cos(angle);
                float y = speed * (float)Math.Sin(angle);
                s.MovementSpeed = new PointF(x, y);
                Add(s);
            }
        }

        public new void Clear()
        {
            base.Clear();
            save.Clear();
            buffer = new float[] { };
            Width = Game.RESOLUTION_WIDTH;
            Height = Game.RESOLUTION_HEIGHT;
        }

        public void RenderPrep(int viewLoc, Matrix4x4f baseCamera)
        {
            matrix = baseCamera;
            Gl.UniformMatrix4f(viewLoc, 1, false, matrix);
            Gl.Uniform1(program.IsTextureLocation, 1);
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
                    if (this[i].X > Location.X + Width) this[i].X -= Width + this[i].Width;
                    else if (this[i].Right < Location.X) this[i].X += Width + this[i].Width;
                    if (this[i].Y > Location.Y + Height) this[i].Y -= Height + this[i].Height;
                    else if (this[i].Bottom < Location.Y) this[i].Y += Height + this[i].Height;
                }
                else if (random)
                {
                    if (this[i].X > Location.X + Width)
                    {
                        this[i].X -= Width + Texture.TileSizeX;
                        this[i].Y = r.Next(-Texture.TileSizeY, Height);
                    }
                    else if (this[i].Right < Location.X)
                    {
                        this[i].X += Width + Texture.TileSizeY;
                        this[i].Y = r.Next(-Texture.TileSizeY, Height);
                    }
                    if (this[i].Y > Location.Y + Height)
                    {
                        this[i].Y -= Height + Texture.TileSizeX;
                        this[i].X = r.Next(-Texture.TileSizeX, Width);
                    }
                    else if (this[i].Bottom < Location.Y)
                    {
                        this[i].Y += Height + Texture.TileSizeY;
                        this[i].X = r.Next(-Texture.TileSizeX, Width);
                    }
                }
                buffer[i * 4] = this[i].X;
                buffer[i * 4 + 1] = this[i].Y;
                buffer[i * 4 + 2] = this[i].TextureX;
                buffer[i * 4 + 3] = this[i].TextureY;
            }
        }

        public override void Render(int frame, bool showInvisible = false)
        {
            if (!Visible) return;
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(VAO);
            Gl.UseProgram(program.ID);
            UpdateBuffer();
            Gl.UniformMatrix4f(program.ModelLocation, 1, false, Matrix4x4f.Identity);
            Gl.UniformMatrix4f(program.TexLocation, 1, false, texMatrix);
            Gl.Uniform4f(program.MasterColorLocation, 1, new Vertex4f(Color.R / 255f, Color.G / 255f, Color.B / 255f, Color.A / 255f));
            float r = (float)BaseColor.R / 255;
            float g = (float)BaseColor.G / 255;
            float b = (float)BaseColor.B / 255;
            float a = (float)BaseColor.A / 255;
            if (ColorModifier is object)
            {
                Color c = ColorModifier.GetFrame(frame);
                r *= (float)c.R / 255;
                g *= (float)c.G / 255;
                b *= (float)c.B / 255;
                a *= (float)c.A / 255;
            }
            if (InheritRoomColor && owner.CurrentRoom is object)
            {
                float r2 = (float)owner.CurrentRoom.Color.R / 255;
                float g2 = (float)owner.CurrentRoom.Color.G / 255;
                float b2 = (float)owner.CurrentRoom.Color.B / 255;
                r *= r2;
                g *= g2;
                b *= b2;
            }
            Gl.Uniform4f(Texture.Program.ColorLocation, 1, new Vertex4f(r, g, b, a));
            Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, buffer.Length / 4);
        }

        public void UpdateBuffer()
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

        public void Load(JToken loadFrom, Game game)
        {
            string name = (string)loadFrom["Name"] ?? "";
            if (name == Name) return;
            Name = name;
            Texture = game.TextureFromName((string)loadFrom["Texture"] ?? "");
            program = Texture.Program;
            Clear();
            texMatrix = Matrix4x4f.Scaled(Texture.TileSizeX / Texture.Width, Texture.TileSizeY / Texture.Height, 1f);
            save = new JArray();
            float mx = (float)(loadFrom["XSpeed"] ?? 1);
            float my = (float)(loadFrom["YSpeed"] ?? 0);
            MovementSpeed = new PointF(mx, my);
            BaseColor = Color.FromArgb((int)(loadFrom["Color"] ?? -1));
            InheritRoomColor = (bool)(loadFrom["InheritColor"] ?? false);
            JArray objs = (JArray)loadFrom["Objects"];
            for (int i = 0; i < objs.Count; i++)
            {
                switch ((string)objs[i]["Type"])
                {
                    case "Fill":
                        {
                            Animation animation = Texture.AnimationFromName((string)objs[i]["Animation"] ?? "");
                            if (animation is object)
                                Fill(animation);
                        }
                        break;
                    case "Distribute":
                        {
                            Animation animation = Texture.AnimationFromName((string)objs[i]["Animation"]);
                            if (animation is object)
                            {
                                float x = (float)(objs[i]["StartX"] ?? 0f);
                                float y = (float)(objs[i]["StartY"] ?? 0f);
                                float stx = (float)(objs[i]["StrideX"] ?? 0f);
                                float sty = (float)(objs[i]["StrideY"] ?? 0f);
                                int amx = (int)(objs[i]["AmountX"] ?? 0);
                                int amy = (int)(objs[i]["AmountY"] ?? 0);
                                Distribute(animation, new PointF(x, y), new PointF(stx, sty), new Point(amx, amy));
                            }
                        }
                        break;
                    case "Populate":
                        {
                            int count = (int)(objs[i]["Count"] ?? 1);
                            JArray anims = (JArray)objs[i]["Options"];
                            if (anims is object)
                            {
                                Animation[] options = new Animation[anims.Count];
                                for (int j = 0; j < anims.Count; j++)
                                {
                                    options[j] = Texture.AnimationFromName((string)anims[j] ?? "");
                                }
                                float layer = (float)(objs[i]["Layer"] ?? 1);
                                float xSpeed = (float)(objs[i]["XSpeed"] ?? 1);
                                float ySpeed = (float)(objs[i]["YSpeed"] ?? 0);
                                bool even = (bool)(objs[i]["Even"] ?? true);
                                Populate(count, options, layer, new PointF(xSpeed, ySpeed), even);
                            }
                        }
                        break;
                    case "Scatter":
                        {
                            int count = (int)(objs[i]["Count"] ?? 1);
                            Animation animation = Texture.AnimationFromName((string)objs[i]["Animation"] ?? "");
                            float speed = (float)(objs[i]["Speed"] ?? 1);
                            Scatter(count, animation, speed);
                        }
                        break;
                }
            }
        }
    }
}
