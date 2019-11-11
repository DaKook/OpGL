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
            Up,
            Down,
            Jump,
            Pause,
            Kill,
            Escape,
            Count
        }
        private int[] inputs = new int[(int)Inputs.Count];
        private List<Inputs> bufferInputs = new List<Inputs>();
        private int[] lastPressed = new int[(int)Inputs.Count];
        public Dictionary<Keys, Inputs> inputMap = new Dictionary<Keys, Inputs>() {
            { Keys.Left, Inputs.Left }, { Keys.A, Inputs.Left },
            { Keys.Right, Inputs.Right }, { Keys.D, Inputs.Right },
            //{ Keys.Up, Inputs.Up }, { Keys.W, Inputs.Up },
            //{ Keys.Down, Inputs.Down }, { Keys.S, Inputs.Down },
            { Keys.Up, Inputs.Jump }, { Keys.Down, Inputs.Jump }, { Keys.Space, Inputs.Jump }, { Keys.Z, Inputs.Jump }, { Keys.V, Inputs.Jump },
            { Keys.Enter, Inputs.Pause },
            { Keys.Escape, Inputs.Escape },
            { Keys.R, Inputs.Kill }
        };
        private SortedSet<Keys> heldKeys = new SortedSet<Keys>();
        private int mouseX = -1;
        private int mouseY = -1;
        private bool leftMouse = false;
        private bool rightMouse = false;
        private bool middleMouse = false;

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
            Scripts.TryGetValue(name ?? "", out Script script);
            return script;
        }

        public bool PlayerControl = true;
        public bool Freeze = false;
        public bool WaitingForAction;
        public List<Script> CurrentScripts = new List<Script>();
        public List<VTextBox> TextBoxes = new List<VTextBox>();
        private static Random r = new Random();
        public SortedList<string, Number> Vars = new SortedList<string, Number>();

        // OpenGL
        private GlControl glControl;
        private ProgramData program;
        private uint fbo;

        // Textures
        public Texture FontTexture;
        public Texture BoxTexture;
        private List<Texture> textures;
        public Texture TextureFromName(string name)
        {
            foreach (Texture texture in textures)
            {
                if (texture.Name.ToLower() == name.ToLower()) return texture;
            }
            return null;
        }

        // Sounds
        public SortedList<string, SoundEffect> Sounds = new SortedList<string, SoundEffect>();
        public SoundEffect GetSound(string name)
        {
            if (Sounds.TryGetValue(name, out SoundEffect se))
                return se;
            else
                return null;
        }
        public SortedList<string, Music> Songs = new SortedList<string, Music>();
        public Music GetMusic(string name)
        {
            if (Songs.TryGetValue(name, out Music m))
                return m;
            else
                return null;
        }
        public Music CurrentSong;

        // Editing
        private enum Tools { Ground, Background, Spikes, Tiles, Checkpoint, Enemy, Platform, Terminal,
            Select
        }
        private Tools tool = Tools.Ground;
        private enum FocusOptions { Level, Tileset, Dialog }
        private FocusOptions CurrentEditingFocus = FocusOptions.Level;
        private BoxSprite selection;
        private Point currentTile = new Point(0, 0);
        private Texture currentTexture;
        private AutoTileSettings autoTiles
        {
            get
            {
                if (tool == Tools.Background) return backgroundTiles;
                else if (tool == Tools.Spikes) return spikesTiles;
                else return groundTiles;
            }
            set
            {
                if (tool == Tools.Background) backgroundTiles = value;
                else if (tool == Tools.Spikes) spikesTiles = value;
                else groundTiles = value;
            }
        }
        private AutoTileSettings groundTiles;
        private AutoTileSettings backgroundTiles;
        private AutoTileSettings spikesTiles;
        private FullImage tileset;
        private BoxSprite tileSelection;
        private bool isEditor;
        private bool selecting;
        private List<Sprite> selectedSprites = new List<Sprite>();
        private PointF selectOrigin;

        // Rooms
        public Room CurrentRoom;
        public SortedList<int, JObject> RoomDatas = new SortedList<int, JObject>();
        public int FocusedRoom;
        public int WidthRooms;
        public int HeightRooms;
        public List<RoomGroup> RoomGroups = new List<RoomGroup>();

        public int StartX;
        public int StartY;
        public int StartRoomX;
        public int StartRoomY;

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
        private float cameraX;
        private float cameraY;
        private int flashFrames;
        private bool isFlashing = false;
        private Color flashColour = Color.White;
        private int shakeFrames;
        private int shakeIntensity;
        public const int RESOLUTION_WIDTH = 320;
        public const int RESOLUTION_HEIGHT = 240;

        // Sprites
        private SpriteCollection sprites
        {
            get => CurrentRoom.Objects;
        }
        public SpriteCollection hudSprites;
        public BGSpriteCollection BGSprites;
        public SortedList<string, Sprite> UserAccessSprites = new SortedList<string, Sprite>();
        public Sprite SpriteFromName(string name)
        {
            UserAccessSprites.TryGetValue(name, out Sprite sprite);
            return sprite;
        }

        public Crewman ActivePlayer;
        public void SetPlayer(Crewman player)
        {
            if (ActivePlayer != null)
            {
                ActivePlayer.Respawned -= RespawnPlatforms;
            }
            ActivePlayer = player;
            ActivePlayer.Respawned += RespawnPlatforms;
        }
        public void RespawnPlatforms()
        {
            foreach (Sprite sprite in sprites)
            {
                if (!(sprite is Platform)) continue;
                Platform platform = sprite as Platform;
                if (!platform.Visible && platform.Animation == platform.DisappearAnimation)
                {
                    platform.Reappear();
                }
            }
        }
        public bool IsPlaying { get; private set; } = false;
        private int FrameCount = 1; // start at 1 so inputs aren't "new" at start
        public enum GameStates { Playing, Editing, Menu }
        public GameStates CurrentState = GameStates.Playing;

        public Game(GlControl control)
        {
            glControl = control;

            InitGlProgram();
            InitOpenGLSettings();
            InitSounds();
            InitMusic();

            textures = new List<Texture>();
            LoadTextures();

            hudSprites = new SpriteCollection();
#if TEST
            Texture viridian = TextureFromName("viridian");
            Texture tiles = TextureFromName("tiles");
            Texture platforms = TextureFromName("platforms");
            Texture sprites32 = TextureFromName("sprites32");
            Texture gravityline = TextureFromName("gravityline");
            Texture background = TextureFromName("background");
            BGSprites = new BGSpriteCollection(background);
            BGSprites.Populate(20, new Animation[] { Animation.Static(2, 0, background), background.AnimationFromName("Star1") }, 1, new PointF(3, 0));
            BGSprites.Populate(20, new Animation[] { Animation.Static(3, 0, background), background.AnimationFromName("Star2") }, 1, new PointF(2, 0));
            BGSprites.Populate(20, new Animation[] { Animation.Static(4, 0, background), background.AnimationFromName("Star3") }, 1, new PointF(1, 0));
            BGSprites.Color = Color.Gray;
            //BGSprites.Populate(2000, new Animation[] { background.AnimationFromName("Snow1"), background.AnimationFromName("Snow2"), background.AnimationFromName("Snow3"), background.AnimationFromName("Snow4") }, 1, new PointF(14f, 1f));
            FontTexture = TextureFromName("font");
            BoxTexture = TextureFromName("box");
            Crewman.Flip1 = GetSound("jump");
            Crewman.Flip2 = GetSound("jump2");
            Crewman.Cry = GetSound("hurt");
            Platform.DisappearSound = GetSound("vanish");
            Checkpoint.ActivateSound = GetSound("save");
            Terminal.ActivateSound = GetSound("terminal");
            WarpToken.WarpSound = GetSound("teleport");
            selection = new BoxSprite(0, 0, BoxTexture, 1, 1, Color.Blue);
            hudSprites.Add(selection);
            tileSelection = new BoxSprite(0, 0, BoxTexture, 1, 1, Color.Red);
            selection.Visible = false;
            currentTexture = tiles;
            tileset = new FullImage(0, 0, tiles);

            //This will probably be moved somewhere else and might be customizeable per-level
            Terminal.TextBox = new VTextBox(0, 0, FontTexture, " Press ENTER to activate terminal ", Color.FromArgb(255, 130, 20));
            Terminal.TextBox.CenterX = RESOLUTION_WIDTH / 2;
            Terminal.TextBox.Y = 4;
            hudSprites.Add(Terminal.TextBox);
            hudSprites.Add(timerSprite = new StringDrawable(8, RESOLUTION_HEIGHT - 12, TextureFromName("font2"), "TEST", Color.White));

            JObject jObject = JObject.Parse(System.IO.File.ReadAllText("levels/TestLevel.lv7"));
            LoadLevel(jObject);
            ActivePlayer.Layer = 1;
            //WarpLine wl = new WarpLine(319, 200, 32, false, -152, 0);
            //sprites.Add(wl);
            CurrentState = GameStates.Editing;
            isEditor = true;
            tool = Tools.Ground;
            CurrentSong = Songs["Peregrinator Homo"];
            CurrentSong.Play();
            groundTiles = AutoTileSettings.Default13(3, 2);
            groundTiles.Name = "Ground";
            backgroundTiles = AutoTileSettings.Default13(0, 17);
            backgroundTiles.Name = "BG";
            spikesTiles = AutoTileSettings.Default4(8, 0);
            spikesTiles.Name = "Spikes";
            //WarpToken wt = new WarpToken(200, 180, sprites32, sprites32.AnimationFromName("WarpToken"), 16, 8, this, WarpToken.FlipSettings.Flip);
            //sprites.Add(wt);

#endif
            glControl.Render += glControl_Render;
            glControl.Resize += glControl_Resize;
            glControl.KeyDown += GlControl_KeyDown;
            glControl.KeyUp += GlControl_KeyUp;
            glControl.MouseMove += GlControl_MouseMove;
            glControl.MouseDown += GlControl_MouseDown;
            glControl.MouseUp += GlControl_MouseUp;
            glControl.MouseLeave += GlControl_MouseLeave;
        }

        private void GlControl_MouseLeave(object sender, EventArgs e)
        {
            mouseX = -1;
            mouseY = -1;
        }

        private void GlControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                leftMouse = false;
            else if (e.Button == MouseButtons.Right)
                rightMouse = false;
            else if (e.Button == MouseButtons.Middle)
                middleMouse = false;
        }

        private void GlControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                leftMouse = true;
                TriggerLeftClick();
            }
            else if (e.Button == MouseButtons.Right)
            {
                rightMouse = true;
            }
            else if (e.Button == MouseButtons.Middle)
                middleMouse = true;
        }

        private void TriggerLeftClick()
        {
            if (CurrentState == GameStates.Editing)
            {
                if (tool == Tools.Tiles)
                {
                    //sprites.Add(new Tile((int)(selection.X + cameraX), (int)(selection.Y + cameraY), currentTexture, currentTile.X, currentTile.Y));
                }
            }
            else
                ActivePlayer.X += 100;
        }

        private void GlControl_MouseMove(object sender, MouseEventArgs e)
        {
            mouseX = (int)(e.X * ((float)RESOLUTION_WIDTH / glControl.Width));
            mouseY = (int)(e.Y * ((float)RESOLUTION_HEIGHT / glControl.Height));
        }

        //INITIALIZE
        #region "Init"
        private void InitGlProgram()
        {
            program = new ProgramData(GLProgram.Load("shaders/v2dTexTransform.txt", "shaders/f2dTex.txt"));

            Gl.UseProgram(program.ID);
            int modelMatrixLoc = Gl.GetUniformLocation(program.ID, "model");
            Gl.UniformMatrix4f(modelMatrixLoc, 1, false, Matrix4x4f.Identity);

            // origin at top-left
            camera = Matrix4x4f.Translated(-1f, 1f, 0f);
            camera.Scale(2f / RESOLUTION_WIDTH, -2f / RESOLUTION_HEIGHT, 1);
            hudView = camera;
            int viewMatrixLoc = Gl.GetUniformLocation(program.ID, "view");
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
                        int gridSize = (int)jObject["GridSize"];
                        int gridSize2;
                        if (jObject.ContainsKey("GridSize2"))
                            gridSize2 = (int)jObject["GridSize2"];
                        else
                            gridSize2 = gridSize;
                        Texture tex = CreateTexture(fName, gridSize, gridSize2);
                        textures.Add(tex);

                        // Animations
                        JArray arr = (JArray)jObject["Animations"];
                        if (arr != null)
                        {
                            SortedList<string, Animation> anims = new SortedList<string, Animation>();
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
                            Sprite.SolidState[,] states = new Sprite.SolidState[(int)(tex.Width / tex.TileSizeX), (int)(tex.Height / tex.TileSizeY)];
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
                        // Characters
                        JArray chrs = (JArray)jObject["Characters"];
                        if (chrs != null)
                        {
                            SortedList<int, int> characters = new SortedList<int, int>();
                            int i = 0;
                            int x = 0;
                            int y = 0;
                            while (i < chrs.Count)
                            {
                                int ch = (int)chrs[i];
                                if (ch > 0 && ch < 256 && y == 0)
                                {
                                    x = ch;
                                    y = 1;
                                }
                                else if (y > 0 && ch >= 0)
                                {
                                    if (!characters.ContainsKey(ch))
                                        characters.Add(x, ch);
                                    else
                                        characters[x] = ch;
                                    y -= 1;
                                    x += 1;
                                }
                                else if (ch < 0)
                                {
                                    y = -ch;
                                }
                                i++;
                            }
                            tex.CharacterWidths = characters;
                        }
                    }
                    else // no _data file, create with default grid size
                    {
                        textures.Add(CreateTexture(fName, 32, 32));
                        textures.Last().Animations = new SortedList<string, Animation>();
                    }
                }
            }
        }
        private Texture CreateTexture(string texture, int gridSize, int gridSize2)
        {
            SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.Decode("textures/" + texture + ".png");

            uint texa = Gl.CreateVertexArray();
            Gl.BindVertexArray(texa);
            uint texb = Gl.CreateBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, texb);
            float[] fls = new float[]
            {
                0f,       0f,        0f, 0f,
                0f,       gridSize2, 0f, 1f,
                gridSize, gridSize2, 1f, 1f,
                gridSize, 0f,        1f, 0f
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

            return new Texture(tex, bmp.Width, bmp.Height, gridSize, gridSize2, texture, program, texa, texb);
        }

        private void InitOpenGLSettings()
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.OneMinusDstAlpha, BlendingFactor.One);

            glControl_Resize(null, null);

            Gl.ClearColor(0f, 0f, 0f, 1f);

            // screenshot
            fbo = Gl.CreateFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            uint fTex = Gl.CreateTexture(TextureTarget.Texture2d);
            Gl.BindTexture(TextureTarget.Texture2d, fTex);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, RESOLUTION_WIDTH, RESOLUTION_HEIGHT, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            Gl.GenerateMipmap(TextureTarget.Texture2d);

            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, fTex, 0);
            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
                throw new Exception("hey");
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void InitSounds()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("sounds/").ToList();
            files.Sort();
            foreach (string file in files)
            {
                if (!file.EndsWith(".wav")) continue;
                SoundEffect se = new SoundEffect(file);
                Sounds.Add(se.Name, se);
            }
        }

        private void InitMusic()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("music/").ToList();
            files.Sort();
            foreach (string file in files)
            {
                if (!(file.EndsWith(".ogg") || file.EndsWith(".wav"))) continue;
                Music m = new Music(file);
                Songs.Add(m.Name, m);
            }
        }
        //END INITIALIZE
        #endregion
        private void Screenshot()
        {
            Debugger.NotifyOfCrossThreadDependency();
            if (glControl.InvokeRequired)
                glControl.Invoke((Action)(() => RenderScreengrab()));
            else
                RenderScreengrab();
        }
        private unsafe void RenderScreengrab()
        {
            RenderOffScreen();
            glControl_Render(null, null);

            // bitmap header
            const int headerSize = 26;
            int rowSize = RESOLUTION_WIDTH * 4;
            int fileSize = headerSize + rowSize * RESOLUTION_HEIGHT;
            byte[] data = new byte[RESOLUTION_WIDTH * RESOLUTION_HEIGHT * 4 + headerSize];
            data[0] = 0x42; // BM
            data[1] = 0x4D;
            Array.Copy(BitConverter.GetBytes(fileSize), 0, data, 0x2, 4); // size of file
            data[0x0A] = headerSize; // pointer to pixel array
            data[0x0E] = 12; // size of BITMAPCOREHEADER
            Array.Copy(BitConverter.GetBytes((ushort)RESOLUTION_WIDTH), 0, data, 0x12, 2);
            Array.Copy(BitConverter.GetBytes((ushort)RESOLUTION_HEIGHT), 0, data, 0x14, 2);
            data[0x16] = 1; // "must be 1"
            data[0x18] = 32; // bpp

            fixed (byte* fixedData = data)
                Gl.ReadPixels(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)(fixedData + headerSize));
            RenderOnScreen();

            System.IO.File.WriteAllBytes("screenshot.bmp", data);
        }
        private void RenderOffScreen()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            Gl.Viewport(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
        }
        private void RenderOnScreen()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            glControl_Resize(null, null);
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

        private void GlControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (inputMap.ContainsKey(e.KeyCode) && !heldKeys.Contains(e.KeyCode))
            {
                bufferInputs.Add(inputMap[e.KeyCode]);
            }
            if (!heldKeys.Contains(e.KeyCode))
                heldKeys.Add(e.KeyCode);
            if (CurrentState == GameStates.Editing)
            {
                if (CurrentEditingFocus == FocusOptions.Level)
                {
                    if (e.Control && e.KeyCode == Keys.S)
                    {
                        string s = SaveLevel().ToString();
                        System.IO.File.WriteAllText("levels/TestLevel.lv7", s);
                    }
                    else if (e.KeyCode == Keys.Right)
                    {
                        sprites.Remove(ActivePlayer);
                        RoomDatas[FocusedRoom] = CurrentRoom.Save();
                        LoadRoom((CurrentRoom.X + 1) % WidthRooms, CurrentRoom.Y);
                    }
                    else if (e.KeyCode == Keys.Left)
                    {
                        sprites.Remove(ActivePlayer);
                        RoomDatas[FocusedRoom] = CurrentRoom.Save();
                        int x = CurrentRoom.X - 1;
                        if (x < 0) x = WidthRooms - 1;
                        LoadRoom(x, CurrentRoom.Y);
                    }
                    else if (e.KeyCode == Keys.Down)
                    {
                        sprites.Remove(ActivePlayer);
                        RoomDatas[FocusedRoom] = CurrentRoom.Save();
                        LoadRoom(CurrentRoom.X, (CurrentRoom.Y + 1) % HeightRooms);
                    }
                    else if (e.KeyCode == Keys.Up)
                    {
                        sprites.Remove(ActivePlayer);
                        RoomDatas[FocusedRoom] = CurrentRoom.Save();
                        int y = CurrentRoom.Y - 1;
                        if (y < 0) y = HeightRooms - 1;
                        LoadRoom(CurrentRoom.X, y);
                    }
                    else if (e.KeyCode == Keys.S)
                    {
                        currentTile.Y = (currentTile.Y + 1) % ((int)currentTexture.Height / currentTexture.TileSizeY);
                    }
                    else if (e.KeyCode == Keys.W)
                    {
                        currentTile.Y -= 1;
                        if (currentTile.Y < 0) currentTile.Y += (int)currentTexture.Height / currentTexture.TileSizeY;
                    }
                    else if (e.KeyCode == Keys.D)
                    {
                        currentTile.X = (currentTile.X + 1) % ((int)currentTexture.Width / currentTexture.TileSizeX);
                    }
                    else if (e.KeyCode == Keys.A)
                    {
                        currentTile.X -= 1;
                        if (currentTile.X < 0) currentTile.X += (int)currentTexture.Width / currentTexture.TileSizeX;
                    }
                    else if (e.KeyCode == Keys.D1)
                    {
                        tool = Tools.Ground;
                    }
                    else if (e.KeyCode == Keys.D2)
                    {
                        tool = Tools.Background;
                    }
                    else if (e.KeyCode == Keys.D3)
                    {
                        tool = Tools.Spikes;
                    }
                    else if (e.KeyCode == Keys.OemMinus)
                    {
                        tool = Tools.Tiles;
                        tileSelection.X = currentTile.X * 8;
                        tileSelection.Y = currentTile.Y * 8;
                        tileSelection.SetSize(1, 1);
                    }
                    else if (e.KeyCode == Keys.Tab)
                    {
                        CurrentEditingFocus = FocusOptions.Tileset;
                        tileset.Layer = -1;
                        if (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes)
                        {
                            if (autoTiles.Size == 3)
                            {
                                selection.SetSize(3, 1);
                                tileSelection.SetSize(3, 1);
                            }
                            else if (autoTiles.Size == 13)
                            {
                                selection.SetSize(3, 5);
                                tileSelection.SetSize(3, 5);
                            }
                            else if (autoTiles.Size == 47)
                            {
                                selection.SetSize(8, 6);
                                tileSelection.SetSize(8, 6);
                            }
                            else if (autoTiles.Size == 4)
                            {
                                selection.SetSize(4, 1);
                                tileSelection.SetSize(4, 1);
                            }
                            tileSelection.X = autoTiles.Origin.X * currentTexture.TileSizeX;
                            tileSelection.Y = autoTiles.Origin.Y * currentTexture.TileSizeY;
                        }
                        else if (tool == Tools.Tiles)
                        {
                            tileSelection.SetSize(1, 1);
                            tileSelection.X = currentTile.X * currentTexture.TileSizeX;
                            tileSelection.Y = currentTile.Y * currentTexture.TileSizeY;
                        }
                        hudSprites.Add(tileSelection);
                        hudSprites.Add(tileset);
                    }
                    else if (e.KeyCode == Keys.Enter)
                    {
                        LoadRoom(CurrentRoom.X, CurrentRoom.Y);
                        CurrentState = GameStates.Playing;
                        ActivePlayer.Visible = true;
                        selection.Visible = false;
                    }
                }
                else if (CurrentEditingFocus == FocusOptions.Tileset)
                {
                    if (e.KeyCode == Keys.Tab)
                    {
                        CurrentEditingFocus = FocusOptions.Level;
                        hudSprites.Remove(tileset);
                        hudSprites.Remove(tileSelection);
                        selection.SetSize(1, 1);
                    }
                    else if (e.KeyCode == Keys.D1 && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes))
                    {
                        selection.SetSize(3, 5);
                    }
                    else if (e.KeyCode == Keys.D2 && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes))
                    {
                        selection.SetSize(3, 1);
                    }
                    else if (e.KeyCode == Keys.D3 && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes))
                    {
                        selection.SetSize(8, 6);
                    }
                    else if (e.KeyCode == Keys.D4 && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes))
                    {
                        selection.SetSize(4, 1);
                    }
                }
            }
        }
        private void GlControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (inputMap.ContainsKey(e.KeyCode))
            {
                inputs[(int)inputMap[e.KeyCode]]--;
            }
            heldKeys.Remove(e.KeyCode);
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
                for (int i = 0; i < bufferInputs.Count; i++)
                {
                    inputs[(int)bufferInputs[i]]++;
                    lastPressed[(int)bufferInputs[i]] = FrameCount;
                }
                bufferInputs.Clear();

                // begin frame
                if (CurrentState == GameStates.Playing)
                {
                    HandleUserInputs();

                    if (!Freeze && CurrentState == GameStates.Playing)
                        ProcessWorld();

                    for (int i = hudSprites.Count - 1; i >= 0; i--)
                    {
                        Sprite d = hudSprites[i];
                        d.Process();
                    }
                    BGSprites.Process();
                    for (int i = 0; i < CurrentScripts.Count; i++)
                    {
                        Script script = CurrentScripts[i];
                        script.Process();
                        if (script.IsFinished)
                        {
                            CurrentScripts.RemoveAt(i--);
                        }
                    }
                }
                else if (CurrentState == GameStates.Editing)
                {
                    HandleEditingInputs();
                }

                // end frame
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

        private Tile GetTile(int x, int y, int layer = -2)
        {

            Tile ret = null;
            List<Sprite> spr = sprites.GetPotentialColliders(x, y);
            if (spr.Count == 0) return null;
            else
            {
                for (int i = 0; i < spr.Count; i++)
                {
                    if (spr[i] is Tile && spr[i].Layer == layer) return spr[i] as Tile;
                }
            }
            return ret;
        }

        private void HandleEditingInputs()
        {
            sprites.SortForCollisions();
            if (mouseX > -1 && mouseY > -1 || CurrentEditingFocus == FocusOptions.Dialog)
            {
                selection.Visible = true;
                if (!heldKeys.Contains(Keys.OemCloseBrackets))
                    selection.X = mouseX - mouseX % 8;
                if (!heldKeys.Contains(Keys.OemOpenBrackets))
                    selection.Y = mouseY - mouseY % 8;
            }
            else
            {
                selection.Visible = false;
            }
            if (CurrentEditingFocus == FocusOptions.Level)
            {
                if (heldKeys.Contains(Keys.Control) || tool == Tools.Select || selecting || selectedSprites.Count > 0)
                {
                    if (leftMouse && !selecting && selectedSprites.Count == 0)
                    {
                        selecting = true;
                        selectOrigin = new PointF(selection.X, selection.Y);
                    }
                    else if (!leftMouse && selecting && selectedSprites.Count == 0)
                    {

                    }
                }
                else
                {
                    if (tool == Tools.Tiles)
                    {
                        if (leftMouse || rightMouse)
                        {
                            TileTool(selection.X + cameraX, selection.Y + cameraY, leftMouse);
                        }
                        else if (middleMouse)
                        {
                            Tile t = GetTile((int)(selection.X + cameraX), (int)(selection.Y + cameraY));
                            if (t != null)
                            {
                                currentTexture = t.Texture;
                                currentTile = new Point(t.TextureX, t.TextureY);
                            }
                        }
                    }
                    else if (tool == Tools.Ground || tool == Tools.Background || tool == Tools.Spikes && autoTiles != null)
                    {
                        if (leftMouse || rightMouse)
                        {
                            AutoTilesTool(selection.X + cameraX, selection.Y + cameraY, leftMouse, tool == Tools.Background);
                        }
                    }
                }
            }
            else if (CurrentEditingFocus == FocusOptions.Tileset)
            {
                if (tool == Tools.Tiles)
                {
                    if (leftMouse)
                    {
                        currentTile = new Point((int)selection.X / 8, (int)selection.Y / 8);
                        tileSelection.X = selection.X;
                        tileSelection.Y = selection.Y;
                        tileSelection.SetSize(1, 1);
                    }
                }
                else if (tool == Tools.Ground || tool == Tools.Background || tool == Tools.Spikes)
                {
                    if (leftMouse)
                    {
                        if (selection.WidthTiles == 3 && selection.HeightTiles == 1)
                        {
                            autoTiles = AutoTileSettings.Default3((int)selection.X / 8, (int)selection.Y / 8);
                            autoTiles.Name = "(" + currentTexture.Name + ") " + ((int)(selection.X / 8)).ToString() + ", " + ((int)(selection.Y / 8)).ToString() + ": Auto3";
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(3, 1);
                        }
                        else if (selection.HeightTiles == 5)
                        {
                            autoTiles = AutoTileSettings.Default13((int)selection.X / 8, (int)selection.Y / 8);
                            autoTiles.Name = "(" + currentTexture.Name + ") " + ((int)(selection.X / 8)).ToString() + ", " + ((int)(selection.Y / 8)).ToString() + ": Auto13";
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(3, 5);
                        }
                        else if (selection.WidthTiles == 8)
                        {
                            autoTiles = AutoTileSettings.Default47((int)selection.X / 8, (int)selection.Y / 8);
                            autoTiles.Name = "(" + currentTexture.Name + ") " + ((int)(selection.X / 8)).ToString() + ", " + ((int)(selection.Y / 8)).ToString() + ": Auto47";
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(8, 6);
                        }
                        else if (selection.WidthTiles == 4)
                        {
                            autoTiles = AutoTileSettings.Default4((int)selection.X / 8, (int)selection.Y / 8);
                            autoTiles.Name = "(" + currentTexture.Name + ") " + ((int)(selection.X / 8)).ToString() + ", " + ((int)(selection.Y / 8)).ToString() + ": Auto4";
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(4, 1);
                        }
                    }
                }
            }
        }

        private void TileTool(float x, float y, bool leftClick)
        {
            Tile tile = GetTile((int)x, (int)y);
            if (tile != null)
            {
                sprites.RemoveFromCollisions(tile);
            }
            if (leftClick)
            {
                Tile t = new Tile((int)x, (int)y, currentTexture, currentTile.X, currentTile.Y);
                sprites.AddForCollisions(t);
            }
        }

        private void AutoTilesTool(float x, float y, bool leftClick, bool isBackground)
        {
            Tile tile = GetTile((int)x, (int)y);
            if (tile != null)
            {
                sprites.RemoveFromCollisions(tile);
            }
            if (leftClick)
            {
                Point p = autoTiles.GetTile(AutoTilesPredicate((int)x, (int)y));
                Tile t = new Tile((int)x, (int)y, currentTexture, p.X, p.Y);
                t.Tag = autoTiles.Name;
                sprites.AddForCollisions(t);
            }
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (i != 0 || j != 0)
                    {
                        int xx = (int)x + (i * 8);
                        int yy = (int)y + (j * 8);
                        if (GetTile(xx, yy)?.Tag == autoTiles.Name)
                        {
                            tile = GetTile(xx, yy);
                            if (tile != null)
                            {
                                sprites.RemoveFromCollisions(tile);
                            }
                            Point p = autoTiles.GetTile(AutoTilesPredicate(xx, yy));
                            Tile t = new Tile(xx, yy, currentTexture, p.X, p.Y);
                            t.Tag = autoTiles.Name;
                            sprites.AddForCollisions(t);
                        }
                    }
                }
            }
        }

        private Predicate<Point> AutoTilesPredicate(int x, int y)
        {
            bool bg = tool != Tools.Ground;
            bool sp = tool == Tools.Spikes;
            Tile gt;
            return (p) => (gt = GetTile(p.X + x, p.Y + y)) != null && ((!sp && gt.Tag == autoTiles.Name) || (bg && gt.Solid == Sprite.SolidState.Ground)) ||
            (p.X < 0 && x == 0) ||
            (p.X > 0 && x == RESOLUTION_WIDTH - 8) ||
            (p.Y < 0 && y == 0) ||
            (p.Y > 0 && y == RESOLUTION_HEIGHT - 8);
        }

        private void HandleUserInputs()
        {
            if (isEditor && IsInputActive(Inputs.Escape))
            {
                CurrentScripts.Clear();
                foreach (VTextBox box in TextBoxes)
                {
                    box.Disappear();
                }
                LoadRoom(CurrentRoom.X, CurrentRoom.Y);
                ActivePlayer.Visible = false;
                selection.Visible = true;
                CurrentState = GameStates.Editing;
            }
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
                    Script s = t.Script;
                    if (t.Repeat)
                        s.Finished += (script) => {
                            t.AlreadyUsed = false;
                            if (t.IsOverlapping(ActivePlayer))
                            {
                                ActivePlayer.CurrentTerminal = t;
                                Terminal.TextBox.Appear();
                            }
                        };
                    s.ExecuteFromBeginning();
                    if (!s.IsFinished)
                        CurrentScripts.Add(s);
                }
            }

            if (IsInputActive(Inputs.Jump))
            {
                if (IsInputNew(Inputs.Jump))
                {
                    if (WaitingForAction)
                    {
                        WaitingForAction = false;
                        for (int i = CurrentScripts.Count - 1; i >= 0; i--)
                        {
                            Script script = CurrentScripts[i];
                            if (script.WaitingForAction != null)
                            {
                                script.WaitingForAction();
                                script.WaitingForAction = null;
                                script.Continue();
                            }
                        }
                    }
                    else if (PlayerControl)
                        ActivePlayer.FlipOrJump();
                }
            }
        }

        public void LoadRoom(int x, int y)
        {
            if (isEditor && CurrentState == GameStates.Editing)
            {
                sprites.Remove(ActivePlayer);
                ActivePlayer.IsWarpingH = false;
                ActivePlayer.IsWarpingV = false;
                ActivePlayer.MultiplePositions = false;
                ActivePlayer.Offsets.Clear();
                RoomDatas[FocusedRoom] = CurrentRoom.Save();
            }
            FocusedRoom = x + y * WidthRooms;
            if (!RoomDatas.ContainsKey(FocusedRoom))
            {
                Room r = new Room(new SpriteCollection(), Script.Empty, Script.Empty);
                r.X = x;
                r.Y = y;
                RoomDatas.Add(FocusedRoom, r.Save());
            }
            CurrentRoom = Room.LoadRoom(RoomDatas[FocusedRoom], this);
            cameraX = CurrentRoom.X * Room.ROOM_WIDTH;
            cameraY = CurrentRoom.Y * Room.ROOM_HEIGHT;
            CurrentRoom.Objects.Add(ActivePlayer);
            ActivePlayer.CenterX = (ActivePlayer.CenterX + Room.ROOM_WIDTH) % Room.ROOM_WIDTH + CurrentRoom.X * Room.ROOM_WIDTH;
            ActivePlayer.CenterY = (ActivePlayer.CenterY + Room.ROOM_HEIGHT) % Room.ROOM_HEIGHT + CurrentRoom.Y * Room.ROOM_HEIGHT;

            //for (int i = 0; i < sprites.Count; i++)
            //{
            //    if (sprites[i] is Tile)
            //    {
            //        int l = (int)(sprites[i].X + sprites[i].Y * RESOLUTION_WIDTH);
            //        if (tiles.ContainsKey(l)) tiles.Remove(l);
            //        tiles.Add(l, sprites[i] as Tile);
            //    }
            //}

            //ActivePlayer.PreviousX = ActivePlayer.PreviousX % Room.ROOM_WIDTH + CurrentRoom.X * Room.ROOM_WIDTH;
            //ActivePlayer.PreviousY = ActivePlayer.PreviousY % Room.ROOM_HEIGHT + CurrentRoom.Y * Room.ROOM_HEIGHT;
        }

        private void ProcessWorld()
        {
            List<Sprite> mpSprites = new List<Sprite>();
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
            else if (ActivePlayer.CurrentTerminal != null && !Terminal.TextBox.Visible)
            {
                Terminal.TextBox.Appear();
            }
            if (!ActivePlayer.IsWarpingV && !ActivePlayer.IsWarpingH)
            {
                if (ActivePlayer.CenterX > (CurrentRoom.X + 1) * Room.ROOM_WIDTH)
                    LoadRoom((CurrentRoom.X + 1) % WidthRooms, CurrentRoom.Y);
                else if (ActivePlayer.CenterX < CurrentRoom.X * Room.ROOM_WIDTH)
                    LoadRoom((CurrentRoom.X + WidthRooms - 1) % WidthRooms, CurrentRoom.Y);
                if (ActivePlayer.CenterY > (CurrentRoom.Y + 1) * Room.ROOM_HEIGHT)
                    LoadRoom(CurrentRoom.X, (CurrentRoom.Y + 1) % HeightRooms);
                else if (ActivePlayer.CenterY < CurrentRoom.Y * Room.ROOM_HEIGHT)
                    LoadRoom(CurrentRoom.X, (CurrentRoom.Y + HeightRooms - 1) % HeightRooms);
            }
        }

        //private List<Sprite> GetCollidersForRooms(Sprite sp)
        //{
        //    List<Sprite> ret = new List<Sprite>();
        //    int srx = (int)(sp.X - (sp.X % Room.ROOM_WIDTH)) / Room.ROOM_WIDTH;
        //    int sry = (int)(sp.Y - (sp.Y % Room.ROOM_HEIGHT)) / Room.ROOM_HEIGHT;
        //    int srx2 = (int)(sp.Right - (sp.Right % Room.ROOM_WIDTH)) / Room.ROOM_WIDTH;
        //    int sry2 = (int)(sp.Bottom - (sp.Bottom % Room.ROOM_HEIGHT)) / Room.ROOM_HEIGHT;
        //    for (int y = sry; y <= sry2; y++)
        //    {
        //        for (int x = srx; x <= srx2; x++)
        //        {
        //            if (CurrentRooms.ContainsKey(x + y * WidthRooms))
        //                ret.AddRange(CurrentRooms[x + y * WidthRooms].Objects.GetPotentialColliders(sp));
        //        }
        //    }
        //    return ret;
        //}

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
                        if (groundCollisions.Any((a) => a.CollidedWith.Solid == Sprite.SolidState.Ground && a.Vertical == c.Vertical && Math.Sign(c.Distance) != Math.Sign(a.Distance)))
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
                    if (c.CollidedWith.Solid == Sprite.SolidState.Entity || c.CollidedWith is GravityLine || c.CollidedWith is WarpLine)
                    {
                        entityCollisions.Add(c);
                        if (c.CollidedWith is WarpLine)
                        {
                            (c.CollidedWith as WarpLine).Collide(c.CollidedWith.TestCollision(drawable));
                        }
                        else
                        {
                            if (drawable is Platform)
                                PerformCollisionChecks(c.CollidedWith);
                            else
                                drawable.Collide(c);
                        }
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

        public void Flash(int frames, int r = 255, int g = 255, int b = 255)
        {
            flashFrames = frames;
            flashColour = Color.FromArgb(r, g, b);
        }
        public void Shake(int frames, int intensity = 2)
        {
            shakeFrames = frames;
            shakeIntensity = intensity;
        }
        public void AddSprite(Sprite s, float x, float y)
        {
            if (!sprites.Contains(s))
                sprites.Add(s);
            s.X = x;
            s.Y = y;
        }

        private void glControl_Render(object sender, GlControlEventArgs e)
        {
#if TEST
            Stopwatch t = new Stopwatch();
            t.Start();
#endif

            // clear the color buffer
            if (flashFrames > 0)
            {
                if (!isFlashing)
                {
                    isFlashing = true;
                    Gl.ClearColor((float)flashColour.R / 255, (float)flashColour.G / 255, (float)flashColour.B / 255, 1f);
                }
                flashFrames -= 1;
            }
            else if (isFlashing && flashFrames == 0)
            {
                isFlashing = false;
                Gl.ClearColor(0f, 0f, 0f, 1f);
            }
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            if (!isFlashing)
            {
                Matrix4x4f cam = camera;
                int offsetX = 0;
                int offsetY = 0;
                if (shakeFrames > 0)
                {
                    offsetX = r.Next(-shakeIntensity, shakeIntensity + 1);
                    offsetY = r.Next(-shakeIntensity, shakeIntensity + 1);
                    shakeFrames -= 1;
                }
                cam.Translate(-(cameraX + offsetX), -(cameraY + offsetY), 0);
                int viewMatrixLoc = Gl.GetUniformLocation(program.ID, "view");

                BGSprites.RenderPrep(viewMatrixLoc, camera);
                BGSprites.Render();

                Gl.UniformMatrix4f(viewMatrixLoc, 1, false, cam);
                sprites.Render();

                Gl.UniformMatrix4f(viewMatrixLoc, 1, false, hudView);
                hudSprites.Render();
            }

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

        public JObject SaveLevel()
        {
            JObject ret = new JObject();
            //Settings
            ret.Add("Width", WidthRooms);
            ret.Add("Height", HeightRooms);
            ret.Add("StartX", StartX);
            ret.Add("StartY", StartY);
            ret.Add("StartRoomX", StartRoomX);
            ret.Add("StartRoomY", StartRoomY);
            JObject[] jarr = new JObject[Scripts.Count];
            for (int i = 0; i < Scripts.Count; i++)
            {
                JObject scr = new JObject();
                scr.Add("Name", Scripts.Values[i].Name);
                scr.Add("Contents", Scripts.Values[i].Contents);
                jarr[i] = scr;
            }
            JArray arr = new JArray(jarr);
            ret.Add("Scripts", arr);
            jarr = new JObject[UserAccessSprites.Count];
            for (int i = 0; i < UserAccessSprites.Count; i++)
            {
                JObject obj = UserAccessSprites.Values[i].Save();
                jarr[i] = obj;
            }
            arr = new JArray(jarr);
            ret.Add("Objects", arr);
            jarr = RoomDatas.Values.ToArray();
            arr = new JArray(jarr);
            ret.Add("Rooms", arr);
            JObject player = ActivePlayer.Save();
            ret.Add("Player", player);
            return ret;
        }

        public void LoadLevel(JObject loadFrom)
        {
            Scripts.Clear();
            //Settings
            WidthRooms = (int)loadFrom["Width"];
            HeightRooms = (int)loadFrom["Height"];
            int startRoomX = (int)loadFrom["StartRoomX"];
            int startRoomY = (int)loadFrom["StartRoomY"];
            int startX = (int)loadFrom["StartX"];
            int startY = (int)loadFrom["StartY"];
            //Initialize scripts
            JArray scripts = (JArray)loadFrom["Scripts"];
            SortedList<string, string> scriptContents = new SortedList<string, string>();
            {
                foreach (JToken scr in scripts)
                {
                    string name = (string)scr["Name"];
                    Scripts.Add(name, new Script(null, name));
                    scriptContents.Add(name, (string)scr["Contents"]);
                }
            }
            //Objects
            {
                JArray objects = (JArray)loadFrom["Objects"];
                foreach (JToken sprite in objects)
                {
                    Sprite s = Sprite.LoadSprite(sprite, this);
                    if (s != null)
                        UserAccessSprites.Add(s.Name, s);
                }
            }
            //Rooms
            {
                JArray rooms = (JArray)loadFrom["Rooms"];
                foreach (JToken room in rooms)
                {
                    int x = (int)room["X"];
                    int y = (int)room["Y"];
                    int id = x + y * WidthRooms;
                    RoomDatas.Add(id, (JObject)room);
                }
            }
            //Load Scripts
            {
                for (int i = 0; i < Scripts.Count; i++)
                {
                    string name = Scripts.Keys[i];
                    string contents = scriptContents[name];
                    Script script = Scripts.Values[i];
                    script.Commands = Command.ParseScript(this, contents, script);
                    script.Contents = contents;
                }
            }
            //Load Player
            {
                Crewman player = Sprite.LoadSprite(loadFrom["Player"], this) as Crewman;
                SetPlayer(player);
                ActivePlayer.X = startX;
                ActivePlayer.Y = startY;
                LoadRoom(startRoomX, startRoomY);
            }
            //Load Room Groups
            {
                JArray groups = (JArray)loadFrom["Groups"];
                if (groups != null)
                    foreach (JToken group in groups)
                    {
                        string enterScript = (string)group["EnterScript"];
                        string exitScript = (string)group["ExitScript"];
                        JArray corners = (JArray)group["Rooms"];
                        if (corners.Count == 4)
                        {
                            RoomGroup newGroup = new RoomGroup(ScriptFromName(enterScript), ScriptFromName(exitScript));
                            Point topLeft = new Point((int)corners[0], (int)corners[1]);
                            Point bottomRight = new Point((int)corners[2], (int)corners[3]);
                            for (int x = topLeft.X; x < bottomRight.X + 1; x++)
                            {
                                for (int y = topLeft.Y; y < bottomRight.Y + 1; y++)
                                {
                                    int id = y * WidthRooms + x;
                                    newGroup.RoomDatas.Add(id, RoomDatas[id]);
                                }
                            }
                        }
                    }
            }
        }

        
    }
}
