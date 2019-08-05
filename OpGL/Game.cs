#define TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Diagnostics;

using OpenGL;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OpGL
{
    public class Game
    {
        public Keys[] Left = new Keys[] { Keys.Left, Keys.A };
        public Keys[] Right = new Keys[] { Keys.Right, Keys.D };
        public Keys[] Jump = new Keys[] { Keys.Z, Keys.V, Keys.Space, Keys.Up, Keys.Down };
        public Keys[] Pause = new Keys[] { Keys.Enter };
        public Keys[] Escape = new Keys[] { Keys.Escape };

        public bool KeyLeft { get; set; }
        public bool KeyRight { get; set; }
        public bool KeyJump { get; set; }
        public bool KeyPause { get; set; }
        public bool KeyEscape { get; set; }


        public Texture TextureFromName(string name)
        {
            foreach (Texture texture in textures)
            {
                if (texture.Name == name) return texture;
            }
            return null;
        }

#if TEST
        const int avgOver = 60;
        float[] renderTimes = new float[avgOver];
        float rtTotal = 0f;
        int rtIndex = 0;
        float[] frameTimes = new float[avgOver];
        float ftTotal = 0f;
        int ftIndex = 0;
#endif

        const int RESOLUTION_WIDTH = 320;
        const int RESOLUTION_HEIGHT = 240;

        Player ActivePlayer;

        private GlControl glControl;
        private uint program;

        private Matrix4x4f camera, hudView;
        private float _camX;
        private float _camY;
        private float cameraX
        {
            get => _camX;
            set
            {
                camera.Translate(value - _camX, 0, 0);
                _camX = value;
            }
        }
        private float cameraY
        {
            get => _camY;
            set
            {
                camera.Translate(0, value - _camY, 0);
                _camY = value;
            }
        }
        private List<Texture> textures;

        private DrawableCollection sprites;
        private DrawableCollection hudSprites;
#if TEST
        private StringDrawable timerSprite;
#endif
        public bool IsPlaying { get; private set; } = false;

        public Game(GlControl control)
        {
            glControl = control;

            InitGlProgram();
            InitOpenGLSettings();

            textures = new List<Texture>();
            LoadTextures();

            sprites = new DrawableCollection();
            hudSprites = new DrawableCollection();
#if TEST
            Texture viridian = TextureFromName("viridian");
            Texture tiles = TextureFromName("tiles");
            Texture platforms = TextureFromName("platforms");
            ActivePlayer = new Player(20, 20, viridian, "Viridian", viridian.Animations[0], viridian.Animations[1]);
            //ActivePlayer.CanFlip = false;
            //ActivePlayer.Jump = 8;
            sprites.Add(ActivePlayer);

            for (int i = 8; i < 160; i += 8)
                sprites.Add(new Tile(i, 160, tiles, 4, 4));
            for (int i = 8; i < 312; i += 8)
                sprites.Add(new Tile(i, 0, tiles, 4, 6));
            for (int i = 160; i < 312; i += 8)
                sprites.Add(new Tile(i, 232, tiles, 4, 4));
            for (int i = 8; i < 232; i += 8)
                sprites.Add(new Tile(312, i, tiles, 3, 5));
            for (int i = 8; i < 160; i += 8)
                sprites.Add(new Tile(0, i, tiles, 5, 5));
            sprites.Add(new Tile(0, 160, tiles, 4, 3));
            sprites.Add(new Tile(160, 152, tiles, 4, 5));
            sprites.Add(new Tile(0, 152, tiles, 5, 4));
            sprites.Add(new Tile(160, 160, tiles, 5, 4));
            sprites.Add(new Tile(0, 0, tiles, 4, 2));
            sprites.Add(new Tile(312, 0, tiles, 5, 2));
            sprites.Add(new Tile(312, 232, tiles, 5, 3));
            sprites.Add(new Platform(96, 64, platforms, platforms.Animations[0], 0, 1, 0, false));
            sprites.Add(new Platform(144, 80, platforms, platforms.Animations[0], -1, 0, 0, false));
            sprites.Add(new Platform(8, 152, platforms, platforms.Animations[1], 0, 0, 1, false));
            sprites.Add(new Platform(40, 152, platforms, platforms.Animations[2], 0, 0, -1, false));
            sprites.Add(new Tile(200, 80, tiles, 4, 5));
            for (int i = 168; i < 241; i += 8)
                sprites.Add(new Tile(160, i, tiles, 5, 5));
            for (int i = 0; i < 160; i += 8)
                for (int j = 168; j < 241; j += 8)
                    sprites.Add(new Tile(i, j, tiles, 3, 2));

            hudSprites.Add(new StringDrawable(8, 8, textures[Textures.FONT], "Welcome to VVVVVVV!" + Environment.NewLine + "You will enjoy...", Color.Red));
            hudSprites.Add(timerSprite = new StringDrawable(8, RESOLUTION_HEIGHT - 12, textures[Textures.FONT], "TEST", Color.White));
#endif
            glControl.Render += glControl_Render;
            glControl.Resize += glControl_Resize;
        }
//INITIALIZE

        #region "Init"
        private void InitGlProgram()
        {
            program = GLProgram.Load("shaders/v2dTexTransform.txt", "shaders/f2dTex.txt");

            Gl.UseProgram(program);
            int modelMatrixLoc = Gl.GetUniformLocation(program, "model");
            Gl.UniformMatrix4f(modelMatrixLoc, 1, false, Matrix4x4f.Identity);

            // origin at top-left
            camera = Matrix4x4f.Translated(-1f, 1f, 0f);
            camera.Scale(2f / RESOLUTION_WIDTH, -2f / RESOLUTION_HEIGHT, 1);
            hudView = camera;
            int viewMatrixLoc = Gl.GetUniformLocation(program, "view");
            Gl.UniformMatrix4f(viewMatrixLoc, 1, false, camera);
        }

        private void LoadTextures()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("textures/").ToList();
            files.Sort();
            foreach (string file in files)
            {
                if (file.EndsWith(".png"))
                {
                    string fName = file.Split('/').Last();
                    fName = fName.Substring(0, fName.Length - 4);

                    if (System.IO.File.Exists("textures/" + fName + "_data.txt"))
                    {
                        JObject jObject = JObject.Parse(System.IO.File.ReadAllText("textures/" + fName + "_data.txt"));
                        Texture tex = CreateTexture(fName, (int)jObject["GridSize"]);
                        textures.Add(tex);

                        // Animations
                        JArray arr = (JArray)jObject["Animations"];
                        if (arr != null)
                        {
                            List<Animation> anims = new List<Animation>();
                            foreach (JObject anim in arr)
                            {
                                JArray frms = (JArray)anim["Frames"];
                                List<Point> frames = new List<Point>(frms.Count);
                                int speed = (int)anim["Speed"];
                                // Animations are specified as X, Y tile coordinates.
                                // Or a single negative value indicating re-use previous
                                int i = 0;
                                Point f = new Point();
                                while (i < frms.Count)
                                {
                                    int x = (int)frms[i];
                                    if (x >= 0)
                                    {
                                        i++;
                                        f = new Point(x, (int)frms[i]);
                                    }
                                    for (int k = 0; k < ((int)frms[i] < 0 ? -(int)frms[i] : 1); k++)
                                        for (int j = 0; j < speed; j++)
                                            frames.Add(f);

                                    i++;
                                }
                                JArray hitbox = (JArray)anim["Hitbox"];
                                Rectangle r = hitbox.Count == 4 ? new Rectangle((int)hitbox[0], (int)hitbox[1], (int)hitbox[2], (int)hitbox[3]) : Rectangle.Empty;
                                anims.Add(new Animation(frames.ToArray(), r, tex));
                            }
                            tex.Animations = anims;
                        }

                        // Tiles
                        JArray tls = (JArray)jObject["Tiles"];
                        if (tls != null)
                        {
                            Drawable.SolidState[,] states = new Drawable.SolidState[(int)(tex.Width / tex.TileSize), (int)(tex.Height / tex.TileSize)];
                            int i = 0;
                            int x = 0;
                            int y = 0;
                            while (i < tls.Count)
                            {
                                int count = (int)tls[i];
                                for (int j = 0; j < count; j++)
                                {
                                    states[x, y] = (Drawable.SolidState)(int)tls[i + 1];
                                    x += 1;
                                    if (x >= states.GetLength(0))
                                    {
                                        x = 0;
                                        y += 1;
                                    }
                                }
                                i += 2;
                            }
                            tex.TileSolidStates = states;
                        }
                    }
                    else // no _data file, create with default grid size
                    {
                        textures.Add(CreateTexture(fName, 32));
                        textures.Last().Animations = new List<Animation>();
                    }
                }
            }
        }
        private Texture CreateTexture(string texture, int gridSize)
        {
            SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.Decode("textures/" + texture + ".png");

            uint texa = Gl.CreateVertexArray();
            Gl.BindVertexArray(texa);
            uint texb = Gl.CreateBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, texb);
            float[] fls = new float[]
            {
                0f,       0f,       0f, 0f,
                0f,       gridSize, 0f, 1f,
                gridSize, gridSize, 1f, 1f,
                gridSize, 0f,       1f, 0f
            };
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)fls.Length * sizeof(float), fls, BufferUsage.StaticDraw);

            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);

            uint tex = Gl.CreateTexture(TextureTarget.Texture2d);
            Gl.BindTexture(TextureTarget.Texture2d, tex);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmp.GetPixels());

            Gl.GenerateMipmap(TextureTarget.Texture2d);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 0f });

            // instancing
            uint ibo = Gl.CreateBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            float[] empty = new float[] { 0f, 0f, 0f, 0f };
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)empty.Length * sizeof(float), empty, BufferUsage.DynamicDraw);

            Gl.VertexAttribPointer(2, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
            Gl.VertexAttribPointer(3, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(2);
            Gl.EnableVertexAttribArray(3);
            Gl.VertexAttribDivisor(2, 1);
            Gl.VertexAttribDivisor(3, 1);

            return new Texture(tex, bmp.Width, bmp.Height, gridSize, texture, program, texa, texb);
        }

        private void InitOpenGLSettings()
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.OneMinusDstAlpha, BlendingFactor.One);

            glControl_Resize(null, null);

            Gl.ClearColor(0f, 0f, 0f, 1f);
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            float relX = (float)glControl.Width / RESOLUTION_WIDTH;
            float relY = (float)glControl.Height / RESOLUTION_HEIGHT;
            float scaleBy = Math.Min(relX, relY);
            int w = (int)(RESOLUTION_WIDTH * scaleBy);
            int h = (int)(RESOLUTION_HEIGHT * scaleBy);
            Gl.Viewport((glControl.Width - w) / 2, (glControl.Height - h) / 2, w, h);
        }
        #endregion

        private void GameLoop()
        {
            Stopwatch stp = new Stopwatch();
            long ticksPerFrame = Stopwatch.Frequency / 60;
            long nextFrame = ticksPerFrame;
            stp.Start();

            while (IsPlaying)
            {
                while (stp.ElapsedTicks < nextFrame)
                {
                    int msToSleep = (int)((float)(nextFrame - stp.ElapsedTicks) / Stopwatch.Frequency - 0.5f);
                    if (msToSleep > 0)
                        Thread.Sleep(msToSleep);
                }
                long ticksElapsed = stp.ElapsedTicks - nextFrame;
                int framesDropped = (int)(ticksElapsed / ticksPerFrame);
                nextFrame += ticksPerFrame * (framesDropped + 1);
#if TEST
                long fStart = stp.ElapsedTicks;
#endif

                if (KeyRight)
                    ActivePlayer.InputDirection = 1;
                else if (KeyLeft)
                    ActivePlayer.InputDirection = -1;
                else
                    ActivePlayer.InputDirection = 0;

                if (ActivePlayer.OnGround && KeyJump)
                {
                    ActivePlayer.FlipOrJump();
                }

                for (int i = 0; i < sprites.Count; i++)
                {
                    if (!sprites[i].Static)
                        sprites[i].Process();
                }

                sprites.SortForCollisions();
                //IEnumerable<Drawable> process = sprites.Where((d) => d.Solid < Drawable.SolidState.NonSolid && (d.AlwaysProcess || d.Within(cameraX, cameraY, RESOLUTION_WIDTH, RESOLUTION_HEIGHT)));
                IEnumerable<Drawable> process = sprites;
                foreach (Drawable drawable in process)
                {
                    if (!drawable.Static)
                    {
                        PerformCollisionChecks(drawable, sprites.GetPotentialColliders(drawable));
                    }
                }

                glControl.Invalidate();

#if TEST
                float ms = (float)(stp.ElapsedTicks - fStart) / Stopwatch.Frequency * 1000f;
                ftTotal += ms;
                ftTotal -= frameTimes[ftIndex];
                frameTimes[ftIndex] = ms;
                ftIndex = (ftIndex + 1) % 60;
#endif
            }
        }

        private void PerformCollisionChecks(Drawable drawable, IEnumerable<Drawable> testFor)
        {
            List<Drawable> collided = new List<Drawable>();
            List<Drawable> noDoubleCollide = new List<Drawable>();
            bool checkedAll;
            do
            {
                checkedAll = true;
                foreach (Drawable d in testFor)
                {
                    PointF oldPos = new PointF(drawable.X, drawable.Y);
                    if (drawable.TestCollision(d))
                    {
                        if (!collided.Contains(d))
                        {
                            collided.Add(d);
                            if (drawable.IsOverlapping(d))
                                noDoubleCollide.Add(d);
                        }
                        else
                        {
                            if (noDoubleCollide.Contains(d))
                                throw new Exception("You seem to be stuck in an infinite collision loop.");
                            noDoubleCollide.Add(d);
                            // platform bounce?
                            if (drawable.Solid == Drawable.SolidState.Entity && d is Platform)
                            {
                                // put entity back
                                drawable.X = oldPos.X; drawable.Y = oldPos.Y;
                                // enable platform bounce, test platform collision with entity without moving entity
                                drawable.Solid = Drawable.SolidState.Ground;
                                d.TestCollision(drawable);
                                drawable.Solid = Drawable.SolidState.Entity;
                                drawable.X = oldPos.X; drawable.Y = oldPos.Y;
                                // test collision with platform, for case where platform was not bounced
                                drawable.TestCollision(d);
                            }
                        }
                        // drawable moved; re-check collisions
                        testFor = sprites.GetPotentialColliders(drawable);
                        checkedAll = false;
                        break;
                    }
                }
            } while (!checkedAll);
        }

        public void StartGame()
        {
            if (!IsPlaying)
            {
                IsPlaying = true;
                Thread thread = new Thread(GameLoop);
                thread.Start();
            }
        }
        public void StopGame() { IsPlaying = false; }

        private void glControl_Render(object sender, GlControlEventArgs e)
        {
#if TEST
            Stopwatch t = new Stopwatch();
            t.Start();
#endif

            // clear the color buffer
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            int viewMatrixLoc = Gl.GetUniformLocation(program, "view");
            Gl.UniformMatrix4f(viewMatrixLoc, 1, false, camera);
            sprites.Render();

            Gl.UniformMatrix4f(viewMatrixLoc, 1, false, hudView);
            hudSprites.Render();

#if TEST
            float ms = (float)t.ElapsedTicks / Stopwatch.Frequency * 1000f;
            t.Stop();
            rtTotal += ms;
            rtTotal -= renderTimes[rtIndex];
            renderTimes[rtIndex] = ms;
            rtIndex = (rtIndex + 1) % 60;
            timerSprite.Text = "Avg time (render, frame): " + (rtTotal / 60).ToString("0.0") + ", " + (ftTotal / 60).ToString("0.0");
#endif
        }

    }
}
