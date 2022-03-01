#define TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Drawing;
using System.Diagnostics;

using OpenTK.Windowing.Desktop;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;

namespace V7
{
    public enum Pushability { PushSprite, Pushable, Solid, Immovable }
    public class Game
    {
        public EventHandler QuitGame;

        // Input
        #region Input
        public enum Inputs
        {
            Left,
            Right,
            Special,
            Jump,
            Pause,
            Kill,
            Escape,
            Up,
            Down,
            Count
        }
        public static readonly SortedList<string, Inputs> InputNames = new SortedList<string, Inputs>()
        {
            { "jump", Inputs.Jump }, { "action", Inputs.Jump },
            { "up", Inputs.Up },
            { "down", Inputs.Down },
            { "left", Inputs.Left },
            { "right", Inputs.Right },
            { "special", Inputs.Special }, { "item", Inputs.Special },
            { "pause", Inputs.Pause }, { "enter", Inputs.Pause },
            { "kill", Inputs.Kill }, { "reset", Inputs.Kill },
            { "escape", Inputs.Escape }, { "exit", Inputs.Escape }
        };
        private int[] inputs = new int[(int)Inputs.Count];
        private List<Inputs> bufferInputs = new List<Inputs>();
        private List<PassedKeyEvent> bufferKeys = new List<PassedKeyEvent>();
        private string keys = "";
        private int[] lastPressed = new int[(int)Inputs.Count];
        public static readonly Keys[] KeyIndexes = new Keys[] {
             Keys.Escape,  Keys.F1,  Keys.F2,  Keys.F3,  Keys.F4,  Keys.F5,  Keys.F6,  Keys.F7,  Keys.F8,  Keys.F9,  Keys.F10,  Keys.F11,  Keys.F12,  Keys.Pause,  Keys.Insert,  Keys.Delete,
             Keys.GraveAccent, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0, Keys.Minus, Keys.Equal, Keys.Backspace,
             Keys.Tab, Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P, Keys.LeftBracket, Keys.RightBracket, Keys.Backslash,
             Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L, Keys.Semicolon, Keys.Apostrophe, Keys.Enter,
             Keys.LeftShift, Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M, Keys.Comma, Keys.Period, Keys.Slash, Keys.Up, Keys.RightShift,
             Keys.LeftControl, Keys.LeftAlt, Keys.Space, Keys.RightAlt, Keys.RightControl, Keys.Left, Keys.Down, Keys.Right
        };
        public Dictionary<Keys, Inputs> inputMap = new Dictionary<Keys, Inputs>() {
            { Keys.Left, Inputs.Left }, { Keys.A, Inputs.Left },
            { Keys.Right, Inputs.Right }, { Keys.D, Inputs.Right },
            { Keys.Up, Inputs.Up }, { Keys.W, Inputs.Up },
            { Keys.Down, Inputs.Down }, { Keys.S, Inputs.Down },
            //{ Keys.Up, Inputs.Jump }, { Keys.Down, Inputs.Jump }, 
            { Keys.Space, Inputs.Jump }, { Keys.Z, Inputs.Jump }, { Keys.V, Inputs.Jump },
            //{ Keys.W, Inputs.Jump }, { Keys.S, Inputs.Jump },
            { Keys.Enter, Inputs.Pause },
            { Keys.Escape, Inputs.Escape },
            { Keys.R, Inputs.Kill },
            { Keys.X, Inputs.Special }, { Keys.B, Inputs.Special }, { Keys.RightShift, Inputs.Special }
        };
        private SortedSet<Keys> heldKeys = new SortedSet<Keys>();
        public int MouseX { get; private set; } = -1;
        public int MouseY { get; private set; } = -1;
        private bool bufferMove;
        public bool JustMoved { get; private set; }
        public bool MouseIn { get; private set; } = false;
        public bool LeftMouse { get; private set; } = false;
        public bool RightMouse { get; private set; } = false;
        public bool MiddleMouse { get; private set; } = false;
        public void ReleaseLeftMouse() => LeftMouse = false;
        private int control = 0;
        public bool Control => control > 0;
        private int shift = 0;
        public bool Shift => shift > 0;
        private int alt = 0;
        public bool Alt => alt > 0;
        #endregion

        private bool isLoading = true;
        private double percent;
        private StringDrawable loadingSprite;
        private bool isInitialized = false;

        private int _fps = 60;
        private int fps {
            get => _fps;
            set
            {
                _fps = value;
                ticksPerFrame = Stopwatch.Frequency / _fps;
            }
        }
        long ticksPerFrame = Stopwatch.Frequency / 60;

        bool SidewaysControls = true;
        bool UsingUpDown => ActivePlayer is object && ActivePlayer.Sideways && SidewaysControls;
        public bool IsJump(Inputs input)
        {
            return (!UsingUpDown && (input == Inputs.Up || input == Inputs.Down)) || (UsingUpDown && (input == Inputs.Right || input == Inputs.Left));
        }

        public bool IsInputActive(Inputs input)
        {
            return inputs[(int)input] != 0;
        }
        public bool IsKeyHeld(Keys key) => heldKeys.Contains(key);
        private bool IsInputNew(Inputs input)
        {
            return lastPressed[(int)input] == FrameCount;
        }
        private bool ignoreAction = false;

        public static TextCopy.Clipboard Clipboard = new TextCopy.Clipboard();

        // Scripts
        #region Scripts
        public SortedList<string, Script> Scripts = new SortedList<string, Script>();
        public Script ScriptFromName(string name, bool createNew = false)
        {
            if (!Scripts.TryGetValue(name ?? "", out Script script) && createNew && !string.IsNullOrEmpty(name))
            {
                script = new Script(new Command[] { }, name, "");
                Scripts.Add(name, script);
            }
            return script;
        }

        public bool PlayerControl = true;
        public enum FreezeOptions { FreezeScreen, OnlySprites, OnlyMovement, Unfrozen, Paused }
        public FreezeOptions Freeze = FreezeOptions.Unfrozen;
        public bool WaitingForAction;
        public List<Script.Executor> CurrentScripts = new List<Script.Executor>();
        public List<Script.Executor> PauseScripts = new List<Script.Executor>();
        public List<VTextBox> TextBoxes = new List<VTextBox>();
        private static Random r = new Random();
        public SortedList<string, Variable> Vars = new SortedList<string, Variable>();
        public Script PauseScript;

        public JObject ScriptInfo;

        List<RoomGroup> toLoad = new List<RoomGroup>();
        Task loadingTask;
        #endregion

        // OpenGL
        private GameWindow gameWindow;
        public TextureProgram ProgramID { get; private set; }
        //private uint fbo;
        private Color currentColor;

        // Textures
        #region Textures
        public FontTexture FontTexture;
        public FontTexture NonMonoFont;
        public FontTexture TinyFont;
        public Texture BoxTexture;
        public SortedList<string, Texture> Textures;
        public Texture TextureFromName(string name)
        {
            if (Textures.TryGetValue(name ?? "", out Texture ret))
                return ret;
            else if (int.TryParse(name ?? "", out int index) && index > -1 && index < Textures.Count)
                return Textures.Values[index];
            else
                return null;
        }
        public SortedList<string, AutoTileSettings.PresetGroup> RoomPresets = new SortedList<string, AutoTileSettings.PresetGroup>();
        #endregion

        // Sounds
        #region Sounds
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
                return Music.Empty;
        }
        public Music CurrentSong;
        public Music LevelMusic;
        public Action MusicFaded;
        #endregion

        //Map
        public SortedList<int, IMapSprite> MapSprites = new SortedList<int, IMapSprite>();
        public Texture MapTexture { get; set; }
        public enum MapAnimations { None = 0, Enter = 1, Fade = 2, Random = 4, Up = 8, Down = 16, Left = 32, Right = 64 }
        public MapAnimations MapAnimation;

        // Editing
        #region Editing
        bool typing = false;
        public StringDrawable TypingTo { get; private set; }
        bool singleLine = false;
        private Action<string> textChanged = null;
        private Action<bool, string> FinishTyping = null;
        public LevelEditor Editor;
        #endregion

        // Textures/Animations
        #region EditorTexAnim
        #endregion
        public StringHighlighter ScriptEditor;

        // Dialog
        public static SortedDictionary<string, Color> colors = new SortedDictionary<string, Color>()
        {
            { "viridian", Color.FromArgb(255, 164, 164, 255) }, { "vermilion", Color.FromArgb(255, 255, 60, 60) },
            { "vitellary", Color.FromArgb(255, 255, 255, 134) }, { "verdigris", Color.FromArgb(255, 144, 255, 144) },
            { "victoria", Color.FromArgb(255, 95, 95, 255) }, { "violet", Color.FromArgb(255, 255, 134, 255) },
            { "valerie", Color.FromArgb(255, 229, 226, 224) }, { "stigma", Color.FromArgb(255, 195, 0, 0) },
            { "terminal", Color.FromArgb(255, 174, 174, 174) }, { "white", Color.White }, { "red", Color.Red },
            { "orange", Color.FromArgb(255, 255, 128, 0) }, { "yellow", Color.Yellow }, { "green", Color.Lime },
            { "blue", Color.Blue }, { "purple", Color.Purple }, { "gray", Color.Gray }, { "black", Color.Black },
            { "brown", Color.SaddleBrown }, { "pink", Color.Pink }, { "magenta", Color.Magenta }, { "cyan", Color.Cyan }
        };

        // Rooms
        public Room CurrentRoom;
        public SortedList<int, JObject> RoomDatas = new SortedList<int, JObject>();
        public int FocusedRoom;
        public int WidthRooms;
        public int HeightRooms;
        public int OffsetXRooms;
        public int OffsetYRooms;
        public SortedList<int, RoomGroup> RoomGroups = new SortedList<int, RoomGroup>();
        public List<RoomGroup> GroupList = new List<RoomGroup>();

        public int StartX;
        public int StartY;
        public int StartRoomX;
        public int StartRoomY;

        public StringDrawable RoomName;
        public RectangleSprite RoomNameBar;

        public SortedSet<Point> ExploredRooms;

        private bool exitCollisions = false;

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
        public Matrix4 Camera { get; private set; }
        public Matrix4 hudView;
        public void SetView(ref Matrix4 view)
        {
            GL.UniformMatrix4(ProgramID.ViewMatrixLocation, false, ref view);
        }
        public float CameraX;
        public float CameraY;
        public float AutoScrollX;
        public float AutoScrollY;
        public float StopScrollX;
        public float StopScrollY;
        public bool AutoScroll;
        public float MaxScrollX;
        public float MaxScrollY;
        public float MinScrollX;
        public float MinScrollY;

        // Screen Effects
        private int flashFrames;
        private bool isFlashing = false;
        private Color flashColour = Color.White;
        private int shakeFrames;
        private int shakeIntensity;
        public bool DoScreenEffects = false;

        // Const Sizes
        public const int RESOLUTION_WIDTH = 320;
        public const int RESOLUTION_HEIGHT = 240;
        public const int HUD_LEFT = 48;
        public const int HUD_TOP = 24;

        // Extra HUD
        int hudLeft = HUD_LEFT;
        int hudTop = HUD_TOP;
        public bool EnableExtraHud
        {
            get => hudLeft == HUD_LEFT;
            set
            {
                if (value)
                {
                    hudLeft = HUD_LEFT;
                    hudTop = HUD_TOP;
                    GL.Enable(EnableCap.ScissorTest);
                }
                else
                {
                    hudLeft = hudTop = 0;
                    GL.Disable(EnableCap.ScissorTest);
                }
                SetCamera();
            }
        }

        // Lights
        public float MainLight = 1.0f;
        public float[] Lights = new float[60];
        public int LightCount;
        public Vector3 GetLight(int index) => (index > -1 && index < 20) ? new Vector3(Lights[index * 3], Lights[index * 3 + 1], Lights[index * 3 + 2]) : new Vector3();
        public void AddLight(float x, float y, float radius)
        {
            if (LightCount >= 20)
            {
                LightCount = 20;
                RemoveLight(0);
            }
            Lights[LightCount * 3] = x;
            Lights[LightCount * 3 + 1] = y;
            Lights[LightCount * 3 + 2] = radius;
            LightCount += 1;
        }
        public void SetLight(int index, float x, float y, float radius)
        {
            if (index > -1 && index < LightCount)
            {
                Lights[index * 3] = x;
                Lights[index * 3 + 1] = y;
                Lights[index * 3 + 2] = radius;
            }
        }
        public void RemoveLight(int index)
        {
            if (index < LightCount && index > -1)
            {
                for (int i = index; i < LightCount; i++)
                {
                    if (i < 20)
                    {
                        Lights[i * 3] = Lights[(i + 1) * 3];
                        Lights[i * 3 + 1] = Lights[(i + 1) * 3 + 1];
                        Lights[i * 3 + 2] = Lights[(i + 1) * 3 + 2];
                    }
                    else
                    {
                        Lights[i * 3] = 0;
                        Lights[i * 3 + 1] = 0;
                        Lights[i * 3 + 2] = 0;
                    }
                }
            }
            LightCount -= 1;
        }

        // Sprite Collections
        public SpriteCollection sprites => CurrentRoom?.Objects;
        public SpriteCollection hudSprites;
        public SortedList<string, StringDrawable> hudText = new SortedList<string, StringDrawable>();
        public SortedList<string, Sprite> hudSpritesUser = new SortedList<string, Sprite>();
        public BGSpriteCollection BGSprites;
        public SortedList<string, Sprite> UserAccessSprites = new SortedList<string, Sprite>();

        private JObject menuBG;
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
            if (ActivePlayer is object)
                ActivePlayer.Respawned += RespawnPlatforms;
        }
        public void RespawnPlatforms()
        {
            if (ActivePlayer.CheckpointX > CurrentRoom.Right || ActivePlayer.CheckpointX < CurrentRoom.GetX ||
                ActivePlayer.CheckpointY > CurrentRoom.Bottom || ActivePlayer.CheckpointY < CurrentRoom.GetY)
            {
                int rx = (int)Math.Floor(ActivePlayer.CheckpointX / Room.ROOM_WIDTH);
                int ry = (int)Math.Floor(ActivePlayer.CheckpointY / Room.ROOM_HEIGHT);
                LoadRoom(rx, ry);
            }
            else
            {
                if (CurrentRoom.IsGroup && AutoScroll)
                {
                    PointF p = GetCameraTarget();
                    if (AutoScrollX != 0)
                        CameraX = p.X;
                    if (AutoScrollY != 0)
                        CameraY = p.Y;
                }
                foreach (Sprite sprite in sprites.ToProcess)
                {
                    if (!(sprite is Platform)) continue;
                    Platform platform = sprite as Platform;
                    if (!platform.Visible && platform.Animation == platform.DisappearAnimation)
                    {
                        platform.Reappear();
                    }
                }
            }
        }
        public bool IsPlaying { get; private set; } = false;
        public int FrameCount = 1; // start at 1 so inputs aren't "new" at start
        public enum GameStates { Playing, Editing }
        public GameStates CurrentState = GameStates.Playing;
        private string currentLevelPath;
        public List<int> CollectedTrinkets = new List<int>();
        public SortedList<int, int> LevelTrinkets = new SortedList<int, int>();
        public bool LoseTrinkets = false;
        public SortedList<string, JToken> Backgrounds { get; set; } = new SortedList<string, JToken>();
        public JToken GetBackground(string name)
        {
            if (Backgrounds.TryGetValue(name ?? "", out JToken ret))
                return ret;
            else
                return null;
        }
        public Script OnPlayerDeath;
        public Script OnPlayerRespawn;
        public List<ActivityZone> ActivityZones = new List<ActivityZone>();
        private IActivityZone CurrentActivityZone;

        public SortedList<int, WarpToken.WarpData> Warps;
        public int GetNextWarpID()
        {
            int ret = 0;
            while (Warps.Count > ret && Warps.Keys[ret] == ret)
                ret++;
            return ret;
        }


        public int FadeSpeed;
        public float FadePos = 255;
        private bool fadeHud = false;

        private string levelLoadProgress = "Loading Level...";
        private bool isLoadingLevel = false;
        private StringDrawable levelLoadSprite;

        public RectangleSprite CutsceneBarTop;
        public RectangleSprite CutsceneBarBottom;
        public int CutsceneBars = 0;

        private List<SpritesLayer> Layers = new List<SpritesLayer>();
        private int StartProcessing;
        private int StartDrawing;
        private int GiveInput;
        public void AddLayer(SpritesLayer layer)
        {
            if (layer.Darken >= 1)
                StartDrawing = Layers.Count;
            if (layer.FreezeBelow)
                StartProcessing = Layers.Count;
            Layers.Add(layer);
            GiveInput = Layers.Count - 1;
            layer.Exit += ExitLayer;
            layer.Finish += RemoveLayer;
        }
        public void ExitLayer(SpritesLayer layer)
        {
            if (layer != Layers.Last()) return;
            layer.FreezeBelow = false;
            GiveInput -= 1;
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                if (Layers[i].FreezeBelow)
                {
                    StartProcessing = i;
                    break;
                }
                else if (i == 0)
                    StartProcessing = 0;
            }
        }
        public void RemoveLayer(SpritesLayer layer) => RemoveLayer(layer, true);
        public void RemoveLayer(SpritesLayer layer, bool dispose)
        {
            if (layer != Layers.LastOrDefault())
            {
                int index = Layers.IndexOf(layer);
                if (index > -1)
                {
                    Layers.RemoveAt(index);
                    for (int i = 0; i < Layers.Count; i++)
                    {
                        SpritesLayer sl = Layers[i];
                        if (!sl.YieldInput)
                            GiveInput = i;
                        if (sl.Darken >= 1f)
                            StartDrawing = i;
                        if (sl.FreezeBelow)
                            StartProcessing = i;
                    }
                }
                return;
            }
            Layers.RemoveAt(Layers.Count - 1);
            StartProcessing = 0;
            GiveInput = 0;
            StartDrawing = 0;
            for (int i = 0; i < Layers.Count; i++)
            {
                SpritesLayer sl = Layers[i];
                if (!sl.YieldInput)
                    GiveInput = i;
                if (sl.Darken >= 1f)
                    StartDrawing = i;
                if (sl.FreezeBelow)
                    StartProcessing = i;
            }
            if (dispose)
                layer.Dispose();
        }

        // Menu
        public List<VMenuItem> MenuItems;
        public int SelectedItem = 0;
        public List<StringDrawable> ItemSprites = new List<StringDrawable>();
        public Color MenuColor = Color.White;
        public Color[] MenuColors = new Color[] { Color.Cyan, Color.FromArgb(255, 160, 30, 255), Color.FromArgb(255, 255, 30, 255), Color.FromArgb(255, 255, 50, 50), Color.Yellow, Color.Lime };
        public float MaxMenuWidth = 0;
        public RectangleSprite ItemSelector;
        private Action WhenFaded;
        private int escapeItem;

        private StringDrawable levelName;
        private StringDrawable levelAuthor;
        private StringDrawable levelSubtitle;
        private StringDrawable levelDesc;
        private bool isPlayerLevels = false;
        private int page = 0;
        private string[] playerLevels;

        //Context Menu
        private bool previewContextMenu;
        private string contextPreviewIndex;
        private List<VMenuItem> contextMenuItems = new List<VMenuItem>();

        public delegate void SendLog(string message);
        public event SendLog Log;
        public void LogMessage(string message) => Log(message);

        public Exception Exception;

        private Stopwatch frameTimer = new Stopwatch();
        private double tickTime;

        public Game()
        {
#if RELEASE
            try
            {
#endif
            if (System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\V7"))
                System.IO.Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\V7");
            else if (System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VVVVVVV"))
                System.IO.Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VVVVVVV");
            GameWindowSettings gws = GameWindowSettings.Default;
            gws.UpdateFrequency = 60;
            gws.RenderFrequency = 60;
            NativeWindowSettings nws = NativeWindowSettings.Default;
            gameWindow = new GameWindow(gws, NativeWindowSettings.Default)
            {
                Title = "VVVVVVV"
            };
            gameWindow.Size = new Vector2i((RESOLUTION_WIDTH + hudLeft) * 2 + gameWindow.Size.X - gameWindow.ClientSize.X, (RESOLUTION_HEIGHT + hudTop) * 2 + gameWindow.Size.Y - gameWindow.ClientSize.Y);
            gameWindow.Closing += (e) => StopGame();
            InitGlProgram();
            InitOpenGLSettings();
            Textures = new SortedList<string, Texture>();
            LoadAllTextures();

            FontTexture = TextureFromName("font") as FontTexture;
            NonMonoFont = TextureFromName("font2") as FontTexture;
            TinyFont = TextureFromName("font3") as FontTexture;
            BoxTexture = TextureFromName("box");
            loadingSprite = new StringDrawable(4, 4, FontTexture, "Loading...");
            hudSprites = new SpriteCollection();
            hudSprites.Add(loadingSprite);
            Warps = new SortedList<int, WarpToken.WarpData>();

            if (System.IO.File.Exists("scripts.txt"))
                ScriptInfo = JObject.Parse(System.IO.File.ReadAllText("scripts.txt"));

            gameWindow.UpdateFrame += GameWindow_UpdateFrame;
            gameWindow.RenderFrame += glControl_Render;
            gameWindow.Resize += glControl_Resize;
            gameWindow.KeyDown += GlControl_KeyDown;
            gameWindow.KeyUp += GlControl_KeyUp;
            gameWindow.MouseMove += GlControl_MouseMove;
            gameWindow.MouseDown += GlControl_MouseDown;
            gameWindow.MouseUp += GlControl_MouseUp;
            gameWindow.MouseLeave += GlControl_MouseLeave;
            gameWindow.TextInput += GlControl_KeyPress;
            gameWindow.MouseWheel += GlControl_MouseWheel;
            mwo = gameWindow.MouseState.Scroll.Y;
            Thread loadThread = new Thread(StartLoading);
            loadThread.Start();
#if RELEASE
            }
            catch (Exception e)
            {
                Exception = e;
                if (isEditor)
                {
                    //JObject lv = SaveLevel();
                    //int n = 1;
                    //while (System.IO.File.Exists("/levels/backup_" + n.ToString()))
                    //    n++;
                    //System.IO.File.WriteAllText("/levels/backup_" + n.ToString(), Newtonsoft.Json.JsonConvert.SerializeObject(lv, Newtonsoft.Json.Formatting.None));
                }
                gameWindow.Close();
            }
#endif
        }

        public void StartGame()
        {
            if (IsPlaying) return;
            IsPlaying = true;
            gameWindow.Run();
        }

        private void GameWindow_UpdateFrame(FrameEventArgs e)
        {
            // Start frame timer
            Stopwatch t = new Stopwatch();
            t.Start();

            // Get tick time
            tickTime = e.Time;
            frameTimer.Restart();

            // Handle key presses
            while (bufferKeys.Count > 0)
            {
                HandleKey(bufferKeys[0]);
                bufferKeys.RemoveAt(0);
            }
            // Handle typing
            if (keys.Length > 0)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    KeyPress(keys[i]);
                }
                keys = "";
            }
            // Handle input values
            for (int i = 0; i < bufferInputs.Count; i++)
            {
                inputs[(int)bufferInputs[i]]++;
                lastPressed[(int)bufferInputs[i]] = FrameCount;
            }
            bufferInputs.Clear();
            JustMoved = false;
            if (bufferMove) JustMoved = true;
            bufferMove = false;

            // Handle mouse wheel
            float delta = (int)gameWindow.MouseState.Scroll.Y - (int)mwo;
            mwo = gameWindow.MouseState.Scroll.Y;
            if (delta != 0 && Layers.Count > 0 && GiveInput > -1 && GiveInput < Layers.Count)
            {
                Layers[GiveInput].HandleWheel((int)delta);
            }

            // begin frame
            if (flashFrames > 0)
                flashFrames -= 1;
            if (shakeFrames > 0)
                shakeFrames -= 1;
            if (!isLoading && isInitialized)
            {
                if (!isLoadingLevel)
                {
                    exitCollisions = false;
                    if (Layers.Count == 0 || !Layers[StartProcessing].FreezeBelow)
                    {
                        if (CurrentState == GameStates.Playing)
                        {
                            if (Freeze != FreezeOptions.Paused)
                            {
                                HandleUserInputs();

                                if (Freeze != FreezeOptions.OnlySprites && CurrentState == GameStates.Playing)
                                    ProcessWorld();

                                BGSprites.Process();
                                for (int i = 0; i < CurrentScripts.Count; i++)
                                {
                                    Script.Executor script = CurrentScripts[i];
                                    script.Process();
                                }
                                if (CutsceneBars > 0)
                                {
                                    CutsceneBarBottom.Visible = CutsceneBarTop.Visible = true;
                                    CutsceneBarTop.X += 8;
                                    CutsceneBarBottom.X -= 8;
                                    if (CutsceneBarTop.X >= 0)
                                    {
                                        CutsceneBarTop.X = 0;
                                        CutsceneBarBottom.X = 0;
                                        CutsceneBars = 0;
                                    }
                                }
                                else if (CutsceneBars < 0)
                                {
                                    CutsceneBarTop.X -= 8;
                                    CutsceneBarBottom.X += 8;
                                    if (CutsceneBarTop.X <= -RESOLUTION_WIDTH)
                                    {
                                        CutsceneBarTop.X = -RESOLUTION_WIDTH;
                                        CutsceneBarBottom.X = RESOLUTION_WIDTH;
                                        CutsceneBars = 0;
                                        CutsceneBarBottom.Visible = CutsceneBarTop.Visible = false;
                                    }
                                }
                            }
                            else if (Freeze == FreezeOptions.Paused)
                            {
                                for (int i = 0; i < PauseScripts.Count; i++)
                                {
                                    Script.Executor script = PauseScripts[i];
                                    script.Process();
                                }
                                if (PauseScripts.Count == 0 && (IsInputNew(Inputs.Pause) || IsInputNew(Inputs.Escape)))
                                {
                                    if (Layers.LastOrDefault() is MapLayer)
                                        HideMap();
                                    Freeze = FreezeOptions.Unfrozen;
                                }
                            }
                        }
                    }
                    if (Layers.Count > 0)
                    {
                        for (int i = 0; i < Layers.Count; i++)
                        {
                            SpritesLayer layer = Layers[i];
                            if (!layer.ProcessAnyway && i < StartProcessing) continue;
                            layer.Process();
                            if (layer.Finished && Layers.ElementAtOrDefault(i) != layer)
                                i--;
                        }
                    }
                    CurrentSong.Process();
                    if (CurrentSong.IsFaded && MusicFaded is object)
                    {
                        Action m = MusicFaded;
                        MusicFaded = null;
                        m();
                    }
                    //if (StartProcessing == -1 || Layers.Count == 0)
                    {
                        for (int i = hudSprites.Count - 1; i >= 0; i--)
                        {
                            Sprite d = hudSprites[i];
                            d.Process();
                        }
                    }
                    if (FadeSpeed != 0)
                        FadePos += FadeSpeed;
                    if (FadeSpeed > 0 && FadePos >= 255)
                    {
                        FadePos = 255;
                        FadeSpeed = 0;
                        WhenFaded?.Invoke();
                    }
                    else if (FadeSpeed < 0 && FadePos <= 0)
                    {
                        FadePos = 0;
                        FadeSpeed = 0;
                        WhenFaded?.Invoke();
                    }
                    sprites?.CheckBuffer();
                }
                else
                {
                    CurrentSong.Process();
                }
            }

            // end frame
            FrameCount %= int.MaxValue;
            FrameCount++;

            float ms = (float)t.ElapsedTicks / Stopwatch.Frequency * 1000f; ;
            ftTotal += ms;
            ftTotal -= frameTimes[FrameCount % 60];
            frameTimes[FrameCount % 60] = ms;

            if (isLoading)
            {
                loadingSprite.Text = "Loading... " + ((int)percent).ToString() + "%";
            }
        }

        private void StartLoading()
        {
            InitSounds();
            InitMusic();
            //TileTexture tiles = TextureFromName("tiles") as TileTexture;
            //Texture platforms = TextureFromName("platforms");
            //Texture sprites32 = TextureFromName("sprites32");
            //Texture enemies = TextureFromName("enemies");
            Texture background = TextureFromName("background");
            BGSprites = new BGSpriteCollection(background, this);
            BGSprites.Scatter(200, background.AnimationFromName("SmallParticle"), 0.065f, 0);
            BGSprites.Scatter(200, background.AnimationFromName("Particle"), 0.1f, 1);
            BGSprites.BaseColor = Color.Gray;
            menuBG = BGSprites.Save();

            ExploredRooms = new SortedSet<Point>(SpriteCollection.pointComparer);

            Crewman.Flip1 = GetSound("jump");
            Crewman.Flip2 = GetSound("jump2");
            Crewman.Cry = GetSound("hurt");
            Platform.DisappearSound = GetSound("vanish");
            Checkpoint.ActivateSound = GetSound("save");
            Terminal.ActivateSound = GetSound("terminal");
            WarpToken.WarpSound = GetSound("teleport");
            GravityLine.Sound = GetSound("blip");

            // Editor prep

            Terminal.TextBox = new VTextBox(0, 0, FontTexture, " Press ENTER to activate terminal ", Color.FromArgb(255, 255, 130, 20))
            {
                CenterX = RESOLUTION_WIDTH / 2,
                Y = 4
            };
            Lever.TextBox = new VTextBox(0, 0, FontTexture, " Press ENTER to flip lever ", Color.FromArgb(255, 255, 130, 20))
            {
                CenterX = RESOLUTION_WIDTH / 2,
                Y = 4
            };
            hudSprites.Add(Terminal.TextBox);
            hudSprites.Add(timerSprite = new StringDrawable(8, RESOLUTION_HEIGHT - 20, TextureFromName("font2") as FontTexture, "TEST", Color.White));

            RoomName = new StringDrawable(0, 0, FontTexture, "", Color.White)
            {
                Layer = 2
            };
            RoomNameBar = new RectangleSprite(0, RESOLUTION_HEIGHT - 10, RESOLUTION_WIDTH, 10)
            {
                Color = Color.Black,
                Layer = 1
            };

            CutsceneBarTop = new RectangleSprite(-RESOLUTION_WIDTH, 0, RESOLUTION_WIDTH, 16);
            CutsceneBarBottom = new RectangleSprite(RESOLUTION_WIDTH, RESOLUTION_HEIGHT - 16, RESOLUTION_WIDTH, 16);
            CutsceneBarTop.Layer = 5;
            CutsceneBarBottom.Layer = 5;
            CutsceneBarBottom.Visible = false;
            CutsceneBarTop.Visible = false;
            CutsceneBarTop.Color = CutsceneBarBottom.Color = Color.Black;
            hudSprites.Add(CutsceneBarTop);
            hudSprites.Add(CutsceneBarBottom);
            //CurrentState = GameStates.Menu;
            ItemSelector = new RectangleSprite(0, 0, 1, 1);
            ItemSelector.Layer = int.MaxValue - 1;
            percent = 100d;
            loadingSprite.Text = "Press Action Button (Z, V, or Space)";
            isLoading = false;

        }

        private void GlControl_KeyPress(TextInputEventArgs e)
        {
            if (typing)
                keys += e.AsString;
        }
        private void KeyPress(char c)
        {
            switch ((int)c)
            {
                case 1:
                    TypingTo.SelectionStart = 0;
                    TypingTo.SelectionLength = TypingTo.Text.Length;
                    TypingTo.Text = TypingTo.Text;
                    break;
                case 3:
                    if (TypingTo.SelectionLength > 0 && TypingTo.SelectionStart > -1)
                    {
                        string copy = TypingTo.Text.Substring(TypingTo.SelectionStart, TypingTo.SelectionLength);
                        Clipboard.SetText(copy);
                    }
                    break;
                case 8:
                    {
                        if (TypingTo.SelectionStart > -1)
                        {
                            if (TypingTo.SelectionLength > 0)
                            {
                                int selL = TypingTo.SelectionLength;
                                TypingTo.SelectionLength = 0;
                                TypingTo.SelectingFromLeft = true;
                                TypingTo.Text = TypingTo.Text.Remove(TypingTo.SelectionStart, selL);
                                textChanged?.Invoke(TypingTo.Text);
                            }
                            else if (TypingTo.SelectionStart > 0)
                            {
                                int index = TypingTo.SelectionStart - 1;
                                int count = 1;
                                if (control > 0)
                                {
                                    index = TypingTo.GetCtrlIndex();
                                    count = TypingTo.SelectionStart - index;
                                }
                                TypingTo.SelectionStart = index;
                                TypingTo.Text = TypingTo.Text.Remove(TypingTo.SelectionStart, count);
                                textChanged?.Invoke(TypingTo.Text);
                            }
                        }
                    }
                    break;
                case 9:
                    //Do nothing
                    break;
                case 10:
                case 13:
                    if (!singleLine)
                    {
                        TypeText("\n");
                        textChanged?.Invoke(TypingTo.Text);
                    }
                    else
                    {
                        string s = TypingTo?.Text;
                        EscapeTyping();
                        ignoreAction = true;
                        FinishTyping?.Invoke(true, s);
                        return;
                    }
                    break;
                case 22:
                    {
                        string paste = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(paste))
                        {
                            TypeText(paste);
                        }
                    }
                    break;
                case 24:
                    if (TypingTo.SelectionLength > 0)
                    {
                        string copy = TypingTo.Text.Substring(TypingTo.SelectionStart, TypingTo.SelectionLength);
                        Clipboard.SetText(copy);
                        int selL = TypingTo.SelectionLength;
                        TypingTo.SelectionLength = 0;
                        TypingTo.SelectingFromLeft = true;
                        TypingTo.Text = TypingTo.Text.Remove(TypingTo.SelectionStart, selL);
                        textChanged?.Invoke(TypingTo.Text);
                    }
                    break;
                case 27:
                    {
                        string s = TypingTo?.Text;
                        EscapeTyping();
                        FinishTyping?.Invoke(false, s);
                        break;
                    }
                default:
                    TypeText(c.ToString());
                    textChanged?.Invoke(TypingTo.Text);
                    break;
            }
        }

        public void EscapeTyping()
        {
            TypingTo.SelectionStart = -1;
            TypingTo.SelectionLength = 0;
            TypingTo.SelectingFromLeft = true;
            TypingTo.Text = TypingTo.Text;
            TypingTo = null;
            typing = false;
            textChanged = null;
        }

        public Color? GetColor(string color, Sprite sender = null, Sprite target = null)
        {
            Color? ret = null;
            string s = color.Replace("#", "").Replace(" ", "").ToLower();
            Crewman c = SpriteFromName(color) as Crewman;
            if (sender is Crewman && (s == "self" || s == "this"))
            {
                ret = (sender as Crewman).TextBoxColor;
            }
            else if (target is Crewman && s == "target")
            {
                ret = (target as Crewman).TextBoxColor;
            }
            else if (s == "player")
            {
                ret = ActivePlayer?.TextBoxColor ?? Color.White;
            }
            else if (c is object)
            {
                ret = c.TextBoxColor;
            }
            else if (colors.ContainsKey(s))
            {
                ret = colors[s];
            }
            else
            {
                if (int.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int val))
                {
                    Color cl = Color.FromArgb(val);
                    ret = cl;
                }
            }
            return ret;
        }

        private void TypeText(string s)
        {
            if (TypingTo.SelectionStart > -1 && TypingTo.SelectionStart <= TypingTo.Text.Length)
            {
                if (TypingTo.SelectionLength > 0)
                {
                    TypingTo.Text = TypingTo.Text.Remove(TypingTo.SelectionStart, TypingTo.SelectionLength);
                    TypingTo.SelectionLength = 0;
                }
                int ss = TypingTo.SelectionStart;
                TypingTo.SelectionStart += s.Length;
                TypingTo.Text = TypingTo.Text.Insert(ss, s);
            }
            else
            {
                TypingTo.SelectionStart += s.Length;
                TypingTo.Text += s;
            }
        }

        private void GlControl_MouseLeave()
        {
            MouseIn = false;
        }

        private void GlControl_MouseUp(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                LeftMouse = false;
            else if (e.Button == MouseButton.Right)
                RightMouse = false;
            else if (e.Button == MouseButton.Middle)
                MiddleMouse = false;
        }

        private void GlControl_MouseDown(MouseButtonEventArgs e)
        {
            bool giveInput = Layers.Count > 0 && GiveInput > -1 && GiveInput < Layers.Count;
            if (e.Button == MouseButton.Left)
            {
                LeftMouse = true;
                if (!giveInput)
                    TriggerLeftClick();
            }
            else if (e.Button == MouseButton.Right)
            {
                RightMouse = true;
            }
            else if (e.Button == MouseButton.Middle)
                MiddleMouse = true;

            if (giveInput)
            {
                Layers[GiveInput].HandleClick(e);
                return;
            }
        }

        private void TriggerLeftClick()
        {
            if (isInitialized)
            {
                if (CurrentState == GameStates.Playing && Editor is object)
                {
                    List<Sprite> spr = sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 1, 1);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is Checkpoint)
                        {
                            ActivePlayer.CenterX = sprite.CenterX;
                            if (sprite.FlipY)
                            {
                                ActivePlayer.Gravity = -Math.Abs(ActivePlayer.Gravity);
                                ActivePlayer.Y = sprite.Y;
                            }
                            else
                            {
                                ActivePlayer.Gravity = Math.Abs(ActivePlayer.Gravity);
                                ActivePlayer.Bottom = sprite.Bottom;
                            }
                            (sprite as Checkpoint).HandleCrewmanCollision(ActivePlayer);
                        }
                        else if (sprite is WarpToken)
                        {
                            sprite.HandleCrewmanCollision(ActivePlayer);
                        }
                    }
                }
            }
        }

        //private void ExitPreviews()
        //{
        //    CurrentEditingFocus = FocusOptions.Level;
        //    sprites.Color = Color.White;
        //    hudSprites.Visible = true;
        //    BGSprites.Visible = true;
        //    previews = null;
        //    LeftMouse = false;
        //    clickPreview = null;
        //    previewReason = "";
        //}

        public MapLayer ShowMap(float x, float y, float width, float height, int mapX = -1, int mapY = -1, int mapW = -1, int mapH = -1, bool white = false, bool freeze = true, bool showAll = false)
        {
            MapLayer ret = new MapLayer(this, MapAnimation, x, y, width, height, mapX, mapY, mapW, mapH, white, showAll, MapTexture);
            ret.FreezeBelow = freeze;
            //ret.Finish += (m) => RemoveLayer(m);
            AddLayer(ret);
            return ret;
        }

        public void HideMap()
        {
            if (Layers.Count > 0 && Layers.Last() is MapLayer)
            {
                (Layers.Last() as MapLayer).Close(MapAnimation);
            }
        }

        private void GlControl_MouseMove(MouseMoveEventArgs e)
        {
            MouseIn = true;
            bufferMove = true;
            MouseX = (int)((e.X - xOffset) / scaleSize - hudLeft);
            MouseY = (int)((e.Y - yOffset) / scaleSize);
        }

        public void OpenScript(Script s)
        {
            if (s is null) return;
            float size = 1;
            ScriptEditor = new StringHighlighter(8, 8, FontTexture, this, s, size);
            ScriptEditor.SetBuffers2(s.Contents);
            StringDrawable sd = new StringDrawable(8, 8, FontTexture, s.Contents);
            sd.Size = size;
            ScriptEditor.SetSelectionSprite(sd);
            singleLine = false;
            StartTyping(sd);
            textChanged = (str) =>
            {
                ScriptEditor.SetBuffers2(str);
                ScriptEditor.CheckScroll();
                ScriptEditor.ShowChoices(ScriptEditor.ScrollX, ScriptEditor.ScrollY);
            };
            FinishTyping = (r, st) =>
            {
                s.Contents = sd.Text;
                s.ClearMarkers();
                s.Commands = Command.ParseScript(this, sd.Text, s);
                RemoveLayer(ScriptEditor);
                ScriptEditor = null;
            };
            ScriptEditor.CheckScroll();
            AddLayer(ScriptEditor);
        }

        private void OpenContextMenu(int x, int y)
        {
            ContextMenu cm = new ContextMenu(x, y, contextMenuItems, this);
            AddLayer(cm);
        }

        public void OpenContextMenu(int x, int y, List<VMenuItem> items)
        {
            contextMenuItems = items;
            OpenContextMenu(x, y);
        }

        public void LoadAllTextures()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("textures/").ToList();
            string path = Editor is object ? Editor.CurrentLevelPath : currentLevelPath ?? "";
            if (System.IO.Directory.Exists("levels/" + path + "/textures"))
            {
                files.AddRange(System.IO.Directory.EnumerateFiles("levels/" + path + "/textures"));
            }
            Texture.LoadTextures(files, this);
        }

        private void ResetTextures()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("textures/").ToList();
            files.Sort();
            Texture.LoadTextures(files, this);
            for (int i = 0; i < Textures.Count; i++)
            {
                if (!Textures.Values[i].IsOriginal)
                {
                    Textures.RemoveAt(i--);
                }
            }
        }

        private void LoadAllMusic()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("music/").ToList();
            files.Sort();
            int i = 0;
            foreach (string file in files)
            {
                LoadMusic(file);
                i++;
                percent = 50d / files.Count * i;
            }
            for (int j = 0; j < Songs.Count; j++)
            {
                if (!Songs.Values[j].IsOriginal)
                    Songs.RemoveAt(j--);
            }
            string path = Editor is object ? Editor.CurrentLevelPath : currentLevelPath ?? "";
            if (System.IO.Directory.Exists("levels/" + path + "/music"))
            {
                files = System.IO.Directory.EnumerateFiles("levels/" + path + "/music").ToList();
                files.Sort();
                i = 0;
                foreach (string file in files)
                {
                    LoadMusic(file, false);
                    i++;
                    percent = 50d + (50d / files.Count * i);
                }
            }
        }

        private void LoadMusic(string file, bool original = true)
        {
            if (!file.EndsWith(".ogg")) return;
            string fName = file.Split('/', '\\').Last();
            fName = fName.Substring(0, fName.Length - 4);
            Music m;
            if ((m = GetMusic(fName)).IsNull)
                m = new Music(file);
            else
            {
                if (original && m.IsOriginal)
                    return;
                if (m.IsPlaying)
                    m.Stop();
                m.Update(file);
            }
            m.IsOriginal = original;
            Songs.Add(m.Name, m);
        }

        //INITIALIZE
#region "Init"
        private void InitGlProgram()
        {
            ProgramID = new TextureProgram(GLProgram.Load("shaders/v2dTexTransform.vsh", "shaders/f2dTex.fsh"));
            RectangleSprite.BaseProgram = ProgramID;

            GL.UseProgram(ProgramID.ID);
            int modelMatrixLoc = GL.GetUniformLocation(ProgramID.ID, "model");
            Matrix4 identity = Matrix4.Identity;
            GL.UniformMatrix4(modelMatrixLoc, false, ref identity);
            SetCamera();
            GL.Uniform1(ProgramID.MainLightLocation, 1.0f);
        }

        private void SetCamera()
        {
            // origin at top-left
            Camera = Matrix4.CreateScale(2f / (RESOLUTION_WIDTH + hudLeft), -2f / (RESOLUTION_HEIGHT + hudTop), 0.005f);
            Camera *= Matrix4.CreateTranslation(-1, 1, 0f);
            Camera = Matrix4.CreateTranslation(hudLeft, 0f, 0f) * Camera;
            hudView = Camera;
            GL.UniformMatrix4(ProgramID.ViewMatrixLocation, false, ref hudView);
        }

        private void InitOpenGLSettings()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.One);

            GL.Enable(EnableCap.ScissorTest);

            glControl_Resize(new ResizeEventArgs(gameWindow.Size.X, gameWindow.Size.Y));

            GL.ClearColor(0f, 0f, 0f, 0f);
            // screenshot
            GL.CreateFramebuffers(1, out int fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.CreateTextures(TextureTarget.Texture2D, 1, out int fTex);
            GL.BindTexture(TextureTarget.Texture2D, fTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, RESOLUTION_WIDTH, RESOLUTION_HEIGHT, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, fTex, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                throw new Exception("hey");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            //Rectangle Sprite
            GL.CreateVertexArrays(1, out int texa);
            GL.BindVertexArray(texa);
            GL.CreateBuffers(1, out int texb);
            GL.BindBuffer(BufferTarget.ArrayBuffer, texb);
            float[] fls = new float[]
            {
                0f,       0f,        0f, 0f,
                0f,       1, 0f, 1f,
                1, 1, 1f, 1f,
                1, 0f,        1f, 0f
            };
            GL.BufferData(BufferTarget.ArrayBuffer, fls.Length * sizeof(float), fls, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            RectangleSprite.BaseVAO = texa;
            RectangleSprite.BaseVBO = texb;
        }

        private void InitSounds()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("sounds/").ToList();
            files.Sort();
            int i = 0;
            foreach (string file in files)
            {
                if (!file.EndsWith(".wav")) continue;
                SoundEffect se = new SoundEffect(file);
                Sounds.Add(se.Name, se);
                i++;
                percent = 50d / files.Count * i;
            }
        }

        private void InitMusic()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("music/").ToList();
            files.Sort();
            int i = 0;
            foreach (string file in files)
            {
                LoadMusic(file);
                i++;
                percent = 50d + (50d / files.Count * i);
            }
        }
        //END INITIALIZE
#endregion
        //Screenshot
#region "Screenshot"
        private void Screenshot()
        {
#if Unsafe
            Debugger.NotifyOfCrossThreadDependency();
            if (glControl.InvokeRequired)
                glControl.Invoke((Action)(() => RenderScreengrab()));
            else
                RenderScreengrab();
#endif
        }
#if Unsafe
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
                GL.ReadPixels(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)(fixedData + headerSize));
            RenderOnScreen();

            System.IO.File.WriteAllBytes("screenshot.bmp", data);
        }
        private void RenderOffScreen()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.Viewport(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
        }
        private void RenderOnScreen()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            glControl_Resize(null, null);
        }
#endif
#endregion
        //Dialog
#region "Dialog"
        public void ShowDialog(string prompt, string defaultResponse, string[] choices, Action<bool, string> action = null)
        {
            Dialog d = new Dialog(this, prompt, defaultResponse, choices, action);
            AddLayer(d);
        }
        public void ShowColorDialog(string prompt, string defaultResponse, Action<bool, string> action)
        {
            Dialog d = new Dialog(this, prompt, defaultResponse, null, action, 36, 26, true);
            AddLayer(d);
        }
#endregion

        //MAIN MANU
#region "Main Menu"
        private void MainMenu()
        {
            if (Layers.LastOrDefault() is MenuLayer)
            {
                SpritesLayer mle = Layers.Last();
                RemoveLayer(mle);
                mle.Dispose();
            }
            SelectedItem = 0;
            MenuLayer.Builder mlb = new MenuLayer.Builder(this);
            mlb.AddItem("Play Game", () =>
            {
                GetSound("hurt")?.Play();
                VTextBox tb = new VTextBox(0, 4, FontTexture, "This option is not\n  yet available!  ", Color.Gray);
                tb.Markers.Add(15, 1);
                tb.Markers.Add(18, 0);
                tb.Text = tb.Text;
                tb.CenterX = RESOLUTION_WIDTH / 2;
                tb.frames = 200;
                mlb.Result.ExtraSprites.Add(tb);
                tb.Disappeared += (t) => { mlb.Result.ExtraSprites.Remove(t); t.Dispose(); };
                tb.Appear();
            }, "Play the main story. This is not available yet!");
            mlb.AddItem("Player Levels", () => {
                GetSound("crew1")?.Play();
                PlayerLevelsMenu();
            }, "Play or create a custom level.");
            mlb.AddItem("Options", () => {
                GetSound("crew1")?.Play();
                OptionsMenu();
            }, "Set options to customize your experience!");
            mlb.AddItem("Credits", () => {
                GetSound("crew1")?.Play();
                mlb.ShowTextBox("Made by DaKook. More credits\n     to come later.", FontTexture, RESOLUTION_WIDTH / 2, 20, Color.Gray);
            }, "Display credits. Not much to see here yet!");
            mlb.AddItem("Exit Game", () =>
            {
                GetSound("hurt")?.Play();
                FadeSpeed = -5;
                fadeHud = true;
                WhenFaded = () =>
                {
                    gameWindow.Close();
                };
            }, "I guess you can't just keep playing forever...");
            MenuLayer ml = mlb.Build();
            ml.EscapeItem = -1;
            ml.Background = BGSprites;
        }

        private void OptionsMenu()
        {
            if (Layers.LastOrDefault() is MenuLayer)
            {
                SpritesLayer mle = Layers.Last();
                RemoveLayer(mle);
                mle.Dispose();
            }
            SelectedItem = 0;
            MenuLayer.Builder mlb = new MenuLayer.Builder(this);
            mlb.AddItem("Use Extra HUD Space: " + (EnableExtraHud ? "(x) ON " : "( ) OFF"), () =>
            {
                GetSound("crew1")?.Play();
                EnableExtraHud = !EnableExtraHud;
                mlb.Result.MenuItems[0].Text = "Use Extra HUD Space: " + (EnableExtraHud ? "(x) ON " : "( ) OFF");
                mlb.Result.CreateMenuSprites();
                if (gameWindow.WindowState == WindowState.Normal)
                {
                    Vector2i s = gameWindow.Size;
                    if (!EnableExtraHud)
                        gameWindow.Size = new Vector2i(s.X - (int)(HUD_LEFT * scaleSize), s.Y - (int)(HUD_TOP * scaleSize));
                    else
                        gameWindow.Size = new Vector2i(s.X + (int)(HUD_LEFT * scaleSize), s.Y + (int)(HUD_TOP * scaleSize));
                }
                glControl_Resize(new ResizeEventArgs(gameWindow.Size));
            }, "Toggle the HUD spaces on the left and bottom of the screen.\nSome displays will move to the main screen when this is off.");
            mlb.AddItem("Back", () =>
            {
                GetSound("crew1")?.Play();
                MainMenu();
                SelectedItem = 2;
            }, "Return to the main menu.");
            MenuLayer ml = mlb.Build();
            ml.EscapeItem = ml.MenuItems.Count - 1;
            ml.Background = BGSprites;
        }

        private void PlayerLevelsMenu()
        {
            if (Layers.LastOrDefault() is MenuLayer)
            {
                SpritesLayer mle = Layers.Last();
                RemoveLayer(mle);
                mle.Dispose();
            }
            SelectedItem = 0;
            MenuLayer.Builder mic = new MenuLayer.Builder(this);
            mic.AddItem("Play a Level", () =>
            {
                GetSound("crew1")?.Play();
                PlayLevelMenu();
            }, "Play a custom level in the \"levels\" folder.");
            mic.AddItem("Level Editor", () =>
            {
                fadeHud = true;
                FadeSpeed = -5;
                CurrentSong.FadeOut();
                WhenFaded = () =>
                {
                    ClearMenu();
                    NewLevel();
                    CurrentState = GameStates.Editing;
                    Editor = new LevelEditor(this);
                    AddLayer(Editor);
                    FadeSpeed = 5;
                    WhenFaded = () => { fadeHud = false; };
                };
            }, "Create or edit a custom level. Be creative!");
            mic.AddItem("Back", () =>
            {
                GetSound("crew1")?.Play();
                MainMenu();
            }, "Return to the main menu.");
            MenuLayer ml = mic.Build();
            ml.EscapeItem = 2;
            ml.Background = BGSprites;
        }

        private void PlayLevelMenu()
        {
            
        }

        public void ClearMenu()
        {
            if (Layers.LastOrDefault() is MenuLayer)
            {
                RemoveLayer(Layers.Last());
            }
        }
#endregion

        //TEXTURE EDITOR
#region TextureEditor

#endregion

        public static float scaleSize = 1;
        float xOffset = 0;
        float yOffset = 0;
        private void glControl_Resize(ResizeEventArgs e)
        {
            float relX = (float)gameWindow.ClientSize.X / (RESOLUTION_WIDTH + hudLeft);
            float relY = (float)gameWindow.ClientSize.Y / (RESOLUTION_HEIGHT + hudTop);
            scaleSize = (int)Math.Min(relX, relY);
            int w = (int)((RESOLUTION_WIDTH + hudLeft) * scaleSize);
            int h = (int)((RESOLUTION_HEIGHT + hudTop) * scaleSize);
            xOffset = (gameWindow.ClientSize.X - w) / 2;
            yOffset = (gameWindow.ClientSize.Y - h) / 2;
            GL.Viewport((int)xOffset, (int)yOffset, w, h);
        }
        //   _  __________     __  _____   ______          ___   _   //
        //  | |/ /  ____\ \   / / |  __ \ / __ \ \        / / \ | |  //
        //  | ' /| |__   \ \_/ /  | |  | | |  | \ \  /\  / /|  \| |  //
        //  |  < |  __|   \   /   | |  | | |  | |\ \/  \/ / | . ` |  //
        //  | . \| |____   | |    | |__| | |__| | \  /\  /  | |\  |  //
        //  |_|\_\______|  |_|    |_____/ \____/   \/  \/   |_| \_|  //

        private void GlControl_KeyDown(KeyboardKeyEventArgs e)
        {
            Keys key = e.Key;
            if ((key == Keys.LeftControl || key == Keys.RightControl) && !e.IsRepeat)
                control++;
            else if ((key == Keys.LeftShift || key == Keys.RightShift) && !e.IsRepeat)
                shift++;
            else if ((key == Keys.LeftAlt || key == Keys.RightAlt) && !e.IsRepeat)
                alt++;
            bufferKeys.Add(new PassedKeyEvent(e));
            
            if (key == Keys.Enter && !e.Control && typing)
            {
                keys += '\n';
            }
            else if (key == Keys.Escape && typing)
            {
                if (!(ScriptEditor is object && ScriptEditor.ChoicesVisible))
                    keys += (char)27;
            }
            else if (key == Keys.Backspace && typing)
            {
                keys += (char)8;
            }
            else if (e.Control && e.Key == Keys.A && typing)
            {
                keys += (char)1;
            }
            else if (e.Control && e.Key == Keys.C && typing)
            {
                keys += (char)3;
            }
            else if (e.Control && e.Key == Keys.V && typing)
            {
                keys += (char)22;
            }
            else if (e.Control && e.Key == Keys.X && typing)
            {
                keys += (char)24;
            }
            if (key == Keys.F9) Screenshot();
            if (inputMap.ContainsKey(key) && !heldKeys.Contains(key))
            {
                Inputs ip = inputMap[key];
                if (IsJump(ip))
                    bufferInputs.Add(Inputs.Jump);
                bufferInputs.Add(ip);
            }
            if (!heldKeys.Contains(key))
                heldKeys.Add(key);
        }

        private void HandleKey(PassedKeyEvent e)
        {
            if (!isInitialized && !isLoading)
            {
                if (inputMap.ContainsKey(e.Key) && inputMap[e.Key] == Inputs.Jump)
                {
                    Shake(40, 2);
                    Flash(10);
                    GetSound("gamesaved")?.Play();
                    CurrentSong = GetMusic("Regio Pelagus");
                    CurrentSong.Play();
                    hudSprites.Remove(loadingSprite);
                    MainMenu();
                    isInitialized = true;
                }
                return;
            }
            if (isLoading) return;
            else if (typing)
            {
                if (e.Key == Keys.Right)
                {
                    int index = TypingTo.GetCtrlIndex(false);
                    if (e.Shift)
                    {
                        if (TypingTo.SelectingFromLeft)
                        {
                            if (e.Control)
                            {
                                TypingTo.SelectionLength = index - TypingTo.SelectionStart;
                                if (TypingTo.SelectionLength < 0)
                                {
                                    TypingTo.SelectingFromLeft = false;
                                    TypingTo.SelectionStart += TypingTo.SelectionLength;
                                    TypingTo.SelectionLength *= -1;
                                }
                            }
                            else if (TypingTo.SelectionStart + TypingTo.SelectionLength < TypingTo.Text.Length)
                            {
                                TypingTo.SelectionLength += 1;
                            }
                        }
                        else
                        {
                            if (e.Control)
                            {
                                int origin = TypingTo.SelectionStart + TypingTo.SelectionLength;
                                if (index >= TypingTo.SelectionStart + TypingTo.SelectionLength)
                                {
                                    TypingTo.SelectingFromLeft = true;
                                    TypingTo.SelectionStart = origin;
                                    TypingTo.SelectionLength = index - origin;
                                }
                                else
                                {
                                    TypingTo.SelectionStart = index;
                                    TypingTo.SelectionLength = origin - index;
                                }
                            }
                            else
                            {
                                TypingTo.SelectionLength -= 1;
                                TypingTo.SelectionStart += 1;
                                if (TypingTo.SelectionLength == 0)
                                    TypingTo.SelectingFromLeft = true;
                            }
                        }
                    }
                    else
                    {
                        if (TypingTo.SelectionLength > 0)
                        {
                            TypingTo.SelectionStart += TypingTo.SelectionLength;
                            TypingTo.SelectionLength = 0;
                            TypingTo.SelectingFromLeft = true;
                        }
                        else
                        {
                            if (e.Control)
                            {
                                TypingTo.SelectionStart = TypingTo.GetCtrlIndex(false);
                            }
                            else if (TypingTo.SelectionStart < TypingTo.Text.Length)
                            {
                                TypingTo.SelectionStart += 1;
                            }
                        }
                    }
                    TypingTo.Text = TypingTo.Text;
                }
                else if (e.Key == Keys.Left)
                {
                    if (e.Shift)
                    {
                        if (TypingTo.SelectingFromLeft && TypingTo.SelectionLength > 0)
                        {
                            if (e.Control)
                            {
                                int index = TypingTo.GetCtrlIndex();
                                if (index < TypingTo.SelectionStart)
                                {
                                    int distance = TypingTo.SelectionStart - index;
                                    TypingTo.SelectionStart = index;
                                    TypingTo.SelectionLength = distance;
                                    TypingTo.SelectingFromLeft = false;
                                }
                                else
                                {
                                    TypingTo.SelectionLength = index - TypingTo.SelectionStart;
                                }
                            }
                            else
                            {
                                TypingTo.SelectionLength -= 1;
                            }
                        }
                        else
                        {
                            if (e.Control)
                            {
                                int index = TypingTo.GetCtrlIndex();
                                int distance = TypingTo.SelectionStart - index;
                                TypingTo.SelectionStart -= distance;
                                TypingTo.SelectionLength += distance;
                                TypingTo.SelectingFromLeft = false;
                            }
                            else
                            {
                                if (TypingTo.SelectionStart > 0)
                                {
                                    TypingTo.SelectionLength += 1;
                                    TypingTo.SelectionStart -= 1;
                                    TypingTo.SelectingFromLeft = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (TypingTo.SelectionLength > 0)
                        {
                            TypingTo.SelectionLength = 0;
                            TypingTo.SelectingFromLeft = true;
                        }
                        else
                        {
                            if (e.Control)
                            {
                                TypingTo.SelectionStart = TypingTo.GetCtrlIndex();
                            }
                            else if (TypingTo.SelectionStart > 0)
                            {
                                TypingTo.SelectionStart -= 1;
                            }
                        }
                    }
                    TypingTo.Text = TypingTo.Text;
                }
                if (!singleLine && !(ScriptEditor is object && ScriptEditor.ChoicesVisible && ScriptEditor.Choices.Count > 1))
                {
                    if (e.Key == Keys.Up)
                    {
                        int curLine = 0;
                        int index = -1;
                        bool first = true;
                        List<int> lineStarts = new List<int>();
                        while (index < TypingTo.SelectionStart && (index > -1 || first) && index < TypingTo.Text.Length)
                        {
                            first = false;
                            lineStarts.Add(index);
                            index = TypingTo.Text.IndexOf('\n', index + 1);
                            curLine++;
                        }
                        curLine--;
                        int v;
                        if (curLine <= 0)
                        {
                            v = 0;
                        }
                        else
                        {
                            int cv = TypingTo.SelectionStart - lineStarts[curLine];
                            v = Math.Min(lineStarts[curLine], lineStarts[curLine - 1] + cv);
                        }
                        TypingTo.SelectionStart = Math.Max(v, 0);
                        TypingTo.SelectionLength = 0;

                        TypingTo.Text = TypingTo.Text;
                    }
                    else if (e.Key == Keys.Down)
                    {
                        int curLine = 0;
                        int index = -1;
                        bool first = true;
                        List<int> lineStarts = new List<int>();
                        while (!(index == -1 && !first) && index < TypingTo.Text.Length)
                        {
                            first = false;
                            lineStarts.Add(index);
                            bool ind = index < TypingTo.SelectionStart;
                            index = TypingTo.Text.IndexOf('\n', index + 1);
                            if (index > -1 && ind)
                                curLine++;
                        }
                        curLine--;
                        if (curLine < 0) curLine = 0;
                        int v;
                        if (curLine == lineStarts.Count - 1)
                        {
                            v = TypingTo.Text.Length;
                        }
                        else
                        {
                            int cv = TypingTo.SelectionStart - lineStarts[curLine];
                            v = Math.Min(lineStarts.Count > curLine + 2 ? lineStarts[curLine + 2] : TypingTo.Text.Length, lineStarts[curLine + 1] + cv);
                        }
                        TypingTo.SelectionStart = v;
                        TypingTo.SelectionLength = 0;

                        TypingTo.Text = TypingTo.Text;
                    }
                }
                if (Layers.Count > 0 && GiveInput > -1 && GiveInput < Layers.Count)
                {
                    Layers[GiveInput].HandleKey(e, true);
                }
                return;
            }
            if (Layers.Count > 0 && GiveInput > -1 && GiveInput < Layers.Count)
            {
                Layers[GiveInput].HandleKey(e, false);
                return;
            }
            if (CurrentState == GameStates.Editing)
            {

            }
            else if (CurrentState == GameStates.Playing && Editor is object)
            {
                if (e.Key == Keys.F1)
                {
                    StringDrawable sd = new StringDrawable(8, 8, FontTexture, "", Color.LightGray);
                    sd.Layer = 61;
                    RectangleSprite rs = new RectangleSprite(4, 4, RESOLUTION_WIDTH - 8, 16);
                    rs.Layer = 60;
                    rs.Color = Color.Black;
                    hudSprites.Add(sd);
                    hudSprites.Add(rs);
                    StartTyping(sd);
                    singleLine = true;
                    Freeze = FreezeOptions.Paused;
                    FinishTyping = (r, st) =>
                    {
                        if (r)
                        {
                            Script s = new Script(null, "customScript", "");
                            s.Commands = Command.ParseScript2(this, sd.Text, s);
                            ExecuteScript(s, ActivePlayer, ActivePlayer, new DecimalVariable[] { });
                        }
                        hudSprites.Remove(sd);
                        hudSprites.Remove(rs);
                        Freeze = FreezeOptions.Unfrozen;
                    };
                }
                else if (e.Key == Keys.F2)
                {
                    if (System.IO.Directory.Exists("levels/" + Editor.CurrentLevelPath))
                    {
                        string sv = Newtonsoft.Json.JsonConvert.SerializeObject(CreateSave());
                        System.IO.File.WriteAllText("levels/" + Editor.CurrentLevelPath + "/editorsave.v7s", sv);
                        VTextBox tb = new VTextBox(0, 0, FontTexture, "Saved State", Color.Gray);
                        tb.CenterX = RESOLUTION_WIDTH / 2;
                        tb.CenterY = RESOLUTION_HEIGHT / 2;
                        tb.Layer = 100;
                        tb.frames = 75;
                        tb.Disappeared += (t) => hudSprites.Remove(t);
                        hudSprites.Add(tb);
                        tb.Appear();
                    }
                }
                else if (e.Key == Keys.F3)
                {
                    if (System.IO.File.Exists("levels/" + Editor.CurrentLevelPath + "/editorsave.v7s"))
                    {
                        JObject jo = JObject.Parse(System.IO.File.ReadAllText("levels/" + Editor.CurrentLevelPath + "/editorsave.v7s"));
                        LoadSave(jo);
                    }
                    else
                    {
                        VTextBox tb = new VTextBox(0, 0, FontTexture, "No save state!", Color.Gray);
                        tb.CenterX = RESOLUTION_WIDTH / 2;
                        tb.CenterY = RESOLUTION_HEIGHT / 2;
                        tb.Layer = 100;
                        tb.frames = 75;
                        tb.Disappeared += (t) => hudSprites.Remove(t);
                        hudSprites.Add(tb);
                        tb.Appear();
                    }
                }
            }
            else if (/*CurrentState == GameStates.Menu*/true)
            {
                if (e.Key == Keys.Right || e.Key == Keys.Down || e.Key == Keys.D || e.Key == Keys.S)
                {
                    SelectedItem += 1;
                    SelectedItem %= MenuItems.Count;
                    UpdateMenuSelection();
                }
                else if (e.Key == Keys.Left || e.Key == Keys.Up || e.Key == Keys.A || e.Key == Keys.W)
                {
                    SelectedItem -= 1;
                    if (SelectedItem < 0)
                        SelectedItem = MenuItems.Count - 1;
                    UpdateMenuSelection();
                }
                else if (e.Key == Keys.Z || e.Key == Keys.Space || e.Key == Keys.Enter || e.Key == Keys.V)
                {
                    MenuItems[SelectedItem].Action?.Invoke();
                }
                else if (e.Key == Keys.Escape)
                {
                    if (escapeItem > -1 && escapeItem < MenuItems.Count)
                        MenuItems[escapeItem].Action();
                }
            }
        }
        public void Cutscene()
        {
            CutsceneBars = 1;
        }
        public void EndCutscene()
        {
            CutsceneBars = -1;
        }

        private float mwo;
        private void GlControl_MouseWheel(MouseWheelEventArgs e)
        {
            
            
        }

        public void OpenScripts()
        {
            OpenScripts((s) =>
            {
                OpenScript(ScriptFromName(s));
            });
        }

        public void OpenScripts(Action<string> select)
        {
            PreviewScreen ps = new PreviewScreen(new Sprite[] { }, null, this);
            ps.OnRightClick = (s) =>
            {
                contextMenuItems.Clear();
                contextMenuItems.Add(new VMenuItem("Delete", () =>
                {
                    if (s.Name is object && Scripts.ContainsKey(s.Name))
                    {
                        Scripts.Remove(s.Name);
                        RefreshScripts();
                    }
                }));
                contextMenuItems.Add(new VMenuItem("Rename", () =>
                {
                    if (s.Name is object && Scripts.ContainsKey(s.Name))
                    {
                        FinishTyping(false, "");
                        ShowDialog("New script name?", s.Name, new string[] { }, (r, st) =>
                        {
                            if (r && !Scripts.ContainsKey(st))
                            {
                                Script script = Scripts[s.Name];
                                string oldName = script.Name;
                                script.Name = st;
                                Scripts.Remove(s.Name);
                                Scripts.Add(script.Name, script);
                                for (int i = 0; i < Scripts.Count; i++)
                                {
                                    string sc = Scripts.Values[i].Contents;
                                    string[] lines = sc.Split('\n');
                                    bool changed = false;
                                    for (int j = 0; j < lines.Length; j++)
                                    {
                                        string line = lines[j];
                                        int firstDelim = line.IndexOfAny(new char[] { ',', '(' });
                                        if (firstDelim == -1)
                                            continue;
                                        string cmd = line.Substring(0, firstDelim);
                                        Command.ArgTypes[] args = Command.GetArgs(cmd);
                                        if (args.Contains(Command.ArgTypes.Script))
                                        {
                                            int arg = 0;
                                            int pos = cmd.Length + 1;
                                            bool trimmed;
                                            if (trimmed = line[firstDelim] == '(' && line.EndsWith(")"))
                                                line = line.Substring(0, line.Length - 1);
                                            while (pos > 0)
                                            {
                                                if (args[arg] == Command.ArgTypes.Script)
                                                {
                                                    int index = line.IndexOf(',', pos);
                                                    if (index == -1) index = line.Length;
                                                    string scr = line.Substring(pos, index - pos);
                                                    if (scr == oldName)
                                                    {
                                                        changed = true;
                                                        line = line.Remove(pos, scr.Length);
                                                        line = line.Insert(pos, script.Name);
                                                    }
                                                }
                                                pos = line.IndexOf(',', pos) + 1;
                                                arg++;
                                            }
                                            if (trimmed)
                                                line += ")";
                                            lines[j] = line;
                                        }
                                    }
                                    if (changed)
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        sb.Append(lines[0]);
                                        for (int j = 1; j < lines.Length; j++)
                                        {
                                            sb.Append("\n");
                                            sb.Append(lines[j]);
                                        }
                                        script = Scripts.Values[i];
                                        script.Contents = sb.ToString();
                                        script.Commands = Command.ParseScript(this, script.Contents, script);
                                    }
                                }
                            }
                            OpenScripts();
                        });
                    }
                }));
            };
            RectangleSprite rs = new RectangleSprite(0, 0, RESOLUTION_WIDTH, 32);
            rs.Color = Color.Black;
            rs.Layer = -1;
            ps.HudSprites.Add(rs);
            StringDrawable search = new StringDrawable(0, 20, FontTexture, "", Color.LightGray);
            search.CenterX = RESOLUTION_WIDTH / 2;
            ps.HudSprites.Add(search);
            StartTyping(search);
            singleLine = true;
            textChanged = (s) =>
            {
                search.CenterX = RESOLUTION_WIDTH / 2;
                RefreshScripts(s);
            };
            ps.OnClick = (p) =>
            {
                EscapeTyping();
                select(p.Name);
            };
            FinishTyping = (r, st) =>
            {
                RemoveLayer(ps);
                if (r)
                {
                    if (ScriptFromName(search.Text) is null)
                        Scripts.Add(search.Text, new Script(new Command[] { }, search.Text, ""));
                    select(search.Text);
                }
                else
                {
                    select(null);
                }
            };
            AddLayer(ps);
            RefreshScripts();
        }

        private void RefreshScripts(string searchFor = "")
        {
            if (!(Layers.Last() is PreviewScreen)) return;
            PreviewScreen ps = Layers.Last() as PreviewScreen;
            SpriteCollection previews = ps.Sprites = new SpriteCollection();
            for (int i = 0; i < previews.Count; i++)
            {
                if (previews[i] is VTextBox)
                    previews[i].Dispose();
            }
            previews.Clear();
            int y = 36;
            foreach (Script script in Scripts.Values)
            {
                if (script.Name.Contains(searchFor) || searchFor == "")
                {
                    VTextBox tb = new VTextBox(0, y, FontTexture, script.Name, Color.White);
                    tb.Visible = true;
                    tb.CenterX = RESOLUTION_WIDTH / 2;
                    tb.Name = script.Name;
                    previews.Add(tb);
                    y += (int)tb.Height + 8;
                }
            }
            ps.MaxScroll = Math.Max(y + 4 - RESOLUTION_HEIGHT, 0);
        }

        public void StartTyping(StringDrawable typeTo, Action<bool, string> finish)
        {
            singleLine = true;
            StartTyping(typeTo);
            FinishTyping = finish;
        }

        public void StartTyping(StringDrawable typeTo, Action<bool, string> finish, bool singleLine)
        {
            this.singleLine = singleLine;
            StartTyping(typeTo, finish);
        }

        public void StartTyping(StringDrawable t, Action<string> changed = null, Action<bool, string> finish = null)
        {
            if (TypingTo is object)
            {
                TypingTo.SelectionStart = -1;
                TypingTo.SelectionLength = 0;
                TypingTo.SelectingFromLeft = true;
                TypingTo.Text = TypingTo.Text;
            }
            if (t is null)
            {
                typing = false;
                TypingTo = null;
                return;
            }
            typing = true;
            TypingTo = t;
            t.SelectionStart = t.Text.Length;
            t.SelectionLength = 0;
            t.Text = t.Text;
            textChanged = changed;
            FinishTyping = finish;
        }
        private void GlControl_KeyUp(KeyboardKeyEventArgs e)
        {
            Keys key = e.Key;
            if (key == Keys.LeftControl || key == Keys.RightControl)
                control--;
            else if (key == Keys.LeftShift || key == Keys.RightShift)
                shift--;
            else if (key == Keys.LeftAlt || key == Keys.RightAlt)
                alt--;
            if (heldKeys.Contains(key) && inputMap.ContainsKey(key))
            {
                Inputs ip = inputMap[key];
                if (IsJump(ip))
                    inputs[(int)Inputs.Jump]--;
                inputs[(int)ip]--;
            }
            heldKeys.Remove(key);
        }

        public Script.Executor ExecuteScript(Script script, Sprite sender, Sprite target, DecimalVariable[] args, bool pause = false, SortedList<string, Variable> createdSprites = null)
        {
            if (script is null) return null;
            Script.Executor scr = new Script.Executor(script, this, args);
            if (createdSprites is object)
            {
                for (int i = 0; i < createdSprites.Count; i++)
                {
                    scr.Locals.Add(createdSprites.Keys[i], createdSprites.Values[i]);
                }
            }
            if (pause)
                PauseScripts.Add(scr);
            else
                CurrentScripts.Add(scr);
            if (pause)
                scr.Finished += (s) => { PauseScripts.Remove(s); };
            else
                scr.Finished += (s) => { CurrentScripts.Remove(s); };
            scr.ExecuteFromBeginning(sender, target);
            return scr;
        }

        private void UpdateMenu()
        {
            int clrIndex = Array.IndexOf(MenuColors, MenuColor);
            clrIndex += 1;
            clrIndex %= MenuColors.Length;
            MenuColor = MenuColors[clrIndex];
            if (BGSprites.Visible)
                BGSprites.BaseColor = Color.FromArgb(255, MenuColor.R / 4, MenuColor.G / 4, MenuColor.B / 4);
            if (!hudSprites.Contains(ItemSelector))
                hudSprites.Add(ItemSelector);
            foreach (StringDrawable sd in ItemSprites)
            {
                hudSprites.Remove(sd);
                sd.Dispose();
            }
            ItemSprites.Clear();

            if (isPlayerLevels)
            {
                levelName.Color = MenuColor;
                levelAuthor.Color = Color.FromArgb(255, MenuColor.R * 3 / 5, MenuColor.G * 3 / 5, MenuColor.B * 3 / 5);
                levelSubtitle.Color = Color.FromArgb(255, MenuColor.R * 3 / 5, MenuColor.G * 3 / 5, MenuColor.B * 3 / 5);
                levelDesc.Color = MenuColor;
            }
        }

        private void PlayLevel(string levelName)
        {
            fadeHud = true;
            FadeSpeed = -5;
            CurrentSong.FadeOut();
            WhenFaded = () =>
            {
                if (levelName.EndsWith("?")) levelName = levelName.Substring(0, levelName.Length - 1);
                bool single = !System.IO.File.Exists("levels/" + levelName + "/" + levelName + ".lv7");
                string path = single ? "levels" : "levels/" + levelName;
                if (System.IO.File.Exists(path + "/" + levelName + ".lv7"))
                {
                    ClearMenu();
                    hudSprites.Remove(this.levelName);
                    hudSprites.Remove(levelSubtitle);
                    hudSprites.Remove(levelAuthor);
                    hudSprites.Remove(levelDesc);
                    Editor?.Dispose();
                    Editor = null;
                    CurrentState = GameStates.Playing;
                    currentLevelPath = levelName;
                    isLoadingLevel = true;
                    ResetTextures();
                    LoadAllTextures();
                    Task.Run(() => {
                        LoadLevel(path, levelName, false);
                        CurrentSong = LevelMusic;
                        CurrentSong?.FadeIn();
                        FadePos = 0;
                        FadeSpeed = 5;
                        WhenFaded = () => { fadeHud = false; };
                        isLoadingLevel = false;
                    });
                }
                else
                {
                    FadeSpeed = 5;
                    WhenFaded = () => { fadeHud = false; };
                    GetSound("hurt")?.Play();
                }
            };
        }

        private void UpdateMenuSelection()
        {
            if (isPlayerLevels)
            {
                int si = SelectedItem + page * 8;
                if (SelectedItem < 8 && si < playerLevels.Length)
                {
                    if (playerLevels[si].StartsWith("?") || !System.IO.File.Exists("levels/" + playerLevels[si] + "/info.txt"))
                    {
                        levelName.Text = playerLevels[si];
                        levelAuthor.Text = "";
                        levelSubtitle.Text = "No info";
                        levelDesc.Text = "";
                        levelName.CenterX = RESOLUTION_WIDTH / 2;
                        levelSubtitle.CenterX = RESOLUTION_WIDTH / 2;
                    }
                    else
                    {
                        JObject jObject = JObject.Parse(System.IO.File.ReadAllText("levels/" + playerLevels[si] + "/info.txt"));
                        levelName.Text = (string)jObject["Name"] ?? playerLevels[si];
                        levelAuthor.Text = "by " + (string)jObject["Author"] ?? "Unknown";
                        levelSubtitle.Text = (string)jObject["Subtitle"] ?? "";
                        levelDesc.Text = (string)jObject["Description"] ?? "";
                        levelDesc.AlignToCenter();
                        levelName.CenterX = RESOLUTION_WIDTH / 2;
                        levelAuthor.Right = RESOLUTION_WIDTH - 16;
                        levelSubtitle.CenterX = RESOLUTION_WIDTH / 2;
                        levelDesc.CenterX = RESOLUTION_WIDTH / 2;

                    }
                }
                else
                {
                    levelName.Text = "";
                    levelAuthor.Text = "";
                    levelSubtitle.Text = "";
                    levelDesc.Text = "";
                }
            }
        }

        public Tile GetTile(int x, int y, int layer = -2)
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

        //   ______ _____ _____ _______ ____  _____    //
        //  |  ____|  __ \_   _|__   __/ __ \|  __ \   //
        //  | |__  | |  | || |    | | | |  | | |__) |  //
        //  |  __| | |  | || |    | | | |  | |  _  /   //
        //  | |____| |__| || |_   | | | |__| | | \ \   //
        //  |______|_____/_____|  |_|  \____/|_|  \_\  //

        public void DeleteSprite(Sprite s)
        {
            sprites.RemoveFromCollisions(s);
            switch (s.GetType().Name)
            {
                case "Trinket":
                    {
                        Trinket t = s as Trinket;
                        if (LevelTrinkets.ContainsKey(t.ID))
                        {
                            LevelTrinkets[t.ID] -= 1;
                            if (LevelTrinkets[t.ID] == 0)
                                LevelTrinkets.Remove(t.ID);
                        }
                    }
                    break;
                case "WarpToken":
                    {
                        WarpToken w = s as WarpToken;
                        if (w.Data.OutRoom == new Point(CurrentRoom.X, CurrentRoom.Y))
                        {
                            PointF o = w.Data.Out;
                            List<Sprite> sprs = sprites.GetPotentialColliders(o.X, o.Y, 1, 1);
                            for (int i = 0; i < sprs.Count; i++)
                            {
                                if (sprs[i] is WarpTokenOutput && (sprs[i] as WarpTokenOutput).ID == w.ID)
                                {
                                    sprites.RemoveFromCollisions(sprs[i]);
                                    break;
                                }
                            }
                        }
                        Warps.Remove(w.ID);
                        UserAccessSprites.Remove(w.Name);
                    }
                    break;
            }
            if (s.Name is null || !UserAccessSprites.ContainsKey(s.Name))
            {
                s.Dispose();
            }
        }

        public void TileFillTool(float x, float y, bool leftClick, LevelEditor.Tools tool, AutoTileSettings autoTiles, Point currentTile, int tileLayer, TileTexture currentTexture, char prefix, List<PointF> alreadyFilled = null, int tileX = -1, int tileY = -1, string tag = null, bool lr = true, bool ud = true)
        {
            bool isAuto = tool != LevelEditor.Tools.Tiles;
            List<PointF> toFill = new List<PointF>();
            bool initial = false;
            if (alreadyFilled == null)
            {
                alreadyFilled = new List<PointF>();
                Tile baseTile = GetTile((int)x, (int)y, tileLayer);
                if (baseTile != null)
                {
                    tileX = baseTile.TextureX;
                    tileY = baseTile.TextureY;
                    tag = baseTile.Tag;
                }
                initial = true;
            }
            if (alreadyFilled.Contains(new PointF(x, y))) return;
            if (isAuto)
            {
                toFill.Add(new PointF(x, y));
            }
            else
                TileTool(x, y, leftClick, currentTexture, currentTile, tileLayer);
            alreadyFilled.Add(new PointF(x, y));
            Point[] toCheck = new Point[] { new Point(8, 0), new Point(0, 8), new Point(-8, 0), new Point(0, -8) };
            foreach (Point point in toCheck)
            {
                if (IsOutsideRoom(x + point.X, y + point.Y))
                    continue;
                if (point.X != 0 && !lr) continue;
                if (point.Y != 0 && !ud) continue;
                Tile t = GetTile((int)x + point.X, (int)y + point.Y, tileLayer);
                int tx = -1;
                int ty = -1;
                if (t != null)
                {
                    tx = t.TextureX;
                    ty = t.TextureY;
                }
                if (((!isAuto || tag == null) && tx == tileX && ty == tileY) || (isAuto && tag != null && t?.Tag == tag))
                {
                    TileFillTool(x + point.X, y + point.Y, leftClick, tool, autoTiles, currentTile, tileLayer, currentTexture, prefix, alreadyFilled, tileX, tileY, tag, lr, ud);
                }
            }
            if (initial && toFill.Count > 0)
            {
                AutoTilesToolMulti(alreadyFilled, leftClick, LevelEditor.Tools.Tiles, autoTiles, tileLayer, currentTexture, prefix);
            }
        }

        public void SpikesFillTool(float x, float y, bool leftClick, LevelEditor.Tools tool, AutoTileSettings autoTiles, int tileLayer, TileTexture currentTexture, bool lr = true, bool ud = true)
        {
            Tile t;
            int dir = -1;
            t = GetTile((int)x, (int)y - 8);
            if (t is object && t.Solid == Sprite.SolidState.Ground && lr)
            {
                dir = 0;
            }
            else if ((t = GetTile((int)x + 8, (int)y)) is object && t.Solid == Sprite.SolidState.Ground && ud)
            {
                dir = 1;
            }
            else if ((t = GetTile((int)x, (int)y + 8)) is object && t.Solid == Sprite.SolidState.Ground && lr)
            {
                dir = 2;
            }
            else if ((t = GetTile((int)x - 8, (int)y)) is object && t.Solid == Sprite.SolidState.Ground && ud)
            {
                dir = 3;
            }
            if (dir > -1)
            {
                bool going = true;
                int increment = -8;
                Point currentLocation = new Point((int)x, (int)y);
                if ((t = GetTile(currentLocation.X, currentLocation.Y, tileLayer)) is object && t.Solid == Sprite.SolidState.Ground)
                    return;
                Point tilePoint = autoTiles.GetTile((int)Math.Pow(2, dir));
                while (going)
                {
                    Tile tile = GetTile(currentLocation.X, currentLocation.Y, tileLayer);
                    if (tile is object)
                    {
                        //if (tile.Solid == Sprite.SolidState.Ground)
                        {
                            if (increment == 8)
                                break;
                            else
                            {
                                increment = 8;
                                if (dir == 0 || dir == 2)
                                    currentLocation.X = (int)x + 8;
                                else
                                    currentLocation.Y = (int)y + 8;
                                continue;
                            }
                        }
                    }
                    tile = new Tile(currentLocation.X, currentLocation.Y, currentTexture, tilePoint.X, tilePoint.Y);
                    tile.Tag = "s" + autoTiles.Name;
                    sprites.AddForCollisions(tile);
                    if (dir == 0 || dir == 2)
                    {
                        currentLocation.X += increment;
                        tile = GetTile(currentLocation.X, currentLocation.Y + (dir - 1) * 8, tileLayer);
                        if (!(tile is object && tile.Solid == Sprite.SolidState.Ground))
                        {
                            if (increment == -8)
                            {
                                increment = 8;
                                currentLocation.X = (int)x + 8;
                                tile = GetTile(currentLocation.X, currentLocation.Y + (dir - 1) * 8, tileLayer);
                                if (!(tile is object && tile.Solid == Sprite.SolidState.Ground))
                                {
                                    going = false;
                                }
                            }
                            else
                                going = false;
                        }
                    }
                    else
                    {
                        currentLocation.Y += increment;
                        tile = GetTile(currentLocation.X + (2 - dir) * 8, currentLocation.Y, tileLayer);
                        if (!(tile is object && tile.Solid == Sprite.SolidState.Ground))
                        {
                            if (increment == -8)
                            {
                                increment = 8;
                                currentLocation.Y = (int)y + 8;
                                tile = GetTile(currentLocation.X + (2 - dir) * 8, currentLocation.Y, tileLayer);
                                if (!(tile is object && tile.Solid == Sprite.SolidState.Ground))
                                {
                                    going = false;
                                }
                            }
                            else
                                going = false;
                        }
                    }
                }
            }
        }

        public bool IsOutsideRoom(float x, float y)
        {
            return x < CameraX || x >= CameraX + Room.ROOM_WIDTH || y < CameraY || y >= CameraY + Room.ROOM_HEIGHT;
        }

        public void TileTool(float x, float y, bool leftClick, TileTexture currentTexture, Point currentTile, int tileLayer)
        {
            if (IsOutsideRoom(x, y)) return;
            Tile tile = GetTile((int)x, (int)y, tileLayer);
            if (tile != null)
            {
                sprites.RemoveFromCollisions(tile);
            }
            if (leftClick)
            {
                Tile t = new Tile((int)x, (int)y, currentTexture, currentTile.X, currentTile.Y);
                t.Layer = tileLayer;
                sprites.AddForCollisions(t);
            }
        }

        public void AutoTilesToolMulti(List<PointF> points, bool leftClick, LevelEditor.Tools tool, AutoTileSettings autoTiles, int tileLayer, TileTexture currentTexture, char prefix, bool separate = false)
        {
            Comparer<PointF> comparer = Comparer<PointF>.Create((p1, p2) =>
            {
                int r = p1.X.CompareTo(p2.X);
                if (r == 0)
                    r = p1.Y.CompareTo(p2.Y);
                return r;
            });
            SortedSet<PointF> alreadyFilled = new SortedSet<PointF>(comparer);
            SortedSet<PointF> pts = new SortedSet<PointF>(points, comparer);
            foreach (PointF point in pts)
            {
                if (IsOutsideRoom(point.X, point.Y)) continue;
                Tile tile = GetTile((int)point.X, (int)point.Y, tileLayer);
                if (tile is object)
                {
                    if (leftClick && (tool == LevelEditor.Tools.Background || tool == LevelEditor.Tools.Spikes) && tile.Solid == Sprite.SolidState.Ground)
                        continue;
                    sprites.RemoveFromCollisions(tile);
                }
                if (leftClick)
                {
                    Point p = autoTiles.GetTile(AutoTilesPredicate((int)point.X, (int)point.Y, leftClick, tool, autoTiles, prefix, pts, separate, tileLayer));
                    if (autoTiles.Size2 != new Point(1, 1))
                    {
                        int xAdd = (int)point.X / 8 % autoTiles.Size2.X * 8;
                        int yAdd = (int)point.Y / 8 % autoTiles.Size2.Y * 6;
                        p.X += xAdd;
                        p.Y += yAdd;
                    }
                    Tile t = new Tile((int)point.X, (int)point.Y, currentTexture, p.X, p.Y);
                    t.Layer = tileLayer;
                    t.Tag = prefix + autoTiles.Name;
                    sprites.AddForCollisions(t);
                }
                if (tool != LevelEditor.Tools.Spikes && !separate)
                {
                    for (int i = -1; i < 2; i++)
                    {
                        for (int j = -1; j < 2; j++)
                        {
                            if (i != 0 || j != 0)
                            {
                                int xx = (int)point.X + (i * 8);
                                int yy = (int)point.Y + (j * 8);
                                if (pts.Contains(new PointF(xx, yy)) || alreadyFilled.Contains(new PointF(xx, yy))) continue;
                                alreadyFilled.Add(new PointF(xx, yy));
                                if (IsOutsideRoom(xx, yy)) continue;
                                if (GetTile(xx, yy, tileLayer)?.Tag == prefix + autoTiles.Name)
                                {
                                    tile = GetTile(xx, yy, tileLayer);
                                    int l = tile.Layer;
                                    if (tile != null)
                                    {
                                        sprites.RemoveFromCollisions(tile);
                                    }
                                    Point p = autoTiles.GetTile(AutoTilesPredicate(xx, yy, leftClick, tool, autoTiles, prefix, pts, false, tileLayer));
                                    if (autoTiles.Size2 != new Point(1, 1))
                                    {
                                        int xAdd = xx / 8 % autoTiles.Size2.X * 8;
                                        int yAdd = yy / 8 % autoTiles.Size2.Y * 6;
                                        p.X += xAdd;
                                        p.Y += yAdd;
                                    }
                                    Tile t = new Tile(xx, yy, currentTexture, p.X, p.Y);
                                    t.Layer = l;
                                    t.Tag = prefix + autoTiles.Name;
                                    sprites.AddForCollisions(t);
                                }
                            }
                        }
                    }
                }
            }
        }

        private Predicate<Point> AutoTilesPredicate(int x, int y, bool leftClick, LevelEditor.Tools tool, AutoTileSettings autoTiles, char prefix, SortedSet<PointF> accountFor = null, bool separate = false, int layer = -2)
        {
            bool bg = tool != LevelEditor.Tools.Ground;
            bool sp = tool == LevelEditor.Tools.Spikes;
            Tile gt;
            return (p) =>
            {
                if (!sp && accountFor is object && accountFor.Contains(new PointF(p.X + x, p.Y + y))) return leftClick;
                if (separate) return false;
                int s = autoTiles.Size;
                if (s == 4)
                {
                    if (p.X != 0 && heldKeys.Contains(Keys.LeftBracket)) return false;
                    if (p.Y != 0 && heldKeys.Contains(Keys.RightBracket)) return false;
                }
                else if (s == 3)
                {
                    if (p.X != 0 && heldKeys.Contains(Keys.RightBracket)) return false;
                    if (p.Y != 0 && heldKeys.Contains(Keys.LeftBracket)) return false;
                }
                return (gt = GetTile(p.X + x, p.Y + y, layer)) != null && ((!sp && gt.Tag == prefix + autoTiles.Name) || (bg && gt.Solid == Sprite.SolidState.Ground)) ||
                     (p.X < 0 && x == CurrentRoom.GetX) ||
                     (p.X > 0 && x == CurrentRoom.Right - 8) ||
                     (p.Y < 0 && y == CurrentRoom.GetY) ||
                     (p.Y > 0 && y == CurrentRoom.Bottom - 8);
            };
        }

        private void HandleUserInputs()
        {
            if (Editor is object && IsInputNew(Inputs.Escape))
            {
                ExitPlaytest();      
            }
            else if (IsInputNew(Inputs.Escape) && !ignoreAction)
            {
                float volume = CurrentSong.Volume;
                CurrentSong.Volume /= 4;
                GetSound("pause")?.Play();
                FreezeOptions restore = Freeze;
                Freeze = FreezeOptions.Paused;
                MenuItems.Clear();
                SelectedItem = 0;
                BGSprites.Visible = false;
                RectangleSprite rs = new RectangleSprite(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                rs.Color = Color.FromArgb(127, 0, 0, 0);
                rs.Layer = int.MaxValue - 2;
                hudSprites.Add(rs);
                escapeItem = 0;
                MenuItems.Add(new VMenuItem("Continue Playing", () =>
                {
                    GetSound("unpause")?.Play();
                    ClearMenu();
                    hudSprites.Remove(rs);
                    BGSprites.Visible = true;
                    ignoreAction = true;
                    CurrentState = GameStates.Playing;
                    Freeze = restore;
                    CurrentSong.Volume = volume;
                }));
                MenuItems.Add(new VMenuItem("Return to Menu", () =>
                {
                    CurrentSong.Volume = volume;
                    hudSprites.Remove(rs);
                    ReturnToMenu();
                }));
                //CurrentState = GameStates.Menu;
                UpdateMenu();
                UpdateMenuSelection();
            }
            else if (IsInputNew(Inputs.Pause) && !ignoreAction && CurrentActivityZone is null)
            {
                Freeze = FreezeOptions.Paused;
                if (PauseScript is object)
                    ExecuteScript(PauseScript, ActivePlayer, ActivePlayer, new DecimalVariable[] { }, true);
                else
                {
                    MapLayer ml = new MapLayer(this, MapAnimations.Up, 0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                    ml.EnableSelect = false;
                    Texture mcm = TextureFromName("minicrewman");
                    if (mcm is object)
                    {
                        Sprite locIndic = new Sprite(0, 0, mcm, mcm.AnimationFromName("happy"));
                        locIndic.CenterX = ml.TopLeft.X + ml.CellSize.Width * (CurrentRoom.X + 0.5f);
                        locIndic.CenterY = ml.TopLeft.Y + ml.CellSize.Height * (CurrentRoom.Y + 0.5f);
                        locIndic.Layer = 55;
                        locIndic.Color = ActivePlayer.TextBoxColor;
                        ml.AddSprite(locIndic);
                    }
                    AddLayer(ml);
                }
            }
            if (PlayerControl && (Freeze == FreezeOptions.Unfrozen || Freeze == FreezeOptions.FreezeScreen))
            {
                if (UsingUpDown)
                {
                    if (IsInputActive(Inputs.Down))
                        ActivePlayer.InputDirection = 1;
                    else if (IsInputActive(Inputs.Up))
                        ActivePlayer.InputDirection = -1;
                    else
                        ActivePlayer.InputDirection = 0;
                }
                else
                {
                    if (IsInputActive(Inputs.Right))
                        ActivePlayer.InputDirection = 1;
                    else if (IsInputActive(Inputs.Left))
                        ActivePlayer.InputDirection = -1;
                    else
                        ActivePlayer.InputDirection = 0;
                }

                if (IsInputActive(Inputs.Kill) && Freeze != FreezeOptions.OnlySprites)
                    ActivePlayer.KillSelf();

                if (ActivePlayer.Script is object && IsInputNew(Inputs.Special))
                {
                    Script s = ActivePlayer.Script;
                    ActivePlayer.Script = null;
                    ExecuteScript(s, ActivePlayer, ActivePlayer, new DecimalVariable[] { });
                }
                if (CurrentActivityZone is object && IsInputNew(Inputs.Pause))
                {
                    if (CurrentActivityZone is Lever)
                    {
                        Lever l = CurrentActivityZone as Lever;
                        l.On = !l.On;
                        if (l.On)
                            l.Animation = l.OnAnimation;
                        else
                            l.Animation = l.OffAnimation;
                        l.ResetAnimation();
                    }
                    else
                        CurrentActivityZone.Activated = true;
                    ExecuteScript(CurrentActivityZone.Script, CurrentActivityZone.Sprite, ActivePlayer, new DecimalVariable[] { });
                }
            }

            if (IsInputActive(Inputs.Jump) && !ignoreAction)
            {
                if (IsInputNew(Inputs.Jump))
                {
                    if (WaitingForAction)
                    {
                        WaitingForAction = false;
                        for (int i = CurrentScripts.Count - 1; i >= 0 && CurrentScripts.Count > 0; i--)
                        {
                            if (i >= CurrentScripts.Count) i = CurrentScripts.Count - 1;
                            Script.Executor script = CurrentScripts[i];
                            if (script.WaitingForAction != null)
                            {
                                script.WaitingForAction();
                                script.WaitingForAction = null;
                                script.Continue();
                            }
                        }
                    }
                    else if (PlayerControl && (Freeze == FreezeOptions.Unfrozen || Freeze == FreezeOptions.FreezeScreen))
                        ActivePlayer.FlipOrJump();
                }
            }
            ignoreAction = false;
        }

        public void ReturnToMenu()
        {
            CurrentSong.FadeOut(1.5f);
            FadeSpeed = -5;
            fadeHud = true;
            WhenFaded = () =>
            {
                //Reset everything
                foreach (Sprite sprite in hudSpritesUser.Values)
                {
                    hudSprites.Remove(sprite);
                }
                hudSpritesUser.Clear();
                foreach (StringDrawable sd in hudText.Values)
                {
                    hudSprites.Remove(sd);
                }
                hudText.Clear();
                for (int i = 0; i < CurrentScripts.Count;)
                {
                    Script.Executor script = CurrentScripts[i];
                    script.Stop();
                    foreach (VTextBox tb in script.TextBoxes)
                    {
                        tb.Disappear();
                    }
                }
                while (Layers.Count > 0)
                {
                    RemoveLayer(Layers.Last());
                }
                UserAccessSprites.Clear();
                CollectedTrinkets.Clear();
                Vars.Clear();
                RoomDatas.Clear();
                MapAnimation = MapAnimations.None;
                HideMap();
                foreach (IMapSprite ms in MapSprites.Values)
                {
                    ms.Dispose();
                }
                MapSprites.Clear();
                MapSprites.Clear();
                //mapImages.Clear();
                foreach (RoomGroup rg in GroupList)
                {
                    rg.Unload();
                    rg.Dispose();
                }
                GroupList.Clear();
                RoomGroups.Clear();
                PlayerControl = true;
                OnPlayerDeath = null;
                OnPlayerRespawn = null;
                flashFrames = 0;
                shakeFrames = 0;
                shakeIntensity = 2;
                flashColour = Color.White;
                CurrentActivityZone = null;
                CutsceneBarTop.Right = 0;
                CutsceneBarBottom.X = RESOLUTION_WIDTH;
                ResetTextures();
                //currentLevelPath = "";
                LoadAllMusic();
                CurrentSong = GetMusic("Regio Pelagus");
                CurrentSong.Rewind();
                CurrentSong.Play();
                CurrentRoom?.Dispose();
                CurrentRoom = null;
                BGSprites.Load(menuBG, this);
                BGSprites.Visible = true;
                MainMenu();
                Freeze = FreezeOptions.Unfrozen;
                FadeSpeed = 5;
                fadeHud = true;
                WhenFaded = () => { fadeHud = false; };
            };
        }

        public void ExitPlaytest()
        {
            foreach (VTextBox textBox in TextBoxes)
            {
                textBox.Disappear();
            }
            foreach (StringDrawable sd in hudText.Values)
            {
                hudSprites.Remove(sd);
            }
            foreach (Sprite sp in hudSpritesUser.Values)
            {
                hudSprites.Remove(sp);
            }
            hudSpritesUser.Clear();
            hudText.Clear();
            CurrentScripts.Clear();
            CurrentState = GameStates.Editing;
            
            ActivePlayer.IsWarpingH = ActivePlayer.IsWarpingV = ActivePlayer.MultiplePositions = false;
            ActivePlayer.Offsets.Clear();
            Editor.ExitPlaytest();
            Freeze = FreezeOptions.Unfrozen;
            CurrentSong?.FadeOut();
            LoadRoom(CurrentRoom.X, CurrentRoom.Y);
            AutoScroll = false;
            AutoScrollX = 0;
            AutoScrollY = 0;
            HideMap();
            MapAnimation = MapAnimations.Up;
            PlayerControl = true;
            CutsceneBars = 0;
            CutsceneBarBottom.X = RESOLUTION_WIDTH;
            CutsceneBarTop.Right = 0;
            foreach (RoomGroup roomGroup in GroupList)
            {
                roomGroup.Unload();
            }

            MainLight = 1.0f;
            while (LightCount > 0)
                RemoveLight(0);
        }

        public void SaveCurrentRoom()
        {
            ActivePlayer.IsWarpingH = false;
            ActivePlayer.IsWarpingV = false;
            ActivePlayer.MultiplePositions = false;
            ActivePlayer.Offsets.Clear();
            if (CurrentRoom is RoomGroup)
            {
                bool firstRoom = true;
                for (int x = CurrentRoom.X; x < CurrentRoom.X + CurrentRoom.WidthRooms; x++)
                {
                    for (int y = CurrentRoom.Y; y < CurrentRoom.Y + CurrentRoom.HeightRooms; y++)
                    {
                        Room r = new Room(new SpriteCollection(), CurrentRoom.EnterScript, CurrentRoom.ExitScript);
                        r.Objects.AddRange(sprites.Where((s) =>
                        {
                            return s.X >= x * RESOLUTION_WIDTH && s.X < (x + 1) * RESOLUTION_WIDTH &&
                                   s.Y >= y * RESOLUTION_HEIGHT && s.Y < (y + 1) * RESOLUTION_HEIGHT;
                        }));
                        if (firstRoom)
                        {
                            r.Name = CurrentRoom.Name;
                            r.RoomDown = CurrentRoom.RoomDown;
                            r.RoomUp = CurrentRoom.RoomUp;
                            r.RoomRight = CurrentRoom.RoomRight;
                            r.RoomLeft = CurrentRoom.RoomLeft;
                            r.Tags = CurrentRoom.Tags;
                            firstRoom = false;
                        }
                        r.PresetName = CurrentRoom.PresetName;
                        r.GroupName = CurrentRoom.GroupName;
                        r.Background = CurrentRoom.Background;
                        r.Ground = CurrentRoom.Ground;
                        r.Spikes = CurrentRoom.Spikes;
                        r.Color = CurrentRoom.Color;
                        r.TileTexture = CurrentRoom.TileTexture;
                        r.X = x;
                        r.Y = y;

                        JObject room = RoomDatas[x + y * 100] = r.Save(this);
                        if (MapSprites.ContainsKey(FocusedRoom))
                            MapSprites[FocusedRoom].Dispose();
                        MapSprites.Remove(FocusedRoom);
                        MapSprites.Add(FocusedRoom, MapSprite.FromRoom(room, 1, this));
                        (CurrentRoom as RoomGroup).RoomDatas[FocusedRoom] = RoomDatas[FocusedRoom];
                    }
                }
            }
            else if (CurrentRoom is object)
            {
                JObject r = CurrentRoom.Save(this);
                RoomDatas[FocusedRoom] = r;
                if (MapSprites.ContainsKey(FocusedRoom))
                    MapSprites[FocusedRoom].Dispose();
                MapSprites.Remove(FocusedRoom);
                MapSprites.Add(FocusedRoom, MapSprite.FromRoom(r, 1, this));
                if (RoomGroups.ContainsKey(FocusedRoom))
                {
                    RoomGroups[FocusedRoom].RoomDatas[FocusedRoom] = RoomDatas[FocusedRoom];
                }
            }
        }

        public void LoadRoom(int x, int y)
        {
            if (Editor is null || CurrentRoom != Editor.WarpRoom)
            {
                CurrentRoom?.Dispose();
            }
            Color fade = sprites?.Color ?? Color.White;
            foreach (ActivityZone activityZone in ActivityZones)
            {
                if (activityZone.TextBox is object)
                    hudSprites.Remove(activityZone.TextBox);
            }
            ActivityZones.Clear();
            if (CurrentState == GameStates.Playing && CurrentRoom?.ExitScript is object)
            {
                ExecuteScript(CurrentRoom.ExitScript, ActivePlayer, ActivePlayer, new DecimalVariable[] { });
            }
            if (Editor is object && CurrentState == GameStates.Editing)
            {
                if (Editor.SaveRoom)
                {
                    SaveCurrentRoom();
                }
                Editor.SaveRoom = true;
            }
            FocusedRoom = x + y * 100;
            if (RoomGroups.ContainsKey(FocusedRoom) && CurrentState == GameStates.Playing)
            {
                RoomGroup load = RoomGroups[FocusedRoom].Load(this);
                while (load.Loading)
                {
                }
                if (load != CurrentRoom)
                {
                    if (Editor is object)
                        Editor.RoomLoc.Text = "Room " + load.X.ToString() + ", " + load.Y.ToString();
                    CurrentRoom = load;
                    if (!CurrentRoom.Objects.Contains(ActivePlayer))
                        CurrentRoom.Objects.Add(ActivePlayer);
                    ActivePlayer.CenterX = (ActivePlayer.CenterX + Room.ROOM_WIDTH) % Room.ROOM_WIDTH + x * Room.ROOM_WIDTH;
                    ActivePlayer.CenterY = (ActivePlayer.CenterY + Room.ROOM_HEIGHT) % Room.ROOM_HEIGHT + y * Room.ROOM_HEIGHT;
                    MaxScrollX = CurrentRoom.Right - RESOLUTION_WIDTH;
                    MaxScrollY = CurrentRoom.Bottom - RESOLUTION_HEIGHT;
                    MinScrollX = CurrentRoom.GetX;
                    MinScrollY = CurrentRoom.GetY;
                    PointF target = GetCameraTarget();
                    CameraX = target.X;
                    CameraY = target.Y;
                    if (CurrentRoom.Name != "")
                    {
                        RoomName.Text = CurrentRoom.Name;
                        RoomName.CenterX = RESOLUTION_WIDTH / 2;
                        RoomNameBar.Bottom = RESOLUTION_HEIGHT;
                        RoomName.Y = RoomNameBar.Y + 1;
                        if (!hudSprites.Contains(RoomName))
                        {
                            hudSprites.Add(RoomName);
                            hudSprites.Add(RoomNameBar);
                        }
                    }
                    else
                    {
                        hudSprites.Remove(RoomName);
                        hudSprites.Remove(RoomNameBar);
                    }
                }
            }
            else
            {
                exitCollisions = true;
                if (Editor is object)
                {
                    Editor.RoomLoc.Text = "Room " + x.ToString() + ", " + y.ToString();
                    Editor.RoomLoc.Right = RESOLUTION_WIDTH - 4;
                }
                if (!RoomDatas.ContainsKey(FocusedRoom))
                {
                    Room r = new Room(new SpriteCollection(), Script.Empty, Script.Empty);
                    r.X = x;
                    r.Y = y;
                    r.TileTexture = TextureFromName("tiles") as TileTexture;
                    int ssn = x + y;
                    if (ssn < 0) ssn = 0;
                    if (RoomPresets.ContainsKey("Space Station"))
                    {
                        AutoTileSettings.PresetGroup g = RoomPresets["Space Station"];
                        AutoTileSettings.RoomPreset p = g.Values[ssn % g.Count];
                        r.UsePreset(p, g.Name);
                    }
                    RoomDatas.Add(FocusedRoom, r.Save(this));
                }
                CurrentRoom = Room.LoadRoom(RoomDatas[FocusedRoom], this);
                CameraX = CurrentRoom.X * Room.ROOM_WIDTH;
                CameraY = CurrentRoom.Y * Room.ROOM_HEIGHT;
                if (!(Editor is object && CurrentState == GameStates.Editing))
                {
                    if (!sprites.Contains(ActivePlayer))
                        CurrentRoom.Objects.Add(ActivePlayer);
                    ExploredRooms.Add(new Point(x, y));
                }
                ActivePlayer.CenterX = (ActivePlayer.CenterX + Room.ROOM_WIDTH) % Room.ROOM_WIDTH + CurrentRoom.X * Room.ROOM_WIDTH;
                ActivePlayer.CenterY = (ActivePlayer.CenterY + Room.ROOM_HEIGHT) % Room.ROOM_HEIGHT + CurrentRoom.Y * Room.ROOM_HEIGHT;
                sprites.RemoveAll((s) => s is Tile && IsOutsideRoom(s.X, s.Y));

                if (CurrentRoom.Name != "")
                {
                    RoomName.Text = CurrentRoom.Name;
                    RoomName.CenterX = RESOLUTION_WIDTH / 2;
                    RoomNameBar.Bottom = RESOLUTION_HEIGHT;
                    RoomName.Y = RoomNameBar.Y + 1;
                    if (!hudSprites.Contains(RoomName))
                    {
                        hudSprites.Add(RoomName);
                        hudSprites.Add(RoomNameBar);
                    }
                }
                else
                {
                    hudSprites.Remove(RoomName);
                    hudSprites.Remove(RoomNameBar);
                }
                if (CurrentState == GameStates.Editing)
                {
                    Editor.ShowTileIndicators();
                    for (int i = 0; i < Warps.Values.Count; i++)
                    {
                        WarpToken.WarpData data = Warps.Values[i];
                        if (data.OutRoom == new Point(x, y))
                        {
                            Texture sp32 = TextureFromName("sprites32");
                            WarpTokenOutput wto = new WarpTokenOutput(data.Out.X, data.Out.Y, sp32, sp32.AnimationFromName("WarpToken"), data, Warps.Keys[i]);
                            sprites.Add(wto);
                        }
                    }
                }
            }
            if (CurrentRoom.BG is object)
            {
                if ((string)CurrentRoom.BG["Name"] != BGSprites.Name)
                    BGSprites.Load(CurrentRoom.BG, this);
            }
            sprites.Color = fade;
            MaxScrollX = CurrentRoom.Right - RESOLUTION_WIDTH;
            MaxScrollY = CurrentRoom.Bottom - RESOLUTION_HEIGHT;
            MinScrollX = CurrentRoom.GetX;
            MinScrollY = CurrentRoom.GetY;
            if (CurrentState == GameStates.Playing && CurrentRoom.EnterScript is object)
            {
                ExecuteScript(CurrentRoom.EnterScript, ActivePlayer, ActivePlayer, new DecimalVariable[] { });
            }
        }

        private void ProcessWorld()
        {
            if (CurrentActivityZone is object && (ActivePlayer.IsOverlapping(CurrentActivityZone as Sprite) is null || CurrentActivityZone.Activated))
            {
                CurrentActivityZone.TextBox?.Disappear();
                CurrentActivityZone = null;
            }
            List<Sprite> toProcess = sprites.ToProcess;
            if (CurrentRoom.IsGroup)
            {
                toProcess = toProcess.FindAll((s) =>
                {
                    return Math.Abs(Math.Floor(s.X / Room.ROOM_WIDTH) - Math.Floor(ActivePlayer.X / Room.ROOM_WIDTH)) <= 1 &&
                        Math.Abs(Math.Floor(s.Y / Room.ROOM_HEIGHT) - Math.Floor(ActivePlayer.Y / Room.ROOM_HEIGHT)) <= 1;
                });
            }
            if (Freeze != FreezeOptions.OnlyMovement)
            {
                for (int i = 0; i < toProcess.Count; i++)
                {
                    toProcess[i].SetPreviousLoaction();
                }
                for (int i = 0; i < toProcess.Count; i++)
                {
                    toProcess[i].Process();
                }
            }
            else
            {
                for (int i = 0; i < toProcess.Count; i++)
                {
                    toProcess[i].AdvanceFrame();
                }
            }
            PerformAllCollisionChecks(toProcess.FindAll((s) => !s.Immovable));

            foreach (ActivityZone activityZone in ActivityZones)
            {
                activityZone.Process();
            }
            if (AutoScroll)
            {
                if (AutoScrollX != 0 && (ActivePlayer.X < CameraX || ActivePlayer.Right > CameraX + RESOLUTION_WIDTH))
                    ActivePlayer.Die();
                else if (AutoScrollY != 0 && (ActivePlayer.Y < CameraY || ActivePlayer.Bottom > CameraY + RESOLUTION_HEIGHT))
                    ActivePlayer.Die();
            }
            if (!ActivePlayer.IsWarpingV && !ActivePlayer.IsWarpingH)
            {
                CheckPlayerRoom(true);
            }
            //Camera
            if (!AutoScroll)
            {
                PointF target = GetCameraTarget();
                float moveX = (target.X - CameraX) / 25;
                moveX = Math.Sign(moveX) * (float)Math.Ceiling(Math.Abs(moveX));
                float moveY = (target.Y - CameraY) / 25;
                moveY = Math.Sign(moveY) * (float)Math.Ceiling(Math.Abs(moveY));
                CameraX += moveX;
                if (Math.Sign(target.X - CameraX) != Math.Sign(moveX))
                    CameraX = target.X;
                CameraY += moveY;
                if (Math.Sign(target.Y - CameraY) != Math.Sign(moveY))
                    CameraY = target.Y;
            }
            else
            {
                bool cx = CameraX > StopScrollX;
                bool cy = CameraY > StopScrollY;
                if (AutoScrollX != 0 && CameraX != StopScrollX)
                {
                    CameraX += AutoScrollX;
                    if (CameraX > StopScrollX != cx)
                        CameraX = StopScrollX;
                }
                if (AutoScrollY != 0 && CameraY != StopScrollY)
                {
                    CameraY += AutoScrollY;
                    if (CameraY > StopScrollY != cy)
                        CameraY = StopScrollY;
                }
            }
        }

        public void CheckPlayerRoom(bool checkWarps)
        {
            if (ActivePlayer.CenterX > CurrentRoom.Right)
            {
                if (CurrentRoom.RoomRight.HasValue && checkWarps)
                    LoadRoom(CurrentRoom.RoomRight.Value.X, CurrentRoom.RoomRight.Value.Y);
                else
                    LoadCurrentRoom();
            }
            else if (ActivePlayer.CenterX < CurrentRoom.GetX)
            {
                if (CurrentRoom.RoomLeft.HasValue && checkWarps)
                    LoadRoom(CurrentRoom.RoomLeft.Value.X, CurrentRoom.RoomLeft.Value.Y);
                else
                    LoadCurrentRoom();
            }
            if (ActivePlayer.CenterY > CurrentRoom.Bottom)
            {
                if (CurrentRoom.RoomDown.HasValue && checkWarps)
                    LoadRoom(CurrentRoom.RoomDown.Value.X, CurrentRoom.RoomDown.Value.Y);
                else
                    LoadCurrentRoom();
            }
            else if (ActivePlayer.CenterY < CurrentRoom.GetY)
            {
                if (CurrentRoom.RoomUp.HasValue && checkWarps)
                    LoadRoom(CurrentRoom.RoomUp.Value.X, CurrentRoom.RoomUp.Value.Y);
                else
                    LoadCurrentRoom();
            }
        }

        public void LoadCurrentRoom()
        {
            int x = (int)Math.Floor(ActivePlayer.CenterX / Room.ROOM_WIDTH);
            int y = (int)Math.Floor(ActivePlayer.CenterY / Room.ROOM_HEIGHT);
            while (x < OffsetXRooms)
                x += WidthRooms;
            while (y < OffsetYRooms)
                y += HeightRooms;
            x = (x - OffsetXRooms) % WidthRooms + OffsetXRooms;
            y = (y - OffsetYRooms) % HeightRooms + OffsetYRooms;
            LoadRoom(x, y);
        }

        private PointF GetCameraTarget()
        {
            float targetX = ActivePlayer.CenterX + (ActivePlayer.XVelocity * 30) - (RESOLUTION_WIDTH / 2);
            float targetY = ActivePlayer.CenterY + (ActivePlayer.YVelocity * 30) - (RESOLUTION_HEIGHT / 2);
            if (heldKeys.Contains(Keys.L))
                targetX = ActivePlayer.CenterX - RESOLUTION_WIDTH / 10;
            else if (heldKeys.Contains(Keys.J))
                targetX = ActivePlayer.CenterX - RESOLUTION_WIDTH / 10 * 9;
            if (heldKeys.Contains(Keys.K))
                targetY = ActivePlayer.CenterY - RESOLUTION_HEIGHT / 10;
            else if (heldKeys.Contains(Keys.I))
                targetY = ActivePlayer.CenterY - RESOLUTION_HEIGHT / 10 * 9;
            targetX = Math.Max(Math.Min(MaxScrollX, targetX), MinScrollX);
            targetY = Math.Max(Math.Min(MaxScrollY, targetY), MinScrollY);
            return new PointF(targetX, targetY);
        }

        private void PerformAllCollisionChecks(List<Sprite> checkCollisions)
        {
            exitCollisions = false;
            bool completed = false;
            Task task = Task.Run(() =>
            {
                Thread.Sleep(20);
                if (!completed)
                    exitCollisions = true;
            });
            sprites.SortForCollisions();
            PointF[] endLocation = new PointF[checkCollisions.Count];
            for (int i = 0; i < checkCollisions.Count; i++)
            {
                Sprite drawable = checkCollisions[i];
                RectangleF startLocation = new RectangleF(drawable.X, drawable.Y, drawable.Width, drawable.Height);
                PerformCollisionChecks(drawable);
                sprites.MoveForCollisions(drawable, startLocation);
                endLocation[i] = new PointF(drawable.X, drawable.Y);
                if (exitCollisions) return;
            }
            // check again any that have moved since completing their collisions
            bool collisionPerformed;
            do
            {
                collisionPerformed = false;
                for (int i = 0; i < checkCollisions.Count; i++)
                {
                    Sprite drawable = checkCollisions[i];
                    drawable.DidCollision = false;
                    if (endLocation[i] != new PointF(drawable.X, drawable.Y))
                    {
                        collisionPerformed = true;
                        RectangleF startLocation = new RectangleF(drawable.X, drawable.Y, drawable.Width, drawable.Height);
                        PerformCollisionChecks(drawable);
                        sprites.MoveForCollisions(drawable, startLocation);
                        endLocation[i] = new PointF(drawable.X, drawable.Y);
                        if (exitCollisions) return;
                    }
                }
            } while (collisionPerformed);
            completed = true;
        }
        private PushData PerformCollisionChecks(Sprite sprite)
        {
            sprite.DidCollision = true;
            if (sprite.WasOnPlatform && !sprite.Platform.DidCollision)
            {
                PerformCollisionChecks(sprite.Platform);
            }
            List<CollisionData> entityCollisions = new List<CollisionData>();
            //Exits when there are no remaining collisions to be handled.
            while (true)
            {
                if (exitCollisions) return new PushData();
                List<Sprite> colliders = sprites.GetPotentialColliders(sprite);
                List<CollisionData> datas = new List<CollisionData>();
                for (int i = 0; i < colliders.Count; i++)
                {
                    CollisionData cd = sprite.TestCollision(colliders[i]);
                    if (cd != null && !entityCollisions.Any((a) => a.CollidedWith == cd.CollidedWith))
                    {
                        if (!(sprite.KillCrewmen && cd.CollidedWith is Crewman) && !(!cd.CollidedWith.Immovable && !cd.CollidedWith.KillCrewmen && cd.Distance == 0))
                            datas.Add(cd);
                        if (cd.CollidedWith.KillCrewmen && sprite is Crewman)
                        {
                            cd.Distance = 0;
                        }
                    }
                }
                CollisionData data = sprite.GetFirstCollision(datas);

                //If no collisions, exit loop.
                if (data is null) break;

                if (data.CollidedWith.Solid != Sprite.SolidState.Ground && !(data.CollidedWith is Crewman && sprite.Solid == Sprite.SolidState.Ground)) entityCollisions.Add(data);
                if (data.CollidedWith.Solid > Sprite.SolidState.Ground && sprite.Solid > Sprite.SolidState.Ground && !(data.CollidedWith is WarpLine))
                {
                    if (sprite is Crewman)
                        sprite.CollideWith(data);
                    else if (data.CollidedWith is Crewman) data.CollidedWith.CollideWith(new CollisionData(data.Vertical, -data.Distance, sprite));
                    continue;
                }
                PushData pd = DoCollision(sprite, data);
                if (pd.GetPushability(data) == Pushability.Immovable && pd.GetOppositePushability(data) == Pushability.Immovable)
                {
                    entityCollisions.Add(data);
                }
            };
            return sprite.FramePush;
        }

        private PushData DoCollision(Sprite sprite, CollisionData collision)
        {
            if (collision.CollidedWith is WarpLine)
            {
                collision.CollidedWith.Collide(new CollisionData(collision.Vertical, -collision.Distance, sprite));
                return sprite.FramePush;
            }
            Pushability ownPushable = sprite.FramePush.GetOppositePushability(collision);
            Pushability otherPushable = collision.CollidedWith.FramePush.GetPushability(collision);
            if (collision.CollidedWith.Solid == Sprite.SolidState.Ground && (collision.CollidedWith.Static || collision.CollidedWith.Immovable)) otherPushable = Pushability.Immovable;
            if (sprite.Pushability > ownPushable) ownPushable = sprite.Pushability;
            bool canPush;
            PushData ret = sprite.FramePush;
            PushData otherPush = collision.CollidedWith.FramePush;
            if (collision.CollidedWith.Solid == Sprite.SolidState.NonSolid) otherPushable = (Pushability)(-1);
            if (ownPushable > otherPushable)
            {
                if (collision.CollidedWith.Solid == Sprite.SolidState.Entity && sprite.Solid == Sprite.SolidState.Ground && !collision.CollidedWith.DidCollision)
                {
                    PerformCollisionChecks(collision.CollidedWith);
                }
                else
                {
                    canPush = collision.CollidedWith.CollideWith(new CollisionData(collision.Vertical, -collision.Distance, sprite));
                    if (!canPush)
                    {
                        otherPush.SetPushability(collision, Pushability.Immovable);
                        ret.SetPushability(collision, Pushability.Immovable);
                        sprite.CollideWith(collision);
                    }
                    else
                        otherPush.SetOppositePushability(collision, ownPushable);
                }
            }
            else if (otherPushable > ownPushable)
            {
                canPush = sprite.CollideWith(collision);
                if (!canPush)
                {
                    ret.SetPushability(collision, Pushability.Immovable);
                    collision.CollidedWith.CollideWith(new CollisionData(collision.Vertical, -collision.Distance, sprite));
                }
                else
                    ret.SetPushability(collision, otherPushable);
            }
            else
            {
                canPush = collision.CollidedWith.CollideWith(new CollisionData(collision.Vertical, -collision.Distance, sprite));
                if (!canPush)
                {
                    otherPush.SetOppositePushability(collision, Pushability.Immovable);
                    sprite.CollideWith(collision);
                }
                else
                    otherPush.SetOppositePushability(collision, ownPushable);
                if (canPush) collision.Distance = 0;
                canPush = sprite.CollideWith(collision);
                if (!canPush)
                {
                    ret.SetPushability(collision, Pushability.Immovable);
                    collision.CollidedWith.CollideWith(new CollisionData(collision.Vertical, -collision.Distance, sprite));
                }
                else
                    ret.SetPushability(collision, otherPushable);
            }
            sprite.FramePush = ret;
            collision.CollidedWith.FramePush = otherPush;
            return ret;
        }

        public void RoomTool(Action exit)
        {
            typing = false;
            MapLayer m = ShowMap(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
            m.OnClick = (x, y) =>
            {
                TypeText(x.ToString() + "," + y.ToString());
                textChanged?.Invoke(TypingTo.Text);
                typing = true;
                exit();
            };
            m.AllowEscape = true;
        }

        public void StopGame() { IsPlaying = false; CurrentSong?.Stop(); }

        public void ShowRoomName()
        {
            if (!hudSprites.Contains(RoomName))
            {
                hudSprites.Add(RoomName);
                hudSprites.Add(RoomNameBar);
            }
        }
        public void HideRoomName()
        {
            hudSprites.Remove(RoomName);
            hudSprites.Remove(RoomNameBar);
        }

        public void Flash(int frames, int r = 255, int g = 255, int b = 255)
        {
            if (!DoScreenEffects) return;
            flashFrames = frames;
            flashColour = Color.FromArgb(255, r, g, b);
        }
        public void Shake(int frames, int intensity = 2)
        {
            if (!DoScreenEffects) return;
            shakeFrames = frames;
            shakeIntensity = intensity;
        }
        public void AddSprite(Sprite s, float x, float y)
        {
            if (s is null) return;
            if (!sprites.Contains(s))
                sprites.Add(s);
            s.X = x + CurrentRoom.GetX;
            s.Y = y + CurrentRoom.GetY;
            //if (s is Crewman)
            //{
            //    Crewman c = s as Crewman;
            //    c.XVelocity = 0;
            //    c.YVelocity = 0;
            //    c.InputDirection = 0;
            //}
        }
        public void RemoveSprite(Sprite s)
        {
            sprites.Remove(s);
            if (s?.ActivityZone is object)
                ActivityZones.Remove(s.ActivityZone);
        }
        public bool HasSprite(Sprite s) => sprites.Contains(s);
        public void SetTilesColor(Color c)
        {
            sprites.TileColor = c;
            CurrentRoom.Color = c;
            foreach (Sprite sprite in sprites.Where((s) => s is Enemy || s is Platform))
            {
                if (sprite is Enemy)
                {
                    sprite.Color = c;
                }
            }
        }
        public void FadeOut()
        {
            FadeSpeed = -5;
            fadeHud = false;
        }
        public void FadeIn()
        {
            FadeSpeed = 5;
            fadeHud = false;
        }
        public void SetActivityZone(IActivityZone activityZone)
        {
            CurrentActivityZone = activityZone;
        }
        public void Destroy(Type type)
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite.GetType() == type)
                {
                    sprites.RemoveFromCollisions(sprite);
                    i--;
                }
            }
        }
        public Sprite[] GetAll(Predicate<Sprite> predicate)
        {
            return sprites.FindAll(predicate).ToArray();
        }
        public void HudText(string name, string text, PointF position, Color color, bool isTextBox = false)
        {
            StringDrawable sd;
            if (hudText.ContainsKey(name))
                sd = hudText[name];
            else
                sd = null;
            if (sd is null)
            {
                if (isTextBox)
                {
                    sd = new VTextBox(0, 0, FontTexture, text, color);
                    sd.Layer = 99;
                    sd.Visible = true;
                }
                else
                    sd = new StringDrawable(0, 0, FontTexture, text, color);
                sd.Name = name;
                hudText.Add(name, sd);
            }
            if (Layers.Count > 0 && Layers.Last() is MapLayer)
            {
                MapLayer ml = Layers.Last() as MapLayer;
                if (!ml.Sprites.Contains(sd))
                    ml.AddSprite(sd);
            }
            else if (!hudSprites.Contains(sd))
                hudSprites.Add(sd);
            sd.Text = text;
            sd.X = position.X;
            sd.Y = position.Y;
            sd.Color = color;
        }
        public void StartLoading(RoomGroup group)
        {
            if (loadingTask is object && !loadingTask.IsCompleted)
            {
                toLoad.Add(group);
            }
            else if (group is object && !group.Loaded)
            {
                loadingTask?.Dispose();
                loadingTask = Task.Run(() => 
                {
                    group.Load(this);
                    while (toLoad.Count > 0)
                    {
                        group = toLoad[0];
                        toLoad.RemoveAt(0);
                        group.Load(this);
                    }
                });
            }
        }

        public void HudRemove(string name)
        {
            if (hudText.ContainsKey(name))
            {
                StringDrawable sd = hudText[name];
                if (Layers.Count > 0 && Layers.Last() is MapLayer)
                {
                    MapLayer ml = Layers.Last() as MapLayer;
                    ml.RemoveSprite(sd);
                }
                hudSprites.Remove(sd);
            }
            else if (hudSpritesUser.ContainsKey(name))
            {
                if (Layers.Count > 0 && Layers.Last() is MapLayer)
                {
                    MapLayer ml = Layers.Last() as MapLayer;
                    ml.RemoveSprite(hudSpritesUser[name]);
                }
                hudSprites.Remove(hudSpritesUser[name]);
            }
            int i = 0;
            while (hudSpritesUser.ContainsKey("," + name + i.ToString()))
            {
                if (Layers.Count > 0 && Layers.Last() is MapLayer)
                {
                    MapLayer ml = Layers.Last() as MapLayer;
                    ml.RemoveSprite(hudSpritesUser["," + name + i.ToString()]);
                }
                hudSprites.Remove(hudSpritesUser["," + name + i.ToString()]);
                hudSpritesUser.Remove("," + name + i.ToString());
                i++;
            }
        }

        public void HudReplace(string name, string original, string replacewith)
        {
            if (hudText.ContainsKey(name))
            {
                StringDrawable sd = hudText[name];
                sd.Text = sd.Text.Replace(original, replacewith);
            }
        }

        public void HudSize(string name, float size)
        {
            if (hudText.ContainsKey(name))
            {
                StringDrawable sd = hudText[name];
                sd.Size = size;
            }
            else if (hudSpritesUser.ContainsKey(name))
            {
                hudSpritesUser[name].Size = size;
            }
        }

        public void IgnoreAction() => ignoreAction = true;

        public void HudSprite(string name, PointF position, Color? color = null, Animation animation = null, Texture texture = null)
        {
            if (name.StartsWith("+"))
            {
                name = "," + name.Substring(1);
                int i = 0;
                while (hudSpritesUser.ContainsKey(name + i.ToString()))
                {
                    i++;
                }
                name += i.ToString();
            }
            if (!hudSpritesUser.TryGetValue(name, out Sprite s) || (texture is object && s?.Texture != texture))
            {
                if (texture is null || animation is null) return;
                s = new Sprite(position.X, position.Y, texture, animation);
                s.Layer = 100 + hudSpritesUser.Count;
                hudSpritesUser.Remove(name);
                hudSpritesUser.Add(name, s);
            }
            if (animation is object)
            {
                s.Animation = animation;
                s.ResetAnimation();
            }
            if (color.HasValue)
                s.Color = color.Value;
            s.X = position.X;
            s.Y = position.Y;
            if (Layers.Count > 0 && Layers.Last() is MapLayer)
            {
                MapLayer ml = Layers.Last() as MapLayer;
                if (!ml.Sprites.Contains(s))
                    ml.AddSprite(s);
            }
            else
            {
                if (!hudSprites.Contains(s))
                    hudSprites.Add(s);
            }
        }

        private void glControl_Render(FrameEventArgs e)
        {
#if TEST
            // Begin frame timer
            Stopwatch t = new Stopwatch();
            t.Start();
#endif

            // Get the current time between ticks
            float progress = (float)(frameTimer.ElapsedTicks / (double)Stopwatch.Frequency * gameWindow.UpdateFrequency);
            // If tick rate == render rate, no need
            if (gameWindow.UpdateFrequency == gameWindow.RenderFrequency) progress = 1;
            // clear the color buffer
            if (EnableExtraHud)
            {
                GL.Scissor(0, 0, gameWindow.ClientSize.X, gameWindow.ClientSize.Y);
                GL.ClearColor(Color.Black);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                SpritesLayer ehLayer = Layers.LastOrDefault();
                if (ehLayer is object && ehLayer.UsesExtraHud)
                {
                    ehLayer.DrawExtraHud(Camera, ProgramID.ViewMatrixLocation);
                }
                else if (ehLayer is null)
                {

                }
                GL.Scissor((int)(hudLeft * scaleSize + xOffset), (int)(hudTop * scaleSize + yOffset), (int)(RESOLUTION_WIDTH * scaleSize), (int)(RESOLUTION_HEIGHT * scaleSize));
            }
            Color c = Color.Black;
            if (flashFrames > 0)
            {
                isFlashing = true;
                c = flashColour;
            }
            else
            {
                isFlashing = false;
                if (BGSprites is object)
                {
                    c = BGSprites.BackgroundColor;
                }
            }
            //if (c != currentColor)
            {
                currentColor = c;
                GL.ClearColor((float)c.R / 255, (float)c.G / 255, (float)c.B / 255, 1);
            }
            GL.Clear(ClearBufferMask.ColorBufferBit);

            if (!isFlashing)
            {
                int offsetX = 0;
                int offsetY = 0;
                Matrix4 cam = Camera;
                if (shakeFrames > 0)
                {
                    offsetX = r.Next(-shakeIntensity, shakeIntensity + 1);
                    offsetY = r.Next(-shakeIntensity, shakeIntensity + 1);
                }
                cam = Matrix4.CreateTranslation(-(((int)(CameraX * scaleSize) / scaleSize) + offsetX), -(((int)(CameraY * scaleSize) / scaleSize) + offsetY), 0) * cam;

                if (Layers.Count == 0 || (StartDrawing > -1 && Layers[StartDrawing].Darken < 1))
                {
                    if (isInitialized)
                    {
                        GL.Uniform1(ProgramID.MainLightLocation, MainLight);
                        GL.Uniform3(ProgramID.LightsLocation, 20, Lights);
                        GL.Uniform1(ProgramID.LightCountLocation, LightCount);
                        BGSprites?.RenderPrep(ProgramID.ViewMatrixLocation, Camera);
                        BGSprites?.Render(FrameCount);

                        GL.UseProgram(ProgramID.ID);
                        GL.UniformMatrix4(ProgramID.ViewMatrixLocation, false, ref cam);
                        sprites?.Render(FrameCount, CurrentState == GameStates.Editing, progress);
                        GL.Uniform1(ProgramID.MainLightLocation, 1.0f);
                    }
                    if (!fadeHud)
                    {
                        var camera = Camera;
                        GL.UniformMatrix4(ProgramID.ViewMatrixLocation, false, ref camera);
                        GL.Uniform4(ProgramID.ColorLocation, new Color4(0, 0, 0, (255 - FadePos) / 255f));
                        GL.Uniform4(ProgramID.MasterColorLocation, Color4.White);
                        GL.Uniform1(ProgramID.IsTextureLocation, 0);
                        RectangleSprite rs = new RectangleSprite(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                        rs.RenderPrep();
                        GL.UniformMatrix4(ProgramID.ModelLocation, false, ref rs.LocMatrix);
                        rs.UnsafeDraw();
                    }
                    hudView = Camera;
                    hudView = Matrix4.CreateTranslation(-offsetX, -offsetY, 0) * hudView;
                    GL.UniformMatrix4(ProgramID.ViewMatrixLocation, false, ref hudView);
                    hudSprites.Render(FrameCount);
                }
                if (Layers.Count > 0)
                {
                    for (int i = StartDrawing; i < Layers.Count; i++)
                    {
                        if (Layers[i].Darken > 0)
                        {
                            var camera = Camera;
                            GL.UniformMatrix4(ProgramID.ViewMatrixLocation, false, ref camera);
                            GL.Uniform4(ProgramID.ColorLocation, new Color4(0, 0, 0, Layers[i].Darken));
                            GL.Uniform4(ProgramID.MasterColorLocation, Color4.White);
                            GL.Uniform1(ProgramID.IsTextureLocation, 0);
                            RectangleSprite rs = new RectangleSprite(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                            rs.RenderPrep();
                            GL.UniformMatrix4(ProgramID.ModelLocation, false, ref rs.LocMatrix);
                            rs.UnsafeDraw();
                        }
                        cam = Camera;
                        cam = Matrix4.CreateTranslation(offsetX, offsetY, 0) * cam;
                        Layers[i].Render(cam, ProgramID.ViewMatrixLocation);
                    }
                }
                if (isLoadingLevel)
                {
                    if (levelLoadSprite is null)
                        levelLoadSprite = new StringDrawable(4, 4, FontTexture, "", Color.White);
                    levelLoadSprite.Text = levelLoadProgress;
                    levelLoadSprite.RenderPrep();
                    ProgramID.Reset();
                    int masterColorLoc = ProgramID.MasterColorLocation;
                    GL.UseProgram(ProgramID.ID);
                    GL.Uniform4(masterColorLoc, new Vector4(1f, 1f, 1f, 1f));
                    ProgramID.Prepare(levelLoadSprite, FrameCount);

                    levelLoadSprite.UnsafeDraw();
                }
                if (fadeHud)
                {
                    var camera = Camera;
                    GL.UniformMatrix4(ProgramID.ViewMatrixLocation, false, ref camera);
                    GL.Uniform4(ProgramID.ColorLocation, new Color4(0, 0, 0, (255 - FadePos) / 255f));
                    GL.Uniform4(ProgramID.MasterColorLocation, Color4.White);
                    GL.Uniform1(ProgramID.IsTextureLocation, 0);
                    RectangleSprite rs = new RectangleSprite(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                    rs.RenderPrep();
                    GL.UniformMatrix4(ProgramID.ModelLocation, false, ref rs.LocMatrix);
                    rs.UnsafeDraw();
                }
            }

#if TEST

            float ms = (float)t.ElapsedTicks / Stopwatch.Frequency * 1000f;
            t.Stop();
            rtTotal += ms;
            rtTotal -= renderTimes[rtIndex];
            renderTimes[rtIndex] = ms;
            rtIndex = (rtIndex + 1) % 60;
            if (!isLoading)
            {
                timerSprite.Text = "Avg time (render, frame): " + (rtTotal / 60).ToString("0.0") + ", " + (ftTotal / 60).ToString("0.0");
                if (rtTotal / 60 < 10 && ftTotal / 60 < 10)
                    timerSprite.Visible = false;
                else
                    timerSprite.Visible = true;
            }
#endif
            GL.Flush();
            gameWindow.SwapBuffers();
        }

        public JObject CreateSave()
        {
            JObject ret = new JObject();
            JArray jar = new JArray();
            //Sprites
            {
                foreach (Sprite sprite in UserAccessSprites.Values)
                {
                    jar.Add(sprite.Save(this, true));
                }
                ret.Add("Sprites", jar);
            }
            //Numbers
            {
                JObject jo = new JObject();
                foreach (DecimalVariable number in Vars.Values)
                {
                    jo.Add(number.Name, (float)number.AssignedValue);
                }
                ret.Add("Vars", jo);
            }
            //Trinkets
            {
                jar = new JArray();
                foreach (int trinket in CollectedTrinkets)
                {
                    jar.Add(trinket);
                }
                ret.Add("Trinkets", jar);
            }
            //Settings
            {
                ret.Add("Room", CurrentRoom.Save(this));
                ret.Add("Music", CurrentSong.Name);
                ret.Add("MapX", OffsetXRooms);
                ret.Add("MapY", OffsetYRooms);
                ret.Add("MapW", WidthRooms);
                ret.Add("MapH", HeightRooms);
            }
            return ret;
        }

        public void LoadSave(JObject loadFrom)
        {
            JArray sprs = (JArray)loadFrom["Sprites"];
            foreach (JToken sprite in sprs)
            {
                string name = (string)sprite["Name"];
                if (name is object && UserAccessSprites.ContainsKey(name))
                    UserAccessSprites[name].Load(sprite, this);
            }
            JToken nums = (JObject)loadFrom["Vars"];
            foreach (JProperty num in nums)
            {
                if (Vars.ContainsKey(num.Name))
                {
                    DecimalVariable v = Vars[num.Name] as DecimalVariable;
                    if (v is object)
                        v.Value = (float)num.Value;
                }
            }
            CollectedTrinkets.Clear();
            JArray trs = (JArray)loadFrom["Trinkets"];
            foreach (JToken tr in trs)
            {
                CollectedTrinkets.Add((int)tr);
            }
            CurrentRoom?.Dispose();
            CurrentRoom = Room.LoadRoom(loadFrom["Room"], this);
            FocusedRoom = CurrentRoom.X + CurrentRoom.Y * 100;
            MaxScrollX = CurrentRoom.Right - RESOLUTION_WIDTH;
            MaxScrollY = CurrentRoom.Bottom - RESOLUTION_HEIGHT;
            MinScrollX = CurrentRoom.GetX;
            MinScrollY = CurrentRoom.GetY;
            PointF target = GetCameraTarget();
            CameraX = target.X;
            CameraY = target.Y;
            string song = (string)loadFrom["Music"] ?? "Silence";
            OffsetXRooms = (int)(loadFrom["MapX"] ?? OffsetXRooms);
            OffsetYRooms = (int)(loadFrom["MapY"] ?? OffsetYRooms);
            WidthRooms = (int)(loadFrom["MapW"] ?? WidthRooms);
            HeightRooms = (int)(loadFrom["MapH"] ?? HeightRooms);
            if (CurrentSong.Name != song && CurrentState == GameStates.Playing)
            {
                CurrentSong.Stop();
                CurrentSong = GetMusic(song);
                CurrentSong.Play();
            }
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
            ret.Add("Music", LevelMusic.Name);
            //Scripts
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
            //Objects
            jarr = new JObject[UserAccessSprites.Count];
            for (int i = 0; i < UserAccessSprites.Count; i++)
            {
                JObject obj = UserAccessSprites.Values[i].Save(this, true);
                jarr[i] = obj;
            }
            arr = new JArray(jarr);
            ret.Add("Objects", arr);
            //Rooms
            jarr = RoomDatas.Values.ToArray();
            arr = new JArray(jarr);
            ret.Add("Rooms", arr);
            //Room Groups
            arr = new JArray();
            for (int i = 0; i < GroupList.Count; i++)
            {
                RoomGroup g = GroupList[i];
                JObject rg = new JObject();
                rg.Add("EnterScript", g.EnterScript.Name);
                rg.Add("ExitScript", g.ExitScript.Name);
                rg.Add("Name", g.Name);
                JArray rooms = new JArray();
                rooms.Add(g.X);
                rooms.Add(g.Y);
                rooms.Add(g.X + g.WidthRooms - 1);
                rooms.Add(g.Y + g.HeightRooms - 1);
                rg.Add("Rooms", rooms);
                arr.Add(rg);
            }
            ret.Add("Groups", arr);
            ret.Add("Player", ActivePlayer.Name);
            return ret;
        }

        public void NewLevel()
        {
            Scripts.Clear();
            string c = 
                "freeze\n" +
                "pausemusic\n" +
                "playsound(souleyeminijingle)\n" +
                "text(terminal,0,84,3)\n" +
                "        Congratulations!\n" +
                "\n" +
                "You have found a shiny trinket!\n" +
                "position(centerx)\n" +
                "showtext\n" +
                "text(terminal,0,132,1)\n" +
                "{c} out of {t}\n" +
                "replace({c},target:trinkets,true)\n" +
                "replace({t},?totaltrinkets,true)\n" +
                "position(centerx)\n" +
                "speak\n" +
                "endtext\n" +
                "musicfadein\n" +
                "unfreeze";
            Script tr = new Script(null, "trinket", c);
            tr.Commands = Command.ParseScript(this, c, tr);
            Scripts.Add("trinket", tr);
            WidthRooms = 5;
            HeightRooms = 5;
            RoomDatas.Clear();
            SetPlayer(new Crewman(0, 0, TextureFromName("viridian") as CrewmanTexture, this, "Viridian", textBoxColor: colors["viridian"]));
            UserAccessSprites.Clear();
            UserAccessSprites.Add("Viridian", ActivePlayer);
            StartX = 0;
            StartY = 0;
            StartRoomX = 0;
            StartRoomY = 0;
            LevelTrinkets.Clear();
            Texture platforms = TextureFromName("platforms");
            Texture sprites32 = TextureFromName("sprites32");
            Texture background = TextureFromName("background");
            {
                Backgrounds.Clear();
                BGSprites.Clear();
                BGSprites.Populate(20, new Animation[] { background.AnimationFromName("Star1s"), background.AnimationFromName("Star1") }, 1, new PointF(3, 0), true);
                BGSprites.Populate(20, new Animation[] { background.AnimationFromName("Star2s"), background.AnimationFromName("Star2") }, 1, new PointF(2, 0), true);
                BGSprites.Populate(20, new Animation[] { background.AnimationFromName("Star3s"), background.AnimationFromName("Star3") }, 1, new PointF(1, 0), true);
                BGSprites.BaseColor = Color.Gray;
                BGSprites.Name = "Outside";
                Backgrounds.Add("Outside", BGSprites.Save());
                BGSpriteCollection bgSave = new BGSpriteCollection(TextureFromName("background"), this);
                bgSave.Populate(20, new Animation[] { background.AnimationFromName("Star1s"), background.AnimationFromName("Star1") }, 1, new PointF(0, -3), true);
                bgSave.Populate(20, new Animation[] { background.AnimationFromName("Star2s"), background.AnimationFromName("Star2") }, 1, new PointF(0, -2), true);
                bgSave.Populate(20, new Animation[] { background.AnimationFromName("Star3s"), background.AnimationFromName("Star3") }, 1, new PointF(0, -1), true);
                bgSave.BaseColor = Color.Gray;
                bgSave.Name = "Warp Zone";
                Backgrounds.Add("Warp Zone", bgSave.Save());
                bgSave.Clear();
                bgSave.Populate(9, new Animation[] { background.AnimationFromName("LabH") }, 1, new PointF(4.5f, 0), false);
                bgSave.Populate(1, new Animation[] { background.AnimationFromName("LabH") }, 1, new PointF(-6.5f, 0), false);
                bgSave.Populate(5, new Animation[] { background.AnimationFromName("LabV") }, 1, new PointF(0, 4.5f), false);
                bgSave.Populate(3, new Animation[] { background.AnimationFromName("LabV") }, 1, new PointF(-0, 6.5f), false);
                bgSave.InheritRoomColor = true;
                bgSave.Name = "Lab";
                Backgrounds.Add("Lab", bgSave.Save());
                bgSave.Clear();
                bgSave.Fill(background.AnimationFromName("WarpH"));
                bgSave.MovementSpeed = new PointF(-2, 0);
                bgSave.Name = "Horizontal Warp";
                Backgrounds.Add("Horizontal Warp", bgSave.Save());
                bgSave.Clear();
                bgSave.Fill(background.AnimationFromName("WarpV"));
                bgSave.MovementSpeed = new PointF(0, -2);
                bgSave.InheritRoomColor = true;
                bgSave.Name = "Vertical Warp";
                Backgrounds.Add("Vertical Warp", bgSave.Save());
                bgSave.Clear();
                bgSave.Fill(background.AnimationFromName("WarpA"));
                bgSave.MovementSpeed = new PointF(-1.4f, -1.4f);
                bgSave.InheritRoomColor = true;
                bgSave.Name = "All Warp";
                Backgrounds.Add("All Warp", bgSave.Save());
                bgSave.Clear();
                bgSave.MovementSpeed = new PointF(0, 0);
                bgSave.Fill(background.AnimationFromName("Factory"));
                bgSave.Distribute(background.AnimationFromName("LightOn"), new PointF(32, 32), new PointF(96, 64), new Point(3, 4));
                bgSave.Distribute(background.AnimationFromName("LightOff"), new PointF(64, 32), new PointF(96, 64), new Point(3, 4));
                bgSave.Scatter(2, background.AnimationFromName("Spark1"), 2);
                bgSave.Scatter(1, background.AnimationFromName("Spark2"), 2);
                bgSave.Scatter(1, background.AnimationFromName("Spark3"), 2);
                bgSave.Scatter(3, background.AnimationFromName("Spark4"), 2);
                bgSave.Scatter(2, background.AnimationFromName("Spark5"), 2);
                bgSave.InheritRoomColor = true;
                bgSave.Name = "Factory";
                Backgrounds.Add("Factory", bgSave.Save());
            }
            LevelMusic = GetMusic("Peregrinator Homo");
            LoadRoom(0, 0);
        }

        public void LoadLevel(string path, string levelName, bool loadTextures = true)
        {
            levelLoadProgress = "Finding Level...";
            if (Editor is object)
                Editor.SaveRoom = false;
            bool isDir = true;
            if (path == "")
            {
                path = "levels";
                levelName = levelName.Substring(0, levelName.Length - 4);
                isDir = false;
            }
            levelLoadProgress = "Reading Level Data...";
            JObject loadFrom = JObject.Parse(System.IO.File.ReadAllText(path + "/" + levelName + ".lv7"));
            Scripts.Clear();
            if (System.IO.Directory.Exists(path + "/scripts") && isDir)
            {
                IEnumerable<string> scriptPaths = System.IO.Directory.EnumerateFiles(path + "/scripts");
                foreach (string sc in scriptPaths)
                {
                    string scName = sc.Split(new char[] { '/', '\\' }).Last();
                    scName = scName.Substring(0, scName.Length - 4);
                    Script script = new Script(null, scName, System.IO.File.ReadAllText(sc).Replace(Environment.NewLine, "\n"));
                    Scripts.Add(scName, script);
                }
            }
            if (loadTextures)
            {
                ResetTextures();
                LoadAllTextures();
            }
            levelLoadProgress = "Loading Music...";
            LoadAllMusic();
            //Settings
            levelLoadProgress = "Loading Level Settings...";
            WidthRooms = (int)loadFrom["Width"];
            HeightRooms = (int)loadFrom["Height"];
            int startRoomX = (int)loadFrom["StartRoomX"];
            int startRoomY = (int)loadFrom["StartRoomY"];
            int startX = (int)loadFrom["StartX"];
            int startY = (int)loadFrom["StartY"];
            string music = (string)loadFrom["Music"] ?? "";
            LevelMusic = GetMusic(music);
            StartX = startX;
            StartY = startY;
            StartRoomX = startRoomX;
            StartRoomY = startRoomY;
            LevelTrinkets.Clear();
            Warps.Clear();
            //Initialize Scripts
            {
                levelLoadProgress = "Initializing Scripts...";
                JArray scripts = (JArray)loadFrom["Scripts"];
                if (scripts is object)
                {
                    foreach (JToken script in scripts)
                    {
                        Script s = new Script(null, (string)script["Name"] ?? "", (string)script["Contents"] ?? "");
                        if (!Scripts.ContainsKey(s.Name))
                            Scripts.Add(s.Name, s);
                    }
                }
            }
            //Objects
            {
                levelLoadProgress = "Loading Level Objects...";
                UserAccessSprites.Clear();
                JArray objects = (JArray)loadFrom["Objects"];
                if (objects is object)
                {
                    foreach (JToken sprite in objects)
                    {
                        Sprite s = Sprite.LoadSprite(sprite, this);
                        if (s != null)
                            UserAccessSprites.Add(s.Name, s);
                    }
                }
            }
            //Rooms
            {
                levelLoadProgress = "Loading Rooms...";
                JArray rooms = (JArray)loadFrom["Rooms"];
                RoomDatas.Clear();
                foreach (JToken room in rooms)
                {
                    int x = (int)room["X"];
                    int y = (int)room["Y"];
                    int id = x + y * 100;
                    RoomDatas.Add(id, (JObject)room);
                    JArray arr = (JArray)room["Objects"];
                    if (arr != null)
                    {
                        foreach (JToken sprite in arr)
                        {
                            string type = (string)sprite["Type"];
                            if (type == "Trinket")
                            {
                                int trinketId = (int)(sprite["ID"] ?? 0);
                                if (LevelTrinkets.ContainsKey(trinketId))
                                    LevelTrinkets[trinketId] += 1;
                                else
                                    LevelTrinkets.Add(trinketId, 1);
                            }
                            else if (type == "WarpToken")
                            {
                                WarpToken.WarpData data = new WarpToken.WarpData(sprite, x, y);
                                int? wid = (int?)sprite["ID"];
                                if (!wid.HasValue)
                                {
                                    sprite["ID"] = wid = GetNextWarpID();
                                }
                                Warps.Add(wid.Value, data);
                            }
                        }
                    }
                }
            }
            // Map
            levelLoadProgress = "Creating Map...";
            foreach (MapSprite ms in MapSprites.Values)
            {
                ms.Dispose();
            }
            MapSprites.Clear();
            for (int i = 0; i < RoomDatas.Count; i++)
            {
                MapSprite ms = MapSprite.FromRoom(RoomDatas.Values[i], 1, this);
                MapSprites.Add(RoomDatas.Keys[i], ms);
            }
            //Load Scripts
            {
                levelLoadProgress = "Parsing Scripts...";
                for (int i = 0; i < Scripts.Count; i++)
                {
                    Script script = Scripts.Values[i];
                    string contents = script.Contents;
                    script.Commands = Command.ParseScript(this, contents, script);
                }
            }
            //Load Player
            {
                levelLoadProgress = "Loading Player...";
                JToken pl = loadFrom["Player"];
                if (pl.Type != JTokenType.Null)
                {
                    if (pl.Type == JTokenType.String)
                    {
                        string defaultPlayer = (string)loadFrom["Player"];
                        Crewman player = SpriteFromName(defaultPlayer) as Crewman;
                        if (player is object)
                            SetPlayer(player);
                    }
                    else
                    {
                        Crewman player = Sprite.LoadSprite(pl, this) as Crewman;
                        SetPlayer(player);
                    }
                    ActivePlayer.X = startX;
                    ActivePlayer.Y = startY;
                    ActivePlayer.Animation = ActivePlayer.StandingAnimation;
                }
                if (Editor is object)
                    Editor.SaveRoom = false;
                LoadRoom(startRoomX, startRoomY);
            }
            //Load Room Groups
            {
                levelLoadProgress = "Loading Room Groups...";
                RoomGroups.Clear();
                GroupList.Clear();
                JArray groups = (JArray)loadFrom["Groups"];
                if (groups is object)
                    foreach (JToken group in groups)
                    {
                        string enterScript = (string)group["EnterScript"];
                        string exitScript = (string)group["ExitScript"];
                        string name = (string)group["Name"];
                        JArray corners = (JArray)group["Rooms"];
                        if (corners.Count == 4)
                        {
                            RoomGroup newGroup = new RoomGroup(ScriptFromName(enterScript), ScriptFromName(exitScript));
                            newGroup.Name = name;
                            Point topLeft = new Point((int)corners[0], (int)corners[1]);
                            Point bottomRight = new Point((int)corners[2], (int)corners[3]);
                            for (int x = topLeft.X; x < bottomRight.X + 1; x++)
                            {
                                for (int y = topLeft.Y; y < bottomRight.Y + 1; y++)
                                {
                                    int id = y * 100 + x;
                                    if (RoomDatas.ContainsKey(id))
                                        newGroup.RoomDatas.Add(id, RoomDatas[id]);
                                    RoomGroups.Add(id, newGroup);
                                }
                            }
                            newGroup.SetSize(bottomRight.X - topLeft.X + 1, bottomRight.Y - topLeft.Y + 1);
                            newGroup.X = topLeft.X;
                            newGroup.Y = topLeft.Y;
                            GroupList.Add(newGroup);
                        }
                    }
            }
        }
    }
}