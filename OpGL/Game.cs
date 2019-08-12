﻿#define TEST

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
            Count
        }
        private int[] inputs = new int[(int)Inputs.Count];
        private int[] lastPressed = new int[(int)Inputs.Count];
        public Dictionary<Keys, Inputs> inputMap = new Dictionary<Keys, Inputs>() {
            { Keys.Left, Inputs.Left }, { Keys.A, Inputs.Left },
            { Keys.Right, Inputs.Right }, { Keys.D, Inputs.Right },
            { Keys.Up, Inputs.Jump }, { Keys.Down, Inputs.Jump }, { Keys.Space, Inputs.Jump }, { Keys.Z, Inputs.Jump }, { Keys.V, Inputs.Jump },
            { Keys.Enter, Inputs.Pause },
            { Keys.R, Inputs.Kill }
        };
        private SortedSet<Keys> heldKeys = new SortedSet<Keys>();

        private bool IsInputActive(Inputs input)
        {
            return inputs[(int)input] != 0;
        }
        private bool IsInputNew(Inputs input)
        {
            return lastPressed[(int)input] == FrameCount;
        }

        // Scripts
        public SortedList<string, Script> Scripts = new SortedList<string, Script>();
        public Script ScriptFromName(string name)
        {
            Scripts.TryGetValue(name, out Script script);
            return script;
        }

        public Action WaitingForAction = null;
        public int DelayFrames;
        public bool PlayerControl = true;
        public bool Freeze = false;
        public Script CurrentScript;
        private string[] presetcolors = new string[] { "cyan", "red", "yellow", "green", "purple", "blue", "gray", "terminal" };
        public List<VTextBox> TextBoxes = new List<VTextBox>();

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
        private SpriteCollection sprites;
        private SpriteCollection hudSprites;
        public SortedList<string, Sprite> UserAccessDrawables = new SortedList<string, Sprite>();
        public Sprite SpriteFromName(string name)
        {
            UserAccessDrawables.TryGetValue(name, out Sprite sprite);
            return sprite;
        }

        Player ActivePlayer;
        public bool IsPlaying { get; private set; } = false;
        private int FrameCount = 1; // start at 1 so inputs aren't "new" at start

        public Game(GlControl control)
        {
            glControl = control;

            InitGlProgram();
            InitOpenGLSettings();

            textures = new List<Texture>();
            LoadTextures();

            sprites = new SpriteCollection();
            hudSprites = new SpriteCollection();
#if TEST
            Texture viridian = TextureFromName("viridian");
            Texture tiles = TextureFromName("tiles");
            Texture platforms = TextureFromName("platforms");
            Texture sprites32 = TextureFromName("sprites32");
            FontTexture = TextureFromName("font");
            ActivePlayer = new Player(20, 20, viridian, "Viridian", viridian.Animations["Standing"], viridian.Animations["Walking"], viridian.Animations["Falling"], viridian.Animations["Jumping"], viridian.Animations["Dying"]);
            //ActivePlayer.CanFlip = false;
            //ActivePlayer.Jump = 8;
            sprites.Add(ActivePlayer);
            UserAccessDrawables.Add(ActivePlayer.Name, ActivePlayer);
            ActivePlayer.TextBoxColor = Color.FromArgb(164, 164, 255);

            //This will probably be moved somewhere else and might be customizeable per-level
            Terminal.TextBox = new VTextBox(0, 0, FontTexture, " Press ENTER to activate terminal ", Color.FromArgb(255, 130, 20));
            Terminal.TextBox.CenterX = RESOLUTION_WIDTH / 2;
            Terminal.TextBox.Y = 4;
            hudSprites.Add(Terminal.TextBox);

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
            sprites.Add(new Platform(96, 64, platforms, platforms.Animations["platform1"], 0, 1, 0, false));
            sprites.Add(new Platform(144, 80, platforms, platforms.Animations["platform1"], -1, 0, 0, false));
            //sprites.Add(new Platform(8, 152, platforms, platforms.Animations[1], 0, 0, 1, false));
            //sprites.Add(new Platform(40, 152, platforms, platforms.Animations[2], 0, 0, -1, false));
            sprites.Add(new Platform(168, 80, platforms, platforms.Animations["conveyor1r"], 0f, 0f, 1, false));
            sprites.Add(new Platform(280, 184, platforms, platforms.Animations["platform1"], 0.5f, 0, 0, true, platforms.Animations["disappear"]));
            sprites.Add(new Platform(262, 216, platforms, platforms.Animations["platform1"], -1f, 0, 0, true, platforms.Animations["disappear"]));
            sprites.Add(new Platform(262, 224, platforms, platforms.Animations["platform1"], -1f, 0, 0, true, platforms.Animations["disappear"]));
            sprites.Add(new Platform(200, 216, platforms, platforms.Animations["platform1"], -1f, 0, 0, true, platforms.Animations["disappear"]));
            sprites.Add(new Platform(200, 224, platforms, platforms.Animations["platform1"], -1f, 0, 0, true, platforms.Animations["disappear"]));
            sprites.Add(new Tile(200, 80, tiles, 4, 5));
            sprites.Add(new Checkpoint(88, 144, sprites32, sprites32.Animations["CheckOff"], sprites32.Animations["CheckOn"]));
            sprites.Add(new Checkpoint(184, 216, sprites32, sprites32.Animations["CheckOff"], sprites32.Animations["CheckOn"], true));
            sprites.Add(new Checkpoint(184, 8, sprites32, sprites32.Animations["CheckOff"], sprites32.Animations["CheckOn"], false, true));
            Enemy en = new Enemy(64, 41, sprites32, sprites32.Animations["Enemy1"], 0, 1, Color.Red);
            en.Bounds = new Rectangle(65, 41, 14, 79);
            sprites.Add(en);
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
            hudSprites.Add(new StringDrawable(8, 8, FontTexture, "Welcome to VVVVVVV!\nYou will enjoy...", Color.Red));
            hudSprites.Add(timerSprite = new StringDrawable(8, RESOLUTION_HEIGHT - 12, FontTexture, "TEST", Color.White));
            VTextBox vText = new VTextBox(40, 40, FontTexture, "Yey! I can talk now!", Color.FromArgb(0xa4, 0xa4, 0xff));
            hudSprites.Add(vText);
            Script testScript = ParseScript("playercontrol,false\n" +
                "say,2,gray\n" +
                "You have activated this terminal.\n" +
                "Congratulations! You have depression.\n" +
                "mood,player,sad\n" +
                "say,2,player\n" +
                "Oh no! Now I'm\n" +
                "depressed!\n" +
                "say,2,gray\n" +
                "Also, your checkpoint\n" +
                "has been set.\n" +
                "checkpoint\n" +
                "playercontrol,true");
            Terminal terminal = new Terminal(136, 144, sprites32, sprites32.Animations["TerminalOff"], sprites32.Animations["TerminalOn"], testScript, true);
            sprites.Add(terminal);


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
                        string test = (string)jObject["Trololol"];
                        textures.Add(tex);

                        // Animations
                        JArray arr = (JArray)jObject["Animations"];
                        if (arr != null)
                        {
                            SortedList<string,Animation> anims = new SortedList<string,Animation>();
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
                                Animation animation = new Animation(frames.ToArray(), r, tex);
                                animation.Name = (string)anim["Name"] ?? "";
                                anims.Add(animation.Name, animation);
                            }
                            tex.Animations = anims;
                        }

                        // Tiles
                        JArray tls = (JArray)jObject["Tiles"];
                        if (tls != null)
                        {
                            Sprite.SolidState[,] states = new Sprite.SolidState[(int)(tex.Width / tex.TileSize), (int)(tex.Height / tex.TileSize)];
                            int i = 0;
                            int x = 0;
                            int y = 0;
                            while (i < tls.Count)
                            {
                                int count = (int)tls[i];
                                for (int j = 0; j < count; j++)
                                {
                                    states[x, y] = (Sprite.SolidState)(int)tls[i + 1];
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
                        textures.Last().Animations = new SortedList<string,Animation>();
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
                lastPressed[(int)inputMap[e.KeyCode]] = FrameCount;
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
                // frame rate limiting
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

                // begin frame
                HandleUserInputs();

                if (!Freeze)
                    ProcessWorld();

                if (DelayFrames > 0)
                {
                    DelayFrames -= 1;
                    if (DelayFrames == 0)
                        CurrentScript.Continue();
                }

                for (int i = hudSprites.Count - 1; i >= 0; i--)
                {
                    Sprite d = hudSprites[i];
                    d.Process();
                }

                FrameCount %= int.MaxValue;
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

        private void HandleUserInputs()
        {
            if (PlayerControl)
            {
                if (IsInputActive(Inputs.Right))
                    ActivePlayer.InputDirection = 1;
                else if (IsInputActive(Inputs.Left))
                    ActivePlayer.InputDirection = -1;
                else
                    ActivePlayer.InputDirection = 0;

                if (IsInputActive(Inputs.Kill))
                    ActivePlayer.KillSelf();

                if (ActivePlayer.CurrentTerminal != null && !ActivePlayer.CurrentTerminal.AlreadyUsed && IsInputActive(Inputs.Pause))
                {
                    ActivePlayer.CurrentTerminal.AlreadyUsed = true;
                    Terminal t = ActivePlayer.CurrentTerminal;
                    ActivePlayer.CurrentTerminal = null;
                    Terminal.TextBox.Disappear();
                    CurrentScript = t.Script.ExecuteFromBeginning();
                    if (t.Repeat)
                        CurrentScript.Finished += (script) => {
                            t.AlreadyUsed = false;
                            if (t.IsOverlapping(ActivePlayer))
                            {
                                ActivePlayer.CurrentTerminal = t;
                                Terminal.TextBox.Appear();
                            }
                        };
                }
            }

            if (IsInputActive(Inputs.Jump))
            {
                if (IsInputNew(Inputs.Jump))
                {
                    if (WaitingForAction != null)
                    {
                        Action exec = WaitingForAction;
                        WaitingForAction = null;
                        exec();
                    }
                    else if (PlayerControl)
                        ActivePlayer.FlipOrJump();
                }
            }
        }
        private void ProcessWorld()
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                if (!sprites[i].Static)
                    sprites[i].Process();
            }

            sprites.SortForCollisions();
            Sprite[] checkCollisions = sprites.Where((d) => !d.Static && !d.Immovable).ToArray();
            PointF[] endLocation = new PointF[checkCollisions.Length];
            for (int i = 0; i < checkCollisions.Length; i++)
            {
                Sprite drawable = checkCollisions[i];
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
                    Sprite drawable = checkCollisions[i];
                    if (endLocation[i] != new PointF(drawable.X, drawable.Y))
                    {
                        collisionPerformed = true;
                        PerformCollisionChecks(drawable);
                        endLocation[i] = new PointF(drawable.X, drawable.Y);
                    }
                }
            } while (collisionPerformed);
            if (ActivePlayer.CurrentTerminal != null && !ActivePlayer.IsOverlapping(ActivePlayer.CurrentTerminal))
            {
                ActivePlayer.CurrentTerminal = null;
                Terminal.TextBox.Disappear();
            }
        }
        private void PerformCollisionChecks(Sprite drawable)
        {
            List<CollisionData> groundCollisions = new List<CollisionData>();
            List<CollisionData> entityCollisions = new List<CollisionData>();
            // previously collided but not yet bounced platforms
            CollisionData hPlatform = null;
            CollisionData vPlatform = null;
            while (true) // loop exits when there are no more collisions to handle
            {
                // get a collision
                List<Sprite> testFor = sprites.GetPotentialColliders(drawable);
                List<CollisionData> collisionDatas = new List<CollisionData>();
                foreach (Sprite d in testFor)
                {
                    CollisionData cd = drawable.TestCollision(d);
                    if (cd != null && !entityCollisions.Any((a) => a.CollidedWith == cd.CollidedWith))
                        collisionDatas.Add(cd);
                }
                CollisionData c = drawable.GetFirstCollision(collisionDatas);

                // exit condition: there is nothing to collide with
                if (c == null) break;

                // entity colliding with ground must be handled in a way that allows bouncing platforms
                if (drawable.Solid == Sprite.SolidState.Entity && c.CollidedWith.Solid == Sprite.SolidState.Ground)
                {
                    groundCollisions.Add(c);
                    CollisionData platformCollision = c.Vertical ? vPlatform : hPlatform;
                    if (c.CollidedWith is Platform)
                    {
                        // if a previous collision means it should be bounced
                        if (groundCollisions.Any((a) => a.CollidedWith.Solid == Sprite.SolidState.Ground && Math.Sign(c.Distance) != Math.Sign(a.Distance)))
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
                    if (c.CollidedWith.Solid == Sprite.SolidState.Entity)
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

        public Room Load(JObject loadFrom)
        {
            JArray sArr = loadFrom["Sprites"] as JArray;
            Room ret = new Room(new SpriteCollection(), null, null);
            foreach (JToken sprite in sArr)
            {
                string type = (string)sprite["Type"];
                //Type t = typeof(Sprite);
                Sprite s;
                float x = (float)sprite["X"];
                float y = (float)sprite["Y"];
                string textureName = (string)sprite["Texture"];
                Texture texture = TextureFromName(textureName);
                if (type == "Tile")
                {
                    int tileX = (int)sprite["TileX"];
                    int tileY = (int)sprite["TileY"];
                    s = new Tile((int)x, (int)y, texture, tileX, tileY);
                }
                else if (type == "Enemy")
                {
                    string animationName = (string)sprite["Animation"];
                    float xSpeed = (float)sprite["XSpeed"];
                    float ySpeed = (float)sprite["YSpeed"];
                    string name = (string)sprite["Name"];
                    int color = (int)sprite["Color"];
                    int boundX = (int)sprite["BoundsX"];
                    int boundY = (int)sprite["BoundsY"];
                    int boundW = (int)sprite["BoundsWidth"];
                    int boundH = (int)sprite["BoundsHeight"];
                    s = new Enemy(x, y, texture, texture.AnimationFromName(animationName), xSpeed, ySpeed, Color.FromArgb(color));
                    s.Name = name;
                    (s as Enemy).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
                }
                else if (type == "Crewman")
                {
                    string standName = (string)sprite["Standing"];
                    string walkName = (string)sprite["Walking"];
                    string fallName = (string)sprite["Falling"];
                    string jumpName = (string)sprite["Jumping"];
                    string dieName = (string)sprite["Dying"];
                    string name = (string)sprite["Name"];
                    int textBoxColor = (int)sprite["TextBox"];
                    bool sad = (bool)sprite["Sad"];
                    int aiState = (int)sprite["AI"];
                    string targetName = (string)sprite["Target"];
                    s = new Crewman(x, y, texture, name, texture.AnimationFromName(standName), texture.AnimationFromName(walkName), texture.AnimationFromName(fallName), texture.AnimationFromName(jumpName), texture.AnimationFromName(dieName), Color.FromArgb(textBoxColor));
                    (s as Crewman).Sad = sad;
                    (s as Crewman).AIState = (Crewman.AIStates)aiState;
                    (s as Crewman).Tag = targetName;
                }
                else if (type == "Checkpoint")
                {
                    string deactivatedName = (string)sprite["Deactivated"];
                    string activatedName = (string)sprite["Activated"];
                    bool flipX = (bool)sprite["FlipX"];
                    bool flipY = (bool)sprite["FlipY"];
                    s = new Checkpoint(x, y, texture, texture.AnimationFromName(deactivatedName), texture.AnimationFromName(activatedName), flipX, flipY);
                }
                else if (type == "Platform")
                {
                    string animationName = (string)sprite["Animation"];
                    string disappearName = (string)sprite["DisappearAnimation"];
                    float xSpeed = (float)sprite["XSpeed"];
                    float ySpeed = (float)sprite["YSpeed"];
                    float conveyor = (float)sprite["Conveyor"];
                    string name = (string)sprite["Name"];
                    bool disappear = (bool)sprite["Disappear"];
                    int color = (int)sprite["Color"];
                    int boundX = (int)sprite["BoundsX"];
                    int boundY = (int)sprite["BoundsY"];
                    int boundW = (int)sprite["BoundsWidth"];
                    int boundH = (int)sprite["BoundsHeight"];
                    s = new Platform(x, y, texture, texture.AnimationFromName(animationName), xSpeed, ySpeed, conveyor, disappear, texture.AnimationFromName(disappearName));
                    s.Name = name;
                    s.Color = Color.FromArgb(color);
                    (s as Platform).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
                }
                else if (type == "Terminal")
                {
                    string deactivatedName = (string)sprite["Deactivated"];
                    string activatedName = (string)sprite["Activated"];
                    string script = (string)sprite["Script"];
                    bool repeat = (bool)sprite["Repeat"];
                    bool flipX = (bool)sprite["FlipX"];
                    bool flipY = (bool)sprite["FlipY"];
                    s = new Terminal(x, y, texture, texture.AnimationFromName(deactivatedName), texture.AnimationFromName(activatedName), ScriptFromName(script), repeat);
                    s.FlipX = flipX;
                    s.FlipY = flipY;
                }

                else s = null;
                if (s != null)
                    ret.Objects.Add(s);
            }
            ret.EnterScript = ScriptFromName((string)loadFrom["EnterScript"]);
            ret.ExitScript = ScriptFromName((string)loadFrom["ExitScript"]);

            return ret;
        }

        public Script ParseScript(string script)
        {
            string[] lines = script.Replace(Environment.NewLine, "\n").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
                            Crewman sayCrewman = SpriteFromName(args.ElementAtOrDefault(2)) as Crewman;
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
                            else if (presetcolors.Contains(args.ElementAtOrDefault(2)))
                            {
                                switch (args[2])
                                {
                                    case "cyan":
                                        sayTextBoxColor = Color.FromArgb(164, 164, 255);
                                        break;
                                    case "red":
                                        sayTextBoxColor = Color.FromArgb(255, 60, 60);
                                        break;
                                    case "yellow":
                                        sayTextBoxColor = Color.FromArgb(255, 255, 134);
                                        break;
                                    case "green":
                                        sayTextBoxColor = Color.FromArgb(144, 255, 144);
                                        break;
                                    case "purple":
                                        sayTextBoxColor = Color.FromArgb(255, 134, 255);
                                        break;
                                    case "blue":
                                        sayTextBoxColor = Color.FromArgb(95, 95, 255);
                                        break;
                                    case "gray":
                                    case "terminal":
                                        sayTextBoxColor = Color.FromArgb(174, 174, 174);
                                        break;
                                }
                            }
                            string sayText = "";
                            if (sayLines > 0)
                            {
                                sayText = lines[i++];
                                for (int sayI = 1; sayI < sayLines; sayI++)
                                {
                                    sayText += "\n" + lines[i++];
                                }
                            }
                            commands.Add(new Command(() =>
                            {
                                if (sayCrewman == null && args.ElementAtOrDefault(2).ToLower() == "player")
                                {
                                    sayCrewman = ActivePlayer;
                                    sayTextBoxColor = ActivePlayer.TextBoxColor;
                                }
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
                                    sayTextBox.Disappeared += (textBox) => hudSprites.Remove(textBox);
                                    CurrentScript.Continue();
                                };
                            }, true));
                        }
                        break;
                    case "text":
                        {
                            if (!int.TryParse(args.LastOrDefault(), out int txLines)) continue;
                            Color txTextBoxColor = Color.Gray;
                            Crewman sayCrewman = SpriteFromName(args.ElementAtOrDefault(1)) as Crewman;
                            int txArgOffset = 0;
                            if (sayCrewman != null)
                            {
                                txTextBoxColor = sayCrewman.TextBoxColor;
                            }
                            else if (args.Length == 7)
                            {
                                txArgOffset = 2;
                                int.TryParse(args[2], out int r);
                                int.TryParse(args[3], out int g);
                                int.TryParse(args[4], out int b);
                                txTextBoxColor = Color.FromArgb(r, g, b);
                            }
                            else if (presetcolors.Contains(args.ElementAtOrDefault(2)))
                            {
                                switch (args[2])
                                {
                                    case "cyan":
                                        txTextBoxColor = Color.FromArgb(164, 164, 255);
                                        break;
                                    case "red":
                                        txTextBoxColor = Color.FromArgb(255, 60, 60);
                                        break;
                                    case "yellow":
                                        txTextBoxColor = Color.FromArgb(255, 255, 134);
                                        break;
                                    case "green":
                                        txTextBoxColor = Color.FromArgb(144, 255, 144);
                                        break;
                                    case "purple":
                                        txTextBoxColor = Color.FromArgb(255, 134, 255);
                                        break;
                                    case "blue":
                                        txTextBoxColor = Color.FromArgb(95, 95, 255);
                                        break;
                                    case "gray":
                                    case "terminal":
                                        txTextBoxColor = Color.FromArgb(174, 174, 174);
                                        break;
                                }
                            }
                            string txText = "";
                            if (txLines > 0)
                            {
                                txText = lines[i++];
                                for (int sayI = 1; sayI < txLines; sayI++)
                                {
                                    txText += "\n" + lines[i++];
                                }
                            }
                            int.TryParse(args.ElementAtOrDefault(2 + txArgOffset), out int txX);
                            int.TryParse(args.ElementAtOrDefault(3 + txArgOffset), out int txY);
                            commands.Add(new Command(() =>
                            {
                                TextBoxes.Add(new VTextBox(txX, txY, FontTexture, txText, txTextBoxColor));
                            }));
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
                    case "mood":
                        commands.Add(MoodCommand(args));
                        break;
                    case "checkpoint":
                        commands.Add(CheckpointCommand());
                        break;
                    case "position":
                        commands.Add(PositionCommand(args));
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
        public Command MoodCommand(string[] args)
        {
            Crewman crewman = SpriteFromName(args[1]) as Crewman;
            if (crewman == null) crewman = ActivePlayer;
            bool sad = (args[2].ToLower() == "sad" || args[2] == "1");
            return new Command(() =>
            {
                if (crewman == null && args.ElementAtOrDefault(1).ToLower() == "player") crewman = ActivePlayer;
                if (crewman != null)
                    crewman.Sad = sad;
            });
        }
        public Command CheckpointCommand()
        {
            return new Command(() =>
            {
                ActivePlayer.CheckpointFlipX = ActivePlayer.FlipX;
                ActivePlayer.CheckpointFlipY = ActivePlayer.FlipY;
                ActivePlayer.CheckpointX = ActivePlayer.CenterX;
                ActivePlayer.CheckpointY = ActivePlayer.FlipY ? ActivePlayer.Y : ActivePlayer.Bottom;
                if (ActivePlayer.CurrentCheckpoint != null)
                {
                    ActivePlayer.CurrentCheckpoint.Deactivate();
                    ActivePlayer.CurrentCheckpoint = null;
                }
            });
        }
        public Command PositionCommand(string[] args)
        {
            string p = args.ElementAtOrDefault(1);
            string c = args.FirstOrDefault();

            return new Command(() =>
            {
                if (TextBoxes.Count > 0)
                {
                    VTextBox tb = TextBoxes.Last();
                    int x = 0;
                    int y = 0;
                    if (c.ToLower() == "centerx" || c.ToLower() == "center")
                        tb.CenterX = RESOLUTION_WIDTH / 2;
                    if (c.ToLower() == "centery" || c.ToLower() == "center")
                        tb.CenterY = RESOLUTION_HEIGHT / 2;
                    Crewman crewman = SpriteFromName(c) as Crewman;
                    if (crewman != null)
                    {
                        if (p == "above")
                        {
                            tb.Bottom = crewman.Y - 2;
                            tb.X = crewman.X - 16;
                        }
                        else
                        {
                            tb.Y = crewman.Bottom + 2;
                            tb.X = crewman.X - 16;
                        }
                    }
                }
            });
        }
    }
}
