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
            Count
        }
        private int[] inputs = new int[(int)Inputs.Count];
        private int[] lastPressed = new int[(int)Inputs.Count];
        public Dictionary<Keys, Inputs> inputMap = new Dictionary<Keys, Inputs>() {
            { Keys.Left, Inputs.Left }, { Keys.A, Inputs.Left },
            { Keys.Right, Inputs.Right }, { Keys.D, Inputs.Right },
            //{ Keys.Up, Inputs.Up }, { Keys.W, Inputs.Up },
            //{ Keys.Down, Inputs.Down }, { Keys.S, Inputs.Down },
            { Keys.Up, Inputs.Jump }, { Keys.Down, Inputs.Jump }, { Keys.Space, Inputs.Jump }, { Keys.Z, Inputs.Jump }, { Keys.V, Inputs.Jump },
            { Keys.Enter, Inputs.Pause },
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
            Scripts.TryGetValue(name, out Script script);
            return script;
        }

        public Action WaitingForAction = null;
        public int DelayFrames;
        public bool PlayerControl = true;
        public bool Freeze = false;
        public Script CurrentScript;
        public List<VTextBox> TextBoxes = new List<VTextBox>();

        // OpenGL
        private GlControl glControl;
        private uint program;

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

        // Editing
        private enum Tools { Ground, Background, Tiles, Checkpoint, Enemy, Platform, Terminal }
        private Tools tool = Tools.Ground;
        private BoxSprite selection;
        private Point currentTile = new Point(0, 0);
        private Texture currentTexture;

        // Rooms
        public Room CurrentRoom;
        public SortedList<int, JToken> RoomDatas = new SortedList<int, JToken>();
        public int FocusedRoom;
        public int WidthRooms;
        public int HeightRooms;

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
        private float _camX;
        private float _camY;
        private float cameraX
        {
            get => _camX;
            set
            {
                camera.Translate(-(value - _camX), 0, 0);
                _camX = value;
            }
        }
        private float cameraY
        {
            get => _camY;
            set
            {
                camera.Translate(0, -(value - _camY), 0);
                _camY = value;
            }
        }
        public const int RESOLUTION_WIDTH = 320;
        public const int RESOLUTION_HEIGHT = 240;

        // Sprites
        private SpriteCollection sprites
        {
            get => CurrentRoom.Objects;
        }
        public SpriteCollection hudSprites;
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

            textures = new List<Texture>();
            LoadTextures();

            hudSprites = new SpriteCollection();
#if TEST
            Texture viridian = TextureFromName("viridian");
            Texture tiles = TextureFromName("tiles");
            Texture platforms = TextureFromName("platforms");
            Texture sprites32 = TextureFromName("sprites32");
            Texture gravityline = TextureFromName("gravityline");
            FontTexture = TextureFromName("font");
            BoxTexture = TextureFromName("box");
            //Crewman newPlayer = new Crewman(20, 20, viridian, "Viridian", viridian.Animations["Standing"], viridian.Animations["Walking"], viridian.Animations["Falling"], viridian.Animations["Jumping"], viridian.Animations["Dying"]);
            ////ActivePlayer.CanFlip = false;
            ////ActivePlayer.Jump = 8;
            //UserAccessSprites.Add(newPlayer.Name, newPlayer);
            //newPlayer.TextBoxColor = Color.FromArgb(164, 164, 255);
            //SetPlayer(newPlayer);
            //LoadRoom(0, 0);
            //WidthRooms = 1;
            //HeightRooms = 1;
            selection = new BoxSprite(0, 0, BoxTexture, 1, 1, Color.Blue);
            hudSprites.Add(selection);
            selection.Visible = false;
            currentTexture = tiles;

            //This will probably be moved somewhere else and might be customizeable per-level
            Terminal.TextBox = new VTextBox(0, 0, FontTexture, " Press ENTER to activate terminal ", Color.FromArgb(255, 130, 20));
            Terminal.TextBox.CenterX = RESOLUTION_WIDTH / 2;
            Terminal.TextBox.Y = 4;
            hudSprites.Add(Terminal.TextBox);
            hudSprites.Add(timerSprite = new StringDrawable(8, RESOLUTION_HEIGHT - 12, FontTexture, "TEST", Color.White));

            JObject jObject = JObject.Parse(System.IO.File.ReadAllText("levels/roomtest"));
            LoadLevel(jObject);

            //JObject jObject = JObject.Parse(System.IO.File.ReadAllText("levels/roomtest"));
            //LoadLevel(jObject);
            //sprites.Add(ActivePlayer);
            ActivePlayer.Layer = 1;
            //ActivePlayer.Visible = false;
            //CurrentState = GameStates.Editing;
            tool = Tools.Tiles;
            //sprites.Remove(ActivePlayer);
            //RoomDatas[0] = CurrentRoom.Save();
            //Clipboard.SetText(RoomDatas[0].ToString());
            //sprites.Add(ActivePlayer);

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
                    sprites.Add(new Tile((int)(selection.X + cameraX), (int)(selection.Y + cameraY), currentTexture, currentTile.X, currentTile.Y));
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
            if (e.Control && e.KeyCode == Keys.C)
            {
                Clipboard.SetText(CurrentRoom.Save().ToString());
            }
            if (inputMap.ContainsKey(e.KeyCode) && !heldKeys.Contains(e.KeyCode))
            {
                inputs[(int)inputMap[e.KeyCode]]++;
                lastPressed[(int)inputMap[e.KeyCode]] = FrameCount;
            }
            heldKeys.Add(e.KeyCode);
            if (CurrentState == GameStates.Editing)
            {
                if (e.KeyCode == Keys.Right)
                {
                    LoadRoom((CurrentRoom.X + 1) % WidthRooms, CurrentRoom.Y);
                }
                else if (e.KeyCode == Keys.Left)
                {
                    int x = CurrentRoom.X - 1;
                    if (x < 0) x = WidthRooms - 1;
                    LoadRoom(x, CurrentRoom.Y);
                }
                else if (e.KeyCode == Keys.Down)
                {
                    LoadRoom(CurrentRoom.X, (CurrentRoom.Y + 1) % HeightRooms);
                }
                else if (e.KeyCode == Keys.Up)
                {
                    int y = CurrentRoom.Y - 1;
                    if (y < 0) y = HeightRooms - 1;
                    LoadRoom(CurrentRoom.X, y);
                }
                else if (e.KeyCode == Keys.S)
                {
                    currentTile.Y = (currentTile.Y + 1) % ((int)currentTexture.Height / currentTexture.TileSize);
                }
                else if (e.KeyCode == Keys.W)
                {
                    currentTile.Y -= 1;
                    if (currentTile.Y < 0) currentTile.Y += (int)currentTexture.Height / currentTexture.TileSize;
                }
                else if (e.KeyCode == Keys.D)
                {
                    currentTile.X = (currentTile.X + 1) % ((int)currentTexture.Width / currentTexture.TileSize);
                }
                else if (e.KeyCode == Keys.A)
                {
                    currentTile.X -= 1;
                    if (currentTile.X < 0) currentTile.X += (int)currentTexture.Width / currentTexture.TileSize;
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

                // begin frame
                if (CurrentState == GameStates.Playing)
                {
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

        private void HandleEditingInputs()
        {
            if (mouseX > -1 && mouseY > -1)
            {
                selection.Visible = true;
                selection.X = mouseX - mouseX % 8;
                selection.Y = mouseY - mouseY % 8;
            }
            else
            {
                selection.Visible = false;
            }
            if (leftMouse || middleMouse || rightMouse)
            {

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

        public void LoadRoom(int x, int y)
        {
            FocusedRoom = x + y * WidthRooms;
            if (!RoomDatas.ContainsKey(FocusedRoom))
            {
                Room r = new Room(new SpriteCollection(), Script.Empty, Script.Empty);
                r.X = x;
                r.Y = y;
                RoomDatas.Add(FocusedRoom, r.Save());
            }
            CurrentRoom = LoadRoom(RoomDatas[FocusedRoom]);
            cameraX = CurrentRoom.X * Room.ROOM_WIDTH;
            cameraY = CurrentRoom.Y * Room.ROOM_HEIGHT;
            CurrentRoom.Objects.Add(ActivePlayer);
            ActivePlayer.CenterX = ActivePlayer.CenterX % Room.ROOM_WIDTH + CurrentRoom.X * Room.ROOM_WIDTH;
            ActivePlayer.CenterY = ActivePlayer.CenterY % Room.ROOM_HEIGHT + CurrentRoom.Y * Room.ROOM_HEIGHT;
            //ActivePlayer.PreviousX = ActivePlayer.PreviousX % Room.ROOM_WIDTH + CurrentRoom.X * Room.ROOM_WIDTH;
            //ActivePlayer.PreviousY = ActivePlayer.PreviousY % Room.ROOM_HEIGHT + CurrentRoom.Y * Room.ROOM_HEIGHT;
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
            if (ActivePlayer.CenterX > (CurrentRoom.X + 1) * Room.ROOM_WIDTH)
                LoadRoom((CurrentRoom.X + 1) % WidthRooms, CurrentRoom.Y);
            else if (ActivePlayer.CenterX < CurrentRoom.X * Room.ROOM_WIDTH)
                LoadRoom((CurrentRoom.X + WidthRooms - 1) % WidthRooms, CurrentRoom.Y);
            if (ActivePlayer.CenterY > (CurrentRoom.Y + 1) * Room.ROOM_HEIGHT)
                LoadRoom(CurrentRoom.X, (CurrentRoom.Y + 1) % HeightRooms);
            else if (ActivePlayer.CenterY < CurrentRoom.Y * Room.ROOM_HEIGHT)
                LoadRoom(CurrentRoom.X, (CurrentRoom.Y + HeightRooms - 1) % HeightRooms);
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
                    if (c.CollidedWith.Solid == Sprite.SolidState.Entity || c.CollidedWith is GravityLine)
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
            jarr = (JObject[])RoomDatas.Values.ToArray();
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
                    Sprite s = LoadSprite(sprite);
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
                    RoomDatas.Add(id, room);
                }
            }
            //Load Scripts
            {
                for (int i = 0; i < Scripts.Count; i++)
                {
                    string name = Scripts.Keys[i];
                    string contents = scriptContents[name];
                    Scripts.Values[i].Commands = Command.ParseScript(this, contents).Commands;
                    Scripts.Values[i].Contents = contents;
                }
            }
            //Load Player
            {
                Crewman player = LoadSprite(loadFrom["Player"]) as Crewman;
                SetPlayer(player);
                ActivePlayer.X = startX;
                ActivePlayer.Y = startY;
                LoadRoom(startRoomX, startRoomY);
            }
        }

        public Room LoadRoom(JToken loadFrom)
        {
            JArray sArr = loadFrom["Objects"] as JArray;
            Room ret = new Room(new SpriteCollection(), null, null);
            ret.X = (int)loadFrom["X"];
            ret.Y = (int)loadFrom["Y"];
            foreach (JToken sprite in sArr)
            {
                Sprite s = LoadSprite(sprite);
                if (s != null)
                    ret.Objects.Add(s);
                if (s is Checkpoint && ActivePlayer.CurrentCheckpoint != null && s.X == ActivePlayer.CurrentCheckpoint.X && s.Y == ActivePlayer.CurrentCheckpoint.Y)
                {
                    (s as Checkpoint).Activate();
                }
            }
            ret.EnterScript = ScriptFromName((string)loadFrom["EnterScript"]) ?? Script.Empty;
            ret.ExitScript = ScriptFromName((string)loadFrom["ExitScript"]) ?? Script.Empty;
            return ret;
        }

        public Sprite LoadSprite(JToken loadFrom)
        {
            string type = (string)loadFrom["Type"];
            //Type t = typeof(Sprite);
            Sprite s;
            float x = (float)loadFrom["X"];
            float y = (float)loadFrom["Y"];
            string textureName = (string)loadFrom["Texture"];
            Texture texture = TextureFromName(textureName);
            if (type == "Tile")
            {
                int tileX = (int)loadFrom["TileX"];
                int tileY = (int)loadFrom["TileY"];
                s = new Tile((int)x, (int)y, texture, tileX, tileY);
            }
            else if (type == "Enemy")
            {
                string animationName = (string)loadFrom["Animation"];
                float xSpeed = (float)loadFrom["XSpeed"];
                float ySpeed = (float)loadFrom["YSpeed"];
                string name = (string)loadFrom["Name"];
                int color = (int)loadFrom["Color"];
                int boundX = (int)loadFrom["BoundsX"];
                int boundY = (int)loadFrom["BoundsY"];
                int boundW = (int)loadFrom["BoundsWidth"];
                int boundH = (int)loadFrom["BoundsHeight"];
                s = new Enemy(x, y, texture, texture.AnimationFromName(animationName), xSpeed, ySpeed, Color.FromArgb(color));
                s.Name = name;
                (s as Enemy).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
            }
            else if (type == "Crewman")
            {
                string standName = (string)loadFrom["Standing"];
                string walkName = (string)loadFrom["Walking"];
                string fallName = (string)loadFrom["Falling"];
                string jumpName = (string)loadFrom["Jumping"];
                string dieName = (string)loadFrom["Dying"];
                string name = (string)loadFrom["Name"];
                int textBoxColor = (int)loadFrom["TextBox"];
                bool sad = (bool)loadFrom["Sad"];
                float gravity = (float)loadFrom["Gravity"];
                bool flipX = (bool)loadFrom["FlipX"];
                s = new Crewman(x, y, texture, name, texture.AnimationFromName(standName), texture.AnimationFromName(walkName), texture.AnimationFromName(fallName), texture.AnimationFromName(jumpName), texture.AnimationFromName(dieName), Color.FromArgb(textBoxColor));
                (s as Crewman).Sad = sad;
                s.Gravity = gravity;
                s.FlipX = flipX;
            }
            else if (type == "Checkpoint")
            {
                string deactivatedName = (string)loadFrom["Deactivated"];
                string activatedName = (string)loadFrom["Activated"];
                bool flipX = (bool)loadFrom["FlipX"];
                bool flipY = (bool)loadFrom["FlipY"];
                s = new Checkpoint(x, y, texture, texture.AnimationFromName(deactivatedName), texture.AnimationFromName(activatedName), flipX, flipY);
            }
            else if (type == "Platform")
            {
                string animationName = (string)loadFrom["Animation"];
                string disappearName = (string)loadFrom["DisappearAnimation"];
                float xSpeed = (float)loadFrom["XSpeed"];
                float ySpeed = (float)loadFrom["YSpeed"];
                float conveyor = (float)loadFrom["Conveyor"];
                string name = (string)loadFrom["Name"];
                bool disappear = (bool)loadFrom["Disappear"];
                int color = (int)loadFrom["Color"];
                int boundX = (int)loadFrom["BoundsX"];
                int boundY = (int)loadFrom["BoundsY"];
                int boundW = (int)loadFrom["BoundsWidth"];
                int boundH = (int)loadFrom["BoundsHeight"];
                s = new Platform(x, y, texture, texture.AnimationFromName(animationName), xSpeed, ySpeed, conveyor, disappear, texture.AnimationFromName(disappearName));
                s.Name = name;
                s.Color = Color.FromArgb(color);
                (s as Platform).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
            }
            else if (type == "Terminal")
            {
                string deactivatedName = (string)loadFrom["Deactivated"];
                string activatedName = (string)loadFrom["Activated"];
                string script = (string)loadFrom["Script"];
                bool repeat = (bool)loadFrom["Repeat"];
                bool flipX = (bool)loadFrom["FlipX"];
                bool flipY = (bool)loadFrom["FlipY"];
                s = new Terminal(x, y, texture, texture.AnimationFromName(deactivatedName), texture.AnimationFromName(activatedName), ScriptFromName(script), repeat);
                s.FlipX = flipX;
                s.FlipY = flipY;
            }
            else if (type == "GravityLine")
            {
                int length = (int)loadFrom["Length"];
                bool horizontal = (bool)loadFrom["Horizontal"];
                string animationName = (string)loadFrom["Animation"];
                float xSpeed = (float)loadFrom["XSpeed"];
                float ySpeed = (float)loadFrom["YSpeed"];
                int boundX = (int)loadFrom["BoundsX"];
                int boundY = (int)loadFrom["BoundsY"];
                int boundW = (int)loadFrom["BoundsWidth"];
                int boundH = (int)loadFrom["BoundsHeight"];
                s = new GravityLine(x, y, texture, texture.AnimationFromName(animationName), horizontal, length);
                (s as GravityLine).XSpeed = xSpeed;
                (s as GravityLine).YSpeed = ySpeed;
                (s as GravityLine).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
            }

            else s = null;

            return s;
        }
    }
}
