using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System;
using System.Drawing;
using OpenTK.Mathematics;

namespace V7
{
    public class BGSpriteCollection : SpriteCollection
    {
        private Matrix4 matrix;
        public PointF Location;
        private float[] buffer;
        private Matrix4 texMatrix;
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
            ret.Add("Color", System.Drawing.Color.FromArgb(BaseColor.A, BaseColor.R, BaseColor.G, BaseColor.B).ToArgb());
            ret.Add("BackgroundColor", BackgroundColor.ToArgb());
            ret.Add("InheritColor", InheritRoomColor);
            ret.Add("XSpeed", MovementSpeed.X);
            ret.Add("YSpeed", MovementSpeed.Y);
            ret.Add("Objects", save.DeepClone());
            return ret;
        }

        public Texture Texture { get; private set; }

        public int VAO { get; private set; }

        public BGSpriteCollection(Texture texture, Game game)
        {
            Color = Color.White;
            BaseColor = Color.White;
            BackgroundColor = Color.Black;
            Texture = texture;
            program = texture.Program;
            buffer = new float[] { };
            texMatrix = Matrix4.CreateScale(texture.TileSizeX / texture.Width, texture.TileSizeY / texture.Height, 1f);
            save = new JArray();
            owner = game;
            Width = Game.RESOLUTION_WIDTH;
            Height = Game.RESOLUTION_HEIGHT;
        }
        private BGSpriteCollection(Game game)
        {
            Color = Color.White;
            BaseColor = Color.White;
            BackgroundColor = Color.Black;
            buffer = new float[] { };
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
                    BackgroundSprite s = new BackgroundSprite(i, j, Texture, animation);
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
                    BackgroundSprite s = new BackgroundSprite((x * stride.X) + start.X, (y * stride.Y) + start.Y, Texture, animation);
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
                Add(new BackgroundSprite(x, y, Texture, a) { MovementSpeed = new PointF(speed.X * layer, speed.Y * layer), AnimationFrame = r.Next(a.FrameCount * a.BaseSpeed), Layer = (int)(layer * 100) });
            }
        }
        public void Scatter(int count, Animation animation, float speed, int layer = 0)
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
                s.Layer = layer;
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

        public void RenderPrep(int viewLoc, Matrix4 baseCamera)
        {
            matrix = baseCamera;
            GL.Uniform4(program.ColorLocation, Color);
            GL.UniformMatrix4(viewLoc, false, ref matrix);
            GL.Uniform1(program.IsTextureLocation, 1);
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
                        (this[i] as BackgroundSprite).AnimationFrame = r.Next(this[i].Animation.FrameCount * this[i].Animation.BaseSpeed);
                        
                    }
                    else if (this[i].Right < Location.X)
                    {
                        this[i].X += Width + Texture.TileSizeY;
                        this[i].Y = r.Next(-Texture.TileSizeY, Height);
                        (this[i] as BackgroundSprite).AnimationFrame = r.Next(this[i].Animation.FrameCount * this[i].Animation.BaseSpeed);
                    }
                    if (this[i].Y > Location.Y + Height)
                    {
                        this[i].Y -= Height + Texture.TileSizeX;
                        this[i].X = r.Next(-Texture.TileSizeX, Width);
                        (this[i] as BackgroundSprite).AnimationFrame = r.Next(this[i].Animation.FrameCount * this[i].Animation.BaseSpeed);
                    }
                    else if (this[i].Bottom < Location.Y)
                    {
                        this[i].Y += Height + Texture.TileSizeY;
                        this[i].X = r.Next(-Texture.TileSizeX, Width);
                        (this[i] as BackgroundSprite).AnimationFrame = r.Next(this[i].Animation.FrameCount * this[i].Animation.BaseSpeed);
                    }
                }
                buffer[i * 4] = this[i].X;
                buffer[i * 4 + 1] = this[i].Y;
                buffer[i * 4 + 2] = this[i].TextureX;
                buffer[i * 4 + 3] = this[i].TextureY;
            }
        }

        public override void Render(int frame, bool showInvisible = false, float progress = 1)
        {
            if (!Visible) return;
            GL.UseProgram(program.ID);
            GL.BindTexture(TextureTarget.Texture2D, Texture.ID);
            GL.BindVertexArray(VAO);
            UpdateBuffer();
            Matrix4 identity = Matrix4.Identity;
            GL.UniformMatrix4(program.ModelLocation, false, ref identity);
            GL.UniformMatrix4(program.TexLocation, false, ref texMatrix);
            GL.Uniform4(program.MasterColorLocation, new Vector4(Color.R / 255f, Color.G / 255f, Color.B / 255f, Color.A / 255f));
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
            GL.Uniform4(Texture.Program.ColorLocation, new Vector4(r, g, b, a));
            GL.DrawArraysInstanced(PrimitiveType.TriangleFan, 0, 4, buffer.Length / 4);
        }

        public void UpdateBuffer()
        {
            if (firstRender)
            {
                firstRender = false;

                int vao = 0;
                GL.CreateVertexArrays(1, out vao);
                VAO = vao;
                GL.BindVertexArray(VAO);

                GL.BindBuffer(BufferTarget.ArrayBuffer, Texture.baseVBO);
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
            GL.BufferData(BufferTarget.ArrayBuffer, buffer.Length * sizeof(float), buffer, BufferUsageHint.DynamicDraw);
        }

        public void Add(BackgroundSprite sprite)
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

        public new void Add(Sprite sprite)
        {
            if (sprite is BackgroundSprite)
                Add(sprite as BackgroundSprite);
        }

        public void Load(JToken loadFrom, Game game)
        {
            string name = (string)loadFrom["Name"] ?? "";
            if (name == Name) return;
            Name = name;
            Texture = game.TextureFromName((string)loadFrom["Texture"] ?? "");
            program = Texture.Program;
            Clear();
            texMatrix = Matrix4.CreateScale(Texture.TileSizeX / Texture.Width, Texture.TileSizeY / Texture.Height, 1f);
            save = new JArray();
            float mx = (float)(loadFrom["XSpeed"] ?? 1);
            float my = (float)(loadFrom["YSpeed"] ?? 0);
            MovementSpeed = new PointF(mx, my);
            System.Drawing.Color c = System.Drawing.Color.FromArgb((int)(loadFrom["Color"] ?? -1));
            BaseColor = Color.FromArgb(c.A, c.R, c.G, c.B);
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

        public static BGSpriteCollection LoadFrom(JToken loadFrom, Game game)
        {
            BGSpriteCollection ret = new BGSpriteCollection(game);
            ret.Load(loadFrom, game);
            return ret;
        }
    }
}
