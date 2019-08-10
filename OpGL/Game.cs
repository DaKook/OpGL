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
using System.Windows.Forms;

namespace OpGL
{
    public class Game
    {
        // Input
        public enum Inputs
        {
            Left,
            Right,
            Jump,
            Pause,
            Kill,
            Escape,
            Count
        }
        private int[] inputs = new int[(int)Inputs.Count];
        public Dictionary<Keys, Inputs> inputMap = new Dictionary<Keys, Inputs>() {
            { Keys.Left, Inputs.Left }, { Keys.A, Inputs.Left },
            { Keys.Right, Inputs.Right }, { Keys.D, Inputs.Right },
            { Keys.Up, Inputs.Jump }, { Keys.Down, Inputs.Jump }, { Keys.Space, Inputs.Jump }, { Keys.Z, Inputs.Jump }, { Keys.V, Inputs.Jump },
            { Keys.Enter, Inputs.Pause },
            { Keys.R, Inputs.Kill },
            { Keys.Escape, Inputs.Escape }
        };
        private SortedSet<Keys> heldKeys = new SortedSet<Keys>();

        bool holdingJump = false;
        private bool IsInputActive(Inputs input)
        {
            return inputs[(int)input] != 0;
        }

        // Scripts
        public Action WaitingForAction = null;
        public int DelayFrames;
        public bool PlayerControl = true;
        public bool Freeze = false;
        public Script CurrentScript;

        // OpenGL
        private GlControl glControl;
        private uint program;

        // Textures
        public Texture FontTexture;
        private List<Texture> textures;
        public Texture TextureFromName(string name)
        {
            foreach (Texture texture in textures)
            {
                if (texture.Name.ToLower() == name.ToLower()) return texture;
            }
            return null;
        }

#if TEST
        // Test
        const int avgOver = 60;
        float[] renderTimes = new float[avgOver];
        float rtTotal = 0f;
        int rtIndex = 0;
        float[] frameTimes = new float[avgOver];
        float ftTotal = 0f;
        private StringDrawable timerSprite;
#endif

        // Camera
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
        const int RESOLUTION_WIDTH = 320;
        const int RESOLUTION_HEIGHT = 240;

        // Drawables
        private DrawableCollection sprites;
        private DrawableCollection hudSprites;
        public List<Drawable> UserAccessDrawables = new List<Drawable>();
        public Drawable GetDrawableByName(string name, bool caseSensitive = false)
        {
            foreach (Drawable drawable in UserAccessDrawables)
            {
                if ((!caseSensitive && drawable.Name.ToLower() == name.ToLower()) || drawable.Name == name)
                    return drawable;
            }
            return null;
        }

        Player ActivePlayer;
        public bool IsPlaying { get; private set; } = false;
        private int FrameCount = 0;

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
            Texture sprites32 = TextureFromName("sprites32");
            FontTexture = TextureFromName("font");
            ActivePlayer = new Player(20, 20, viridian, "Player", viridian.Animations[0], viridian.Animations[1], viridian.Animations[2], viridian.Animations[3], viridian.Animations[4]);
            //ActivePlayer.CanFlip = false;
            //ActivePlayer.Jump = 8;
            sprites.Add(ActivePlayer);
            UserAccessDrawables.Add(ActivePlayer);
            ActivePlayer.TextBoxColor = Color.FromArgb(164, 164, 255);

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
            //sprites.Add(new Platform(8, 152, platforms, platforms.Animations[1], 0, 0, 1, false));
            //sprites.Add(new Platform(40, 152, platforms, platforms.Animations[2], 0, 0, -1, false));
            sprites.Add(new Platform(168, 80, platforms, platforms.Animations[1], 0f, 0f, 1, false));
            sprites.Add(new Platform(280, 184, platforms, platforms.Animations[0], 0.5f, 0, 0, true, platforms.Animations[3]));
            sprites.Add(new Platform(262, 216, platforms, platforms.Animations[0], -1f, 0, 0, false));
            sprites.Add(new Platform(262, 224, platforms, platforms.Animations[0], -1f, 0, 0, false));
            sprites.Add(new Platform(200, 216, platforms, platforms.Animations[0], -1f, 0, 0, false));
            sprites.Add(new Platform(200, 224, platforms, platforms.Animations[0], -1f, 0, 0, false));
            sprites.Add(new Tile(200, 80, tiles, 4, 5));
            sprites.Add(new Checkpoint(88, 144, sprites32, sprites32.Animations[0], sprites32.Animations[1]));
            sprites.Add(new Checkpoint(184, 216, sprites32, sprites32.Animations[0], sprites32.Animations[1], true));
            sprites.Add(new Checkpoint(184, 8, sprites32, sprites32.Animations[0], sprites32.Animations[1], false, true));
            sprites.Add(new Enemy(64, 80, sprites32, sprites32.Animations[2], 0, 1, Color.Red));
            sprites.Add(new Tile(304, 8, tiles, 9, 0));
            for (int i = 168; i < 241; i += 8)
                sprites.Add(new Tile(160, i, tiles, 5, 5));
            for (int i = 0; i < 160; i += 8)
                for (int j = 168; j < 241; j += 8)
                    sprites.Add(new Tile(i, j, tiles, 3, 2));
            for (int i = 168; i < 312; i += 8)
                for (int j = 184; j < 232; j += 8)
                    sprites.Add(new Tile(i, j, tiles, 1, 20));
            for (int i = 168; i < 312; i += 8)
                sprites.Add(new Tile(i, 176, tiles, 1, 19));
            hudSprites.Add(new StringDrawable(8, 8, FontTexture, "Welcome to VVVVVVV!" + Environment.NewLine + "You will enjoy...", Color.Red));
            hudSprites.Add(timerSprite = new StringDrawable(8, RESOLUTION_HEIGHT - 12, FontTexture, "TEST", Color.White));
            VTextBox vText = new VTextBox(40, 40, FontTexture, "Yey! I can talk now!", Color.FromArgb(0xa4, 0xa4, 0xff));
            hudSprites.Add(vText);
            Script testScript = ParseScript("playercontrol,false" + Environment.NewLine +
                "say,1,player" + Environment.NewLine +
                "This is Captain Viridian." + Environment.NewLine +
                "say,1,255,255,134" + Environment.NewLine +
                "What do you see?" + Environment.NewLine +
                "say,1,player" + Environment.NewLine +
                "It looks like a playground..." + Environment.NewLine +
                "say,1,255,255,134" + Environment.NewLine +
                "Be careful, Captain!" + Environment.NewLine +
                "changefont,evilfont" + Environment.NewLine +
                "say,2,180,0,0" + Environment.NewLine +
                "Hahaha, it is too late; you" + Environment.NewLine +
                "have already fallen into my trap!" + Environment.NewLine +
                "changefont,font" + Environment.NewLine +
                "delay,120" + Environment.NewLine +
                "say,1,player" + Environment.NewLine +
                "Professor, did you hear that?" + Environment.NewLine +
                "say,1,255,255,134" + Environment.NewLine +
                "Hear what?" + Environment.NewLine +
                "say,2,player" + Environment.NewLine +
                "Maybe it was nothing... I have" + Environment.NewLine +
                "a bad feeling about this..." + Environment.NewLine +
                "playercontrol,true");
            //WaitingForAction = () =>
            //{
            //    testScript.ExecuteFromBeginning();
            //    CurrentScript = testScript;
            //};
            

#endif
            glControl.Render += glControl_Render;
            glControl.Resize += glControl_Resize;
            glControl.KeyDown += GlControl_KeyDown;
            glControl.KeyUp += GlControl_KeyUp;
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
        #endregion

        private void glControl_Resize(object sender, EventArgs e)
        {
            float relX = (float)glControl.Width / RESOLUTION_WIDTH;
            float relY = (float)glControl.Height / RESOLUTION_HEIGHT;
            float scaleBy = Math.Min(relX, relY);
            int w = (int)(RESOLUTION_WIDTH * scaleBy);
            int h = (int)(RESOLUTION_HEIGHT * scaleBy);
            Gl.Viewport((glControl.Width - w) / 2, (glControl.Height - h) / 2, w, h);
        }

        private void GlControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (inputMap.ContainsKey(e.KeyCode) && !heldKeys.Contains(e.KeyCode))
            {
                inputs[(int)inputMap[e.KeyCode]]++;
                heldKeys.Add(e.KeyCode);
            }
        }
        private void GlControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (inputMap.ContainsKey(e.KeyCode))
            {
                inputs[(int)inputMap[e.KeyCode]]--;
                heldKeys.Remove(e.KeyCode);
            }
        }

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

                if (PlayerControl)
                {
                    if (IsInputActive(Inputs.Right))
                        ActivePlayer.InputDirection = 1;
                    else if (IsInputActive(Inputs.Left))
                        ActivePlayer.InputDirection = -1;
                    else
                        ActivePlayer.InputDirection = 0;

                    if (IsInputActive(Inputs.Kill))
                    {
                        ActivePlayer.KillSelf();
                    }
                }

                if (IsInputActive(Inputs.Jump))
                {
                    if (!holdingJump)
                    {
                        if (WaitingForAction != null)
                        {
                            Action exec = WaitingForAction;
                            WaitingForAction = null;
                            exec();
                        }
                        else if (PlayerControl)
                        {
                            ActivePlayer.FlipOrJump();
                        }
                        holdingJump = true;
                    }
                }
                else if (holdingJump)
                {
                    holdingJump = false;
                }

                if (!Freeze)
                {
                    for (int i = 0; i < sprites.Count; i++)
                    {
                        if (!sprites[i].Static)
                            sprites[i].Process();
                    }

                    sprites.SortForCollisions();
                    Drawable[] checkCollisions = sprites.Where((d) => !d.Static && !d.Immovable).ToArray();
                    PointF[] endLocation = new PointF[checkCollisions.Length];
                    for (int i = 0; i < checkCollisions.Length; i++)
                    {
                        Drawable drawable = checkCollisions[i];
                        PerformCollisionChecks(drawable);
                        endLocation[i] = new PointF(drawable.X, drawable.Y);
                    }
                    // check again any that have moved since completing their collisions
                    bool collisionPerformed;
                    do
                    {
                        collisionPerformed = false;
                        for (int i = 0; i < checkCollisions.Length; i++)
                        {
                            Drawable drawable = checkCollisions[i];
                            if (endLocation[i] != new PointF(drawable.X, drawable.Y))
                            {
                                collisionPerformed = true;
                                PerformCollisionChecks(drawable);
                                endLocation[i] = new PointF(drawable.X, drawable.Y);
                            }
                        }
                    } while (collisionPerformed);
                }

                if (DelayFrames > 0)
                {
                    DelayFrames -= 1;
                    if (DelayFrames == 0)
                        CurrentScript.Continue();
                }

                foreach (Drawable d in hudSprites)
                {
                    d.Process();
                }

                FrameCount++;

                glControl.Invalidate();

#if TEST
                float ms = (float)(stp.ElapsedTicks - fStart) / Stopwatch.Frequency * 1000f;
                ftTotal += ms;
                ftTotal -= frameTimes[FrameCount % 60];
                frameTimes[FrameCount % 60] = ms;
#endif
            }
        }

        private void PerformCollisionChecks(Drawable drawable)
        {
            List<CollisionData> groundCollisions = new List<CollisionData>();
            List<CollisionData> entityCollisions = new List<CollisionData>();
            // previously collided but not yet bounced platforms
            CollisionData hPlatform = null;
            CollisionData vPlatform = null;
            while (true) // loop exits when there are no more collisions to handle
            {
                // get a collision
                List<Drawable> testFor = sprites.GetPotentialColliders(drawable);
                List<CollisionData> collisionDatas = new List<CollisionData>();
                foreach (Drawable d in testFor)
                {
                    CollisionData cd = drawable.TestCollision(d);
                    if (cd != null && !entityCollisions.Any((a) => a.CollidedWith == cd.CollidedWith))
                        collisionDatas.Add(cd);
                }
                CollisionData c = drawable.GetFirstCollision(collisionDatas);

                // exit condition: there is nothing to collide with
                if (c == null) break;

                // entity colliding with ground must be handled in a way that allows bouncing platforms
                if (drawable.Solid == Drawable.SolidState.Entity && c.CollidedWith.Solid == Drawable.SolidState.Ground)
                {
                    groundCollisions.Add(c);
                    CollisionData platformCollision = c.Vertical ? vPlatform : hPlatform;
                    if (c.CollidedWith is Platform)
                    {
                        // if a previous collision means it should be bounced
                        if (groundCollisions.Any((a) => a.CollidedWith.Solid == Drawable.SolidState.Ground && Math.Sign(c.Distance) != Math.Sign(a.Distance)))
                        {
                            c.CollidedWith.Collide(new CollisionData(c.Vertical, -c.Distance, drawable));
                            if (drawable is Platform) // undo duplicate direction flipping
                                c.CollidedWith.Collide(new CollisionData(c.Vertical, 0, drawable));
                        }
                        else
                        {
                            if (c.Vertical)
                                vPlatform = c;
                            else
                                hPlatform = c;
                        }
                    }
                    // collide here because (1) after distance is potentially set to 0 because platform has bounced away
                    // (2) collision should happen before getting distance for bouncing a previously-collided platform or that will be 0
                    drawable.Collide(c);
                    // entity has previously collided with a platform, and this collision means it should be bounced
                    if (platformCollision != null && Math.Sign(platformCollision.Distance) != Math.Sign(c.Distance))
                    {
                        CollisionData pcd = drawable.TestCollision(platformCollision.CollidedWith);
                        float dist = pcd == null ? 0 : -pcd.Distance;
                        // during this reverse collision, don't move the entity (it may on top of the platform)
                        drawable.Static = true;
                        platformCollision.CollidedWith.Collide(new CollisionData(c.Vertical, dist, drawable));
                        drawable.Static = false;
                        if (drawable is Platform) // undo duplicate direction flipping
                            c.CollidedWith.Collide(new CollisionData(c.Vertical, 0, drawable));

                        if (c.Vertical)
                            vPlatform = null;
                        else
                            hPlatform = null;
                    }
                }
                // otherwise, a simple collision should suffice
                else
                {
                    if (c.CollidedWith.Solid == Drawable.SolidState.Entity)
                    {
                        entityCollisions.Add(c);
                        if (drawable is Platform)
                            PerformCollisionChecks(c.CollidedWith);
                        else
                            drawable.Collide(c);
                    }
                    else // only Crewman can collide with entities, but that check is elsewhere
                        drawable.Collide(c);
                }
            }
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

        public Script ParseScript(string script)
        {
            string[] lines = script.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            List<Command> commands = new List<Command>();
            int i = 0;
            while (i < lines.Length)
            {
                string[] args = lines[i++].Split(new char[] { ',', '(', ')' });
                switch (args[0].ToLower())
                {
                    case "say":
                        {
                            if (!int.TryParse(args.ElementAtOrDefault(1), out int sayLines)) continue;
                            Color sayTextBoxColor = Color.Gray;
                            Crewman sayCrewman = GetDrawableByName(args.ElementAtOrDefault(2)) as Crewman;
                            if (sayCrewman != null)
                            {
                                sayTextBoxColor = sayCrewman.TextBoxColor;
                            }
                            else if (args.Length == 5)
                            {
                                int.TryParse(args[2], out int r);
                                int.TryParse(args[3], out int g);
                                int.TryParse(args[4], out int b);
                                sayTextBoxColor = Color.FromArgb(r, g, b);
                            }
                            string sayText = "";
                            if (sayLines > 0)
                            {
                                sayText = lines[i++];
                                for (int sayI = 1; sayI < sayLines; sayI++)
                                {
                                    sayText += Environment.NewLine + lines[i++];
                                }
                            }
                            commands.Add(new Command(() =>
                            {
                                VTextBox sayTextBox = new VTextBox(0, 0, FontTexture, sayText, sayTextBoxColor);
                                if (sayCrewman != null)
                                {
                                    sayTextBox.Bottom = sayCrewman.Y - 2;
                                    sayTextBox.X = sayCrewman.X - 16;
                                    if (sayTextBox.Right > RESOLUTION_WIDTH - 8) sayTextBox.Right = RESOLUTION_WIDTH - 8;
                                    if (sayTextBox.Bottom > RESOLUTION_HEIGHT - 8) sayTextBox.Bottom = RESOLUTION_HEIGHT - 8;
                                    if (sayTextBox.X < 8) sayTextBox.X = 8;
                                    if (sayTextBox.Y < 8) sayTextBox.Y = 8;
                                }
                                else
                                {
                                    sayTextBox.CenterX = RESOLUTION_WIDTH / 2;
                                    sayTextBox.CenterY = RESOLUTION_HEIGHT / 2;
                                }
                                hudSprites.Add(sayTextBox);
                                sayTextBox.Appear();
                                WaitingForAction = () =>
                                {
                                    sayTextBox.Disappear();
                                    sayTextBox.Disappeared += () => hudSprites.Remove(sayTextBox);
                                    CurrentScript.Continue();
                                };
                            }, true));
                        }
                        break;
                    case "changefont":
                        commands.Add(ChangeFontCommand(args));
                        break;
                    case "delay":
                        commands.Add(WaitCommand(args));
                        break;
                    case "playercontrol":
                        commands.Add(PlayerControlCommand(args));
                        break;
                    default:
                        break;
                }
            }
            return new Script(commands.ToArray());
        }

        public Command ChangeFontCommand(string[] args)
        {
            string fontTexture = args.ElementAtOrDefault(1);
            Texture newFont = TextureFromName(fontTexture);
            Action success = () => { };
            if (newFont != null && newFont.Width / newFont.TileSize == 16 && newFont.Height / newFont.TileSize == 16)
            {
                success = () => {
                    FontTexture = newFont;
                };
            }
            return new Command(success, false);
        }
        public Command WaitCommand(string[] args)
        {
            int.TryParse(args.ElementAtOrDefault(1), out int frames);
            return new Command(() =>
            {
                DelayFrames = frames;
            }, true);
        }
        public Command PlayerControlCommand(string[] args)
        {
            bool.TryParse(args.ElementAtOrDefault(1), out bool pc);
            return new Command(() =>
            {
                PlayerControl = pc;
            });
        }
    }
}
