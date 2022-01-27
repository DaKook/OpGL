#define TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using Newtonsoft.Json.Linq;
using OpenTK.Input;

namespace V7
{
    public enum Pushability { PushSprite, Pushable, Solid, Immovable }
    public class Game
    {
        public EventHandler QuitGame;

        // Input
        public enum Inputs
        {
            Left,
            Right,
            Up,
            Down,
            Special,
            Jump,
            Pause,
            Kill,
            Escape,
            Count
        }
        private int[] inputs = new int[(int)Inputs.Count];
        private List<Inputs> bufferInputs = new List<Inputs>();
        private List<KeyboardKeyEventArgs> bufferKeys = new List<KeyboardKeyEventArgs>();
        private string keys = "";
        private int[] lastPressed = new int[(int)Inputs.Count];
        public Dictionary<Key, Inputs> inputMap = new Dictionary<Key, Inputs>() {
            { Key.Left, Inputs.Left }, { Key.A, Inputs.Left },
            { Key.Right, Inputs.Right }, { Key.D, Inputs.Right },
            //{ Key.Up, Inputs.Up }, { Key.W, Inputs.Up },
            //{ Key.Down, Inputs.Down }, { Key.S, Inputs.Down },
            { Key.Up, Inputs.Jump }, { Key.Down, Inputs.Jump }, { Key.Space, Inputs.Jump }, { Key.Z, Inputs.Jump }, { Key.V, Inputs.Jump }, { Key.W, Inputs.Jump }, { Key.S, Inputs.Jump },
            { Key.Enter, Inputs.Pause },
            { Key.Escape, Inputs.Escape },
            { Key.R, Inputs.Kill },
            { Key.X, Inputs.Special }, { Key.B, Inputs.Special }, { Key.RShift, Inputs.Special }
        };
        private SortedSet<Key> heldKeys = new SortedSet<Key>();
        private int mouseX = -1;
        private int mouseY = -1;
        private bool bufferMove;
        private bool justMoved;
        private bool mouseIn = false;
        private bool leftMouse = false;
        private bool rightMouse = false;
        private bool middleMouse = false;

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

        public bool IsInputActive(Inputs input)
        {
            return inputs[(int)input] != 0;
        }
        public bool IsKeyHeld(Key key) => heldKeys.Contains(key);
        private bool IsInputNew(Inputs input)
        {
            return lastPressed[(int)input] == FrameCount;
        }

        // Scripts
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
        public List<VTextBox> TextBoxes = new List<VTextBox>();
        private static Random r = new Random();
        public SortedList<string, Number> Vars = new SortedList<string, Number>();

        // OpenGL
        private GameWindow gameWindow;
        private TextureProgram program;
        private uint fbo;
        private Color currentColor;

        // Textures
        public Texture FontTexture;
        public Texture NonMonoFont;
        public Texture BoxTexture;
        public SortedList<string, Texture> Textures;
        public Texture TextureFromName(string name)
        {
            if (Textures.TryGetValue(name ?? "", out Texture ret))
                return ret;
            else
                return null;
        }
        public SortedList<string, AutoTileSettings.PresetGroup> RoomPresets = new SortedList<string, AutoTileSettings.PresetGroup>();

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
                return Music.Empty;
        }
        public Music CurrentSong;
        public Music LevelMusic;
        public Action MusicFaded;

        //Map
        private SortedList<int, MapSprite> mapSprites = new SortedList<int, MapSprite>();
        private int mapWidth;
        private int mapHeight;
        private RectangleSprite mapBorder;
        private RectangleSprite mapBG;
        private Point selectedMap;

        // Editing
        private enum Tools { Ground, Background, Spikes, Trinket, Checkpoint, Disappear, Conveyor, Platform, Enemy, GravityLine, Start, Crewman, WarpLine, WarpToken, ScriptBox, Terminal, RoomText, PushBlock, Tiles, Select, CustomSprite, Point, Attach }
        private EditorTool[] EditorTools = new EditorTool[] {
            new EditorTool('1', "Ground", "Auto Tiles connect only to the same ground tiles in the same layer.\n     Z: 3x3 brush\n     X: 5x5 brush\n     F: Fill\nHold shift to toggle brush/fill lock. Shift+C lets you set your own brush size.\n(Tip: Hold [ to lock Y-axis, and ] to lock X-axis. These also restrict the fill tool to the opposite axis.)"),
            new EditorTool('2', "Background", "Auto Tiles connect to the same tiles in the same layer, and also to solid tiles.\n     Z: 3x3 brush\n     X: 5x5 brush\n     F: Fill\nHold shift to toggle brush/fill lock. Shift+C lets you set your own brush size.\n(Tip: Hold [ to lock Y-axis, and ] to lock X-axis. These also restrict the fill tool to the opposite axis, as well as preventing tiles from connecting to solid tiles outside the axis.)"),
            new EditorTool('3', "Spikes", "Auto Tiles only connect to solid tiles.\nHold F to fill a surface with spikes.\n(Tip: Hold [ to lock Y-axis, and ] to lock X-axis.)"),
            new EditorTool('4', "Trinket", "Trinkets are collectibles and are not necessarily required. The number of collected trinkets can be accessed in scripting using \"?trinkets\", and the total amout of trinkets in the level can be accessed with \"?totaltrinkets\"."),
            new EditorTool('5', "Checkpoint", "When touched by a crewman, the crewman's respawn point is set to the checkpoint's position.\n     Z: Flip the checkpoint upside-down\n     X: Flip the checkpoint to face left"),
            new EditorTool('6', "Disappear", "Platforms that disappear when a crewman stands on them.\n     To specify length, hold shift and click-and-drag."),
            new EditorTool('7', "Conveyor", "Conveyors push any crewman standing on them in a certain direction. After placing one, press either left or right to specify the direction of the conveyor.\n     To specify length, hold shift and click-and-drag."),
            new EditorTool('8', "Platform", "Platforms that move in a certain direction. After placing one, press any direction to specify which way the platform should move.\n     To specify length, hold shift and click-and-drag.\n     Middle-click for a shortcut to set a platform's bounds."),
            new EditorTool('9', "Enemy", "Crewmen die upon touching an enemy. Enemies move in a certain direction. After placing one, press any direction to specify which way the enemy should move.\n     Middle-click for a shortcut to set an enemy's bounds."),
            new EditorTool('0', "Grav Line", "Gravity lines flip the gravity of any crewman who touches them. Click-and-drag to specify the length and orientation of a Gravity Line."),
            new EditorTool('P', "Start", "Left-click to set the start position. The player will show only while you are holding the mouse button. Hold right-click to see the start position. This will navigate to the starting room. Middle-click to set the player's position without setting the start position for playtesting purposes.\n     Z: Flip the player upside-down\n     X: Flip the player to face left"),
            new EditorTool('O', "Crewman", "Place any crewman. A crewman's texture must contain at least the following animations: Standing, Walking, and Dying.\n     Z: Flip the crewman upside-down\n     X: Flip the crewman to face left"),
            new EditorTool('I', "Warp Line", "When placed on the edge of the room, Warp Lines will warp any moving object, including crewmen, to the opposite side of the room. Click-and-drag to set the length and orientation of a Warp Line."),
            new EditorTool('U', "Warp Token", "Warp Tokens teleport crewmen to a specified location. After placing a Warp Token, you must set the output. This is done just like placing a Warp Token.\nMiddle-click a Warp Token to go to its output, or an output to go to its input."),
            new EditorTool('Y', "Script Box", "Script Boxes run a script when touched by the player, then are deactivated until the room is reloaded. Click-and-drag to set the size of a Script Box. After placing one, type the name of the script for it to run. To move/resize a Script Box, click on it while holding Shift. Middle-click a Script Box to edit its script."),
            new EditorTool('T', "Terminal", "Terminals can be activated by the player by pressing Enter. After placing one, type the name of the script for it to run.\n     Z: Flip the terminal upside-down\n     X: Flip the terminal to face left\nMiddle-click a Terminal to edit its script."),
            new EditorTool('R', "Roomtext", "Roomtext has no hitbox, and is only used to display text to the player. It can be used as warnings and guides. Click anywhere and start typing. Press Enter to confirm, or press Escape to cancel."),
            new EditorTool(';', "PushBlock", "Blocks that can be pushed by crewmen. They are affected by gravity, and can ride platforms.\n     Z: Flip the push block upside-down\n     X: Flip the push block to face left"),
            new EditorTool('-', "Tiles", "Tiles placed individually. Press Tab to open/close the tileset to select a tile. Middle-click on a tile in the room to instantly select it.\n     Z: 3x3 brush\n     X: 5x5 brush\n     F: Fill\nHold shift to toggle brush/fill lock. Shift+C lets you set your own brush size.\nUse WASD to move the selected tile.\n(Tip: Hold [ to lock Y-axis, and ] to lock X-axis. These also restrict the fill tool to the opposite axis.)"),
            new EditorTool('=', "Select", "Select multiple objects at once. Middle-click to edit a property of the selected object(s), and right-click for more options.\n     Use the Arrow Keys to move selected objects. Hold Alt to move them one pixel at a time.\n     Press Delete to delete the selected object(s).\n     Hold Control while selecting to select Tiles and Script Boxes.\n     Hold Shift while selecting to select more objects.\n     Press escape to deselect everything.\n     Use Control+A to select everything in the room."),
            new EditorTool('`', "Custom Sprite", "Places a sprite with no hitbox with any texture or animation.\n     Z: Flip the sprite upside-down\n     X: Flip the sprite to face left")
        };
        private RectangleSprite descBack;
        private StringDrawable descText;
        private int tileToolW = 1;
        private int tileToolH = 1;
        private int tileToolDefW = 1;
        private int tileToolDefH = 1;
        private Tools tool = Tools.Ground;
        private Tools prTool = Tools.Ground;
        private enum FocusOptions { Level, Tileset, Dialog, Map, ScriptEditor, Previews, TileEditor }
        private FocusOptions CurrentEditingFocus = FocusOptions.Level;
        private BoxSprite selection;
        private Point currentTile = new Point(0, 0);
        private Texture currentTexture
        {
            get
            {
                return CurrentRoom.TileTexture ?? TextureFromName("tiles");
            }
        }
        private AutoTileSettings autoTiles
        {
            get
            {
                if (tool == Tools.Background) return CurrentRoom.Background;
                else if (tool == Tools.Spikes) return CurrentRoom.Spikes;
                else return CurrentRoom.Ground;
            }
            set
            {
                if (tool == Tools.Background) CurrentRoom.Background = value;
                else if (tool == Tools.Spikes) CurrentRoom.Spikes = value;
                else CurrentRoom.Ground = value;
            }
        }
        private FullImage tileset;
        private BoxSprite tileSelection;
        private bool isEditor;
        private bool selecting;
        private bool dragging;
        private bool stillHolding;
        private List<Sprite> selectedSprites = new List<Sprite>();
        private List<BoxSprite> selectBoxes = new List<BoxSprite>();
        private PointF selectOrigin;
        private bool flipToolX;
        private bool flipToolY;
        private string defaultPlayer;
        private StringDrawable editorTool;
        bool typing = false;
        StringDrawable typingTo;
        bool singleLine = false;
        private Action<string> textChanged = null;
        private Action<bool> FinishTyping = null;
        private int tileLayer = -2;
        private Point tileScroll;
        private Texture enemyTexture;
        private string enemyAnimation = "Enemy1";
        private bool replaceTiles = false;
        private Color roomColor => CurrentRoom.Color;
        private Action<Key> GiveDirection = null;
        private Texture customSpriteTexture;
        private string customSpriteAnimation;
        private Texture pushTexture;
        private string pushAnimation = "Push";
        private WarpToken currentWarp = null;
        private Room warpRoom = null;
        private StringDrawable toolPrompt = null;
        private bool toolPromptImportant = false;
        private Tile previewTile = null;
        private RectangleSprite topEditor = null;
        private string currentLevelPath = "";
        private bool currentlyBinding = false;
        private Action<Rectangle> bindSprite = null;
        private StringDrawable roomLoc = null;
        private char prefix = 'g';
        private bool isFill => heldKeys.Contains(Key.F) || fillLock;
        private bool fillLock = false;
        private ScriptBox currentlyResizing = null;
        private RectangleSprite mapSelect;
        private MapSprite mapDragging;
        private MapSprite mapMoving;
        private Point mapOrigin;
        private bool saveRoom = true;
        private Action<Point> clickMap = null;

        private SpriteCollection previews;
        private Action<Sprite> clickPreview;
        private int previewScroll;
        private int previewMaxScroll;
        private StringHighlighter scriptEditor;
        private float seScrollX;
        private float seScrollY;
        private float seZoom = 1f;

        private bool hideToolbars;

        // Dialog
        private BoxSprite dialog;
        private StringDrawable prompt;
        private StringDrawable input;
        private string[] choices;
        private List<StringDrawable> choiceSprites = new List<StringDrawable>();
        private int selectedChoice;
        private int choiceScroll;
        private int MaxChoices = 15;
        private Action<bool> closeDialog = null;
        private RectangleSprite colorPreview;
        private bool updateColor = false;
        public static SortedDictionary<string, Color> colors = new SortedDictionary<string, Color>()
        {
            { "viridian", Color.FromArgb(164, 164, 255) }, { "vermilion", Color.FromArgb(255, 60, 60) },
            { "vitellary", Color.FromArgb(255, 255, 134) }, { "verdigris", Color.FromArgb(144, 255, 144) },
            { "victoria", Color.FromArgb(95, 95, 255) }, { "violet", Color.FromArgb(255, 134, 255) },
            { "valerie", Color.FromArgb(225, 225, 225) }, { "stigma", Color.FromArgb(195, 0, 0) },
            { "terminal", Color.FromArgb(174, 174, 174) }
        };

        // Rooms
        public Room CurrentRoom;
        public SortedList<int, JObject> RoomDatas = new SortedList<int, JObject>();
        public int FocusedRoom;
        public int WidthRooms;
        public int HeightRooms;
        public SortedList<int, RoomGroup> RoomGroups = new SortedList<int, RoomGroup>();
        public List<RoomGroup> GroupList = new List<RoomGroup>();

        public int StartX;
        public int StartY;
        public int StartRoomX;
        public int StartRoomY;

        public StringDrawable RoomName;
        public RectangleSprite RoomNameBar;

        private bool exitCollisions = false;
        bool showIndicators = true;
        private List<Sprite> indic = new List<Sprite>();

        // Threads
        private Thread gameThread;

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
        private Matrix4 camera, hudView;
        public float CameraX;
        public float CameraY;
        public float AutoScrollX;
        public float AutoScrollY;
        public float StopScrollX;
        public float StopScrollY;
        public bool AutoScroll;
        private int flashFrames;
        private bool isFlashing = false;
        private Color flashColour = Color.White;
        private int shakeFrames;
        private int shakeIntensity;
        public const int RESOLUTION_WIDTH = 320;
        public const int RESOLUTION_HEIGHT = 240;
        public float MaxScrollX;
        public float MaxScrollY;
        public float MinScrollX;
        public float MinScrollY;

        // Sprites
        private SpriteCollection sprites
        {
            get => CurrentRoom?.Objects;
        }
        public SpriteCollection hudSprites;
        public SortedList<string, StringDrawable> hudText = new SortedList<string, StringDrawable>();
        public BGSpriteCollection BGSprites;
        public SortedList<string, Sprite> UserAccessSprites = new SortedList<string, Sprite>();
        public SortedList<string, JToken> SpriteTemplates = new SortedList<string, JToken>();
        public JToken TemplateFromName(string name)
        {
            SpriteTemplates.TryGetValue(name, out JToken ret);
            return ret;
        }
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
        }
        public bool IsPlaying { get; private set; } = false;
        public int FrameCount = 1; // start at 1 so inputs aren't "new" at start
        public enum GameStates { Playing, Editing, Menu }
        public GameStates CurrentState = GameStates.Playing;
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
        public List<WarpToken.WarpData> Warps = new List<WarpToken.WarpData>();
        public Script OnPlayerDeath;
        public Script OnPlayerRespawn;
        public List<ActivityZone> ActivityZones = new List<ActivityZone>();
        private IActivityZone CurrentActivityZone;


        public int FadeSpeed;
        private bool fadeHud = false;

        public RectangleSprite CutsceneBarTop;
        public RectangleSprite CutsceneBarBottom;
        public int CutsceneBars = 0;

        // Menu
        public List<VMenuItem> MenuItems = new List<VMenuItem>();
        public int SelectedItem = 0;
        public List<StringDrawable> ItemSprites = new List<StringDrawable>();
        public Color MenuColor = Color.White;
        public Color[] MenuColors = new Color[] { Color.Cyan, Color.FromArgb(127, 0, 255), Color.Magenta, Color.Red, Color.Yellow, Color.Lime };
        public float MaxMenuWidth = 0;
        public RectangleSprite ItemSelector;
        private Action WhenFaded;

        //Context Menu
        private RectangleSprite contextBase;
        private RectangleSprite contextSelect;
        private List<VMenuItem> contextMenuItems = new List<VMenuItem>();
        private List<Sprite> contextMenuSprites = new List<Sprite>();
        private int selectedContextItem;
        private bool showingContextMenu;

        public Game(GameWindow window)
        {
            if (System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\V7"))
                System.IO.Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\V7");
            else if (System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VVVVVVV"))
                System.IO.Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VVVVVVV");
            gameWindow = window;
            InitGlProgram();
            InitOpenGLSettings();
            Textures = new SortedList<string, Texture>();
            LoadAllTextures();

            FontTexture = TextureFromName("font");
            NonMonoFont = TextureFromName("font2");
            BoxTexture = TextureFromName("box");
            loadingSprite = new StringDrawable(4, 4, FontTexture, "Loading...");
            hudSprites = new SpriteCollection();
            hudSprites.Add(loadingSprite);
            gameWindow.UpdateFrame += GameWindow_UpdateFrame;
            gameWindow.RenderFrame += glControl_Render;
            gameWindow.Resize += glControl_Resize;
            gameWindow.KeyDown += GlControl_KeyDown;
            gameWindow.KeyUp += GlControl_KeyUp;
            gameWindow.MouseMove += GlControl_MouseMove;
            gameWindow.MouseDown += GlControl_MouseDown;
            gameWindow.MouseUp += GlControl_MouseUp;
            gameWindow.MouseLeave += GlControl_MouseLeave;
            gameWindow.KeyPress += GlControl_KeyPress;
            gameWindow.MouseWheel += GlControl_MouseWheel;
            StartGame();
            Thread loadThread = new Thread(StartLoading);
            loadThread.Start();
        }

        private void GameWindow_UpdateFrame(object sender, FrameEventArgs e)
        {
            while (bufferKeys.Count > 0)
            {
                HandleKey(bufferKeys[0]);
                bufferKeys.RemoveAt(0);
            }
            if (keys.Length > 0)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    KeyPress(keys[i]);
                }
                keys = "";
            }
            for (int i = 0; i < bufferInputs.Count; i++)
            {
                inputs[(int)bufferInputs[i]]++;
                lastPressed[(int)bufferInputs[i]] = FrameCount;
            }
            bufferInputs.Clear();
            justMoved = false;
            if (bufferMove) justMoved = true;
            bufferMove = false;

            // begin frame
            if (!isLoading && isInitialized)
            {
                exitCollisions = false;
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
                }
                else if (CurrentState == GameStates.Editing)
                {
                    HandleEditingInputs();
                }
                else if (CurrentState == GameStates.Menu)
                {
                    BGSprites.Process();
                    ProcessMenu();
                }
                CurrentSong.Process();
                if (CurrentSong.isFaded && MusicFaded is object)
                {
                    Action m = MusicFaded;
                    MusicFaded = null;
                    m();
                }
                for (int i = hudSprites.Count - 1; i >= 0; i--)
                {
                    Sprite d = hudSprites[i];
                    d.Process();
                }
                if (previews is object)
                {
                    previews.SortForCollisions();
                    if (previews is object)
                    {
                        List<Sprite> spr = previews.GetPotentialColliders(mouseX, mouseY + previewScroll, 1, 1);
                        bool foundone = false;
                        foreach (Sprite sprite in previews)
                        {
                            if (sprite.Name is object && sprite.Name != "")
                            {
                                if (spr.Contains(sprite) && !foundone)
                                {
                                    sprite.AdvanceFrame();
                                    sprite.Color = Color.White;
                                    foundone = true;
                                }
                                else
                                {
                                    sprite.ResetAnimation();
                                    sprite.Color = Color.Gray;
                                }
                            }
                        }
                    }
                }
                HandleHUD();
                if (FadeSpeed > 0)
                {
                    Color fade = sprites.Color;
                    if (fade.R < 255)
                    {
                        fade = Color.FromArgb(Math.Min(fade.R + FadeSpeed, 255), Math.Min(fade.G + FadeSpeed, 255), Math.Min(fade.B + FadeSpeed, 255));
                    }
                    sprites.Color = fade;
                    BGSprites.Color = fade;
                    if (fadeHud) hudSprites.Color = fade;
                    if (fade.R == 255)
                    {
                        FadeSpeed = 0;
                        WhenFaded?.Invoke();
                    }
                }
                else if (FadeSpeed < 0)
                {
                    Color fade = BGSprites.Color;
                    if (fade.R > 0)
                    {
                        fade = Color.FromArgb(Math.Max(fade.R + FadeSpeed, 0), Math.Max(fade.G + FadeSpeed, 0), Math.Max(fade.B + FadeSpeed, 0));
                    }
                    if (sprites is object)
                        sprites.Color = fade;
                    BGSprites.Color = fade;
                    if (fadeHud) hudSprites.Color = fade;
                    if (fade.R == 0)
                    {
                        FadeSpeed = 0;
                        WhenFaded?.Invoke();
                    }
                }
                sprites?.CheckBuffer();
            }

            // end frame
            FrameCount %= int.MaxValue;
            FrameCount++;

            if (isLoading)
            {
                loadingSprite.Text = "Loading... " + ((int)percent).ToString() + "%";
            }


#if TEST
            float ms = (float)e.Time;
            ftTotal += ms;
            ftTotal -= frameTimes[FrameCount % 60];
            frameTimes[FrameCount % 60] = ms;
#endif
        }

        private void StartLoading()
        {
            InitSounds();
            InitMusic();
#if TEST
            //Texture viridian = TextureFromName("viridian");
            Texture tiles = TextureFromName("tiles");
            Texture platforms = TextureFromName("platforms");
            Texture sprites32 = TextureFromName("sprites32");
            Texture enemies = TextureFromName("enemies");
            Texture background = TextureFromName("background");
            BGSprites = new BGSpriteCollection(background, this);
            BGSprites.Scatter(200, Animation.Static(0, 5, background), 0.1f);
            BGSprites.BaseColor = Color.Gray;
            //BGSprites.Populate(2000, new Animation[] { background.AnimationFromName("Snow1"), background.AnimationFromName("Snow2"), background.AnimationFromName("Snow3"), background.AnimationFromName("Snow4") }, 1, new PointF(14f, 1f));
            Crewman.Flip1 = GetSound("jump");
            Crewman.Flip2 = GetSound("jump2");
            Crewman.Cry = GetSound("hurt");
            Platform.DisappearSound = GetSound("vanish");
            Checkpoint.ActivateSound = GetSound("save");
            Terminal.ActivateSound = GetSound("terminal");
            WarpToken.WarpSound = GetSound("teleport");
            GravityLine.Sound = GetSound("blip");

            // Editor prep

            selection = new BoxSprite(0, 0, BoxTexture, 1, 1, Color.Blue);
            //hudSprites.Add(selection);
            tileSelection = new BoxSprite(0, 0, BoxTexture, 1, 1, Color.Red);
            selection.Visible = false;
            tileset = new FullImage(0, 0, tiles) { Layer = -1 };
            editorTool = new StringDrawable(4, 4, FontTexture, "1 - Ground", Color.White);
            //hudSprites.Add(editorTool);
            customSpriteTexture = sprites32;
            customSpriteAnimation = "TerminalOff";
            toolPrompt = new StringDrawable(4, 12, FontTexture, "._.", Color.LightBlue);
            previewTile = new Tile(4, 12, tiles, 0, 0) { Layer = 1 };
            roomLoc = new StringDrawable(0, 4, FontTexture, "Room 0, 0", Color.Gray)
            {
                Right = RESOLUTION_WIDTH - 4,
                Layer = 1
            };
            topEditor = new RectangleSprite(0, 0, RESOLUTION_WIDTH, 22)
            {
                Color = Color.FromArgb(100, 0, 0, 0),
                Layer = -1
            };
            enemyTexture = enemies;
            pushTexture = sprites32;
            //hudSprites.Add(topEditor);

            //This will probably be moved somewhere else and might be customizeable per-level
            Terminal.TextBox = new VTextBox(0, 0, FontTexture, " Press ENTER to activate terminal ", Color.FromArgb(255, 130, 20))
            {
                CenterX = RESOLUTION_WIDTH / 2,
                Y = 4
            };
            hudSprites.Add(Terminal.TextBox);
            hudSprites.Add(timerSprite = new StringDrawable(8, RESOLUTION_HEIGHT - 20, TextureFromName("font2"), "TEST", Color.White));

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


            //JObject jObject = JObject.Parse(System.IO.File.ReadAllText("levels/TestLevel.lv7"));
            //LoadLevel(jObject);
            //ActivePlayer.Layer = 1;
            //WarpLine wl = new WarpLine(319, 200, 32, false, -152, 0);
            //sprites.Add(wl);
            CurrentState = GameStates.Menu;
            ItemSelector = new RectangleSprite(0, 0, 1, 1);
            isEditor = false;
            tool = Tools.Ground;
            //CurrentSong = Songs["Peregrinator Homo"];
            //CurrentSong.Play();

            //string tScr = "freeze\npausemusic\ndelay,1\nplaysound,souleyeminijingle\ntext,gray,0,0,2\n        Congratulations!\nYou have found a shiny trinket!\nposition,center\nspeak_active\nendtext\nmusicfadein\nunfreeze";
            //Script scr = new Script(null, "trinket", tScr);
            //scr.Commands = Command.ParseScript(this, tScr, scr);
            //Scripts.Add("trinket", scr);

            //WarpToken wt = new WarpToken(200, 180, sprites32, sprites32.AnimationFromName("WarpToken"), 16, 8, this, WarpToken.FlipSettings.Flip);
            //sprites.Add(wt);

#endif
            percent = 100d;
            loadingSprite.Text = "Press Action Button (Z, V, or Space)";
            isLoading = false;
        }

        private void GlControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (typing)
                keys += e.KeyChar;
        }
        private void KeyPress(char c)
        {
            switch ((int)c)
            {
                case 1:
                    typingTo.SelectionStart = 0;
                    typingTo.SelectionLength = typingTo.Text.Length;
                    typingTo.Text = typingTo.Text;
                    break;
                case 3:
                    if (typingTo.SelectionLength > 0 && typingTo.SelectionStart > -1)
                    {
                        string copy = typingTo.Text.Substring(typingTo.SelectionStart, typingTo.SelectionLength);
                        //Clipboard.SetText(copy);
                    }
                    break;
                case 8:
                    {
                        if (typingTo.SelectionStart > -1)
                        {
                            if (typingTo.SelectionLength > 0)
                            {
                                int selL = typingTo.SelectionLength;
                                typingTo.SelectionLength = 0;
                                typingTo.SelectingFromLeft = true;
                                typingTo.Text = typingTo.Text.Remove(typingTo.SelectionStart, selL);
                                textChanged?.Invoke(typingTo.Text);
                            }
                            else if (typingTo.SelectionStart > 0)
                            {
                                typingTo.SelectionStart -= 1;
                                typingTo.Text = typingTo.Text.Remove(typingTo.SelectionStart, 1);
                                textChanged?.Invoke(typingTo.Text);
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
                        textChanged?.Invoke(typingTo.Text);
                    }
                    else
                    {
                        EscapeTyping();
                        FinishTyping?.Invoke(true);
                        return;
                    }
                    break;
                case 22:
                    //if (Clipboard.ContainsText())
                    //{
                    //    TypeText(Clipboard.GetText());
                    //    textChanged?.Invoke(typingTo.Text);
                    //}
                    break;
                case 24:
                    if (typingTo.SelectionLength > 0)
                    {
                        string copy = typingTo.Text.Substring(typingTo.SelectionStart, typingTo.SelectionLength);
                        //Clipboard.SetText(copy);
                        int selL = typingTo.SelectionLength;
                        typingTo.SelectionLength = 0;
                        typingTo.SelectingFromLeft = true;
                        typingTo.Text = typingTo.Text.Remove(typingTo.SelectionStart, selL);
                        textChanged?.Invoke(typingTo.Text);
                    }
                    break;
                case 27:
                    EscapeTyping();
                    FinishTyping?.Invoke(false);
                    break;
                default:
                    TypeText(c.ToString());
                    textChanged?.Invoke(typingTo.Text);
                    break;
            }
        }

        private void EscapeTyping()
        {
            typingTo.SelectionStart = -1;
            typingTo.SelectionLength = 0;
            typingTo.SelectingFromLeft = true;
            typingTo.Text = typingTo.Text;
            typingTo = null;
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
            else if (Enum.TryParse(s, true, out KnownColor kc))
            {
                ret = Color.FromKnownColor(kc);
            }
            else
            {
                if (int.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int val))
                {
                    ret = Color.FromArgb(val);
                }
            }
            return ret;
        }

        private void TypeText(string s)
        {
            if (typingTo.SelectionStart > -1 && typingTo.SelectionStart <= typingTo.Text.Length)
            {
                if (typingTo.SelectionLength > 0)
                {
                    typingTo.Text = typingTo.Text.Remove(typingTo.SelectionStart, typingTo.SelectionLength);
                    typingTo.SelectionLength = 0;
                }
                int ss = typingTo.SelectionStart;
                typingTo.SelectionStart += s.Length;
                typingTo.Text = typingTo.Text.Insert(ss, s);
            }
            else
            {
                typingTo.SelectionStart += s.Length;
                typingTo.Text += s;
            }
        }

        private void GlControl_MouseLeave(object sender, EventArgs e)
        {
            mouseIn = false;
        }

        private void GlControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                leftMouse = false;
            else if (e.Button == MouseButton.Right)
                rightMouse = false;
            else if (e.Button == MouseButton.Middle)
                middleMouse = false;
        }

        private void GlControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                leftMouse = true;
                TriggerLeftClick();
            }
            else if (e.Button == MouseButton.Right)
            {
                rightMouse = true;
            }
            else if (e.Button == MouseButton.Middle)
                middleMouse = true;
        }

        private void TriggerLeftClick()
        {
            if (isInitialized)
            {
                if (showingContextMenu)
                {
                    if (selectedContextItem > -1)
                    {
                        contextMenuItems[selectedContextItem].Action();
                    }
                    CloseContextMenu();
                    leftMouse = false;
                    return;
                }
                if (CurrentState == GameStates.Playing && isEditor)
                {
                    List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 1, 1);
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
                else if (CurrentState == GameStates.Editing)
                {
                    if (CurrentEditingFocus == FocusOptions.Previews)
                    {
                        while (previews.IsSorting)
                            ;
                        List<Sprite> col = previews.GetPotentialColliders(mouseX, mouseY + previewScroll, 1, 1).FindAll((s) => s.Name is object && s.Name != "" && s.Color == Color.White);
                        if (col.Count > 0)
                        {
                            Action<Sprite> cp = clickPreview;
                            ExitPreviews();
                            cp?.Invoke(col[0]);
                        }
                    }
                    else if (CurrentEditingFocus == FocusOptions.ScriptEditor)
                    {
                        float x = (mouseX + seScrollX) * seZoom;
                        float y = (mouseY + seScrollY) * seZoom;
                        string[] lines = typingTo.Text.Split('\n');
                        int lineY = (int)((y - 8 * seZoom) / (8 * seZoom));
                        lineY = Math.Min(Math.Max(0, lineY), lines.Length - 1);
                        string line = lines[lineY];
                        int lineX = (int)Math.Round((x - 8 * seZoom) / (8 * seZoom));
                        lineX = Math.Min(Math.Max(0, lineX), line.Length);
                        Array.Resize(ref lines, lineY);
                        int sel = lines.Sum((s) => s.Length) + lineX + lineY;
                        typingTo.SelectionStart = sel;
                        typingTo.SelectionLength = 0;
                        typingTo.Text = typingTo.Text;
                    }
                }
                else if (CurrentState == GameStates.Menu)
                {
                    if (mouseX >= ItemSelector.X && mouseX <= ItemSelector.Right)
                    {
                        for (int i = 0; i < ItemSprites.Count; i++)
                        {
                            if (mouseY >= ItemSprites[i].Y && mouseY <= ItemSprites[i].Bottom)
                            {
                                MenuItems[i].Action();
                                leftMouse = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ExitPreviews()
        {
            CurrentEditingFocus = FocusOptions.Level;
            sprites.Color = Color.White;
            hudSprites.Visible = true;
            BGSprites.Visible = true;
            previews = null;
            leftMouse = false;
            clickPreview = null;
        }

        public void ShowMap(float x, float y, float width, float height, int mapX = 0, int mapY = 0, int mapW = -1, int mapH = -1)
        {
            if (mapW == -1) mapW = WidthRooms;
            if (mapH == -1) mapH = HeightRooms;
            mapWidth = mapW;
            mapHeight = mapH;
            float w = width / mapW;
            float h = height / mapH;
            float ofX = 0;
            float ofY = 0;
            if (w / 40 > h / 30)
            {
                w = h * 4 / 3;
                ofX = (width - w * mapW) / 2;
            }
            else
            {
                h = w * 3 / 4;
                ofY = (height - h * mapH) / 2;
            }
            for (int y1 = mapY; y1 < mapH + mapY; y1++)
            {
                for (int x1 = mapX; x1 < mapW + mapX; x1++)
                {
                    if (!mapSprites.ContainsKey(x1 + y1 * 100)) continue;
                    MapSprite ms = mapSprites[x1 + y1 * 100];
                    ms.X = ofX + x + x1 * w;
                    ms.Y = ofY + y + y1 * h;
                    ms.Layer = 25;
                    ms.SetSize(w, h);
                    hudSprites.Add(ms);
                }
            }
            if (mapBorder is null)
            {
                mapBorder = new RectangleSprite(0, 0, 0, 0);
                mapBG = new RectangleSprite(0, 0, 0, 0);
                mapBorder.Layer = 23;
                mapBG.Layer = 24;
                mapBorder.Color = Color.FromArgb(50, 50, 150);
                mapBG.Color = Color.Black;
            }
            mapBorder.X = x - 5;
            mapBorder.Y = y - 5;
            mapBorder.SetSize(width + 10, height + 10);
            mapBG.X = x + ofX;
            mapBG.Y = y + ofY;
            mapBG.SetSize(width - ofX * 2, height - ofY * 2);
            if (!hudSprites.Contains(mapBorder))
            {
                hudSprites.Add(mapBorder);
                hudSprites.Add(mapBG);
            }
        }

        public void HideMap()
        {
            foreach (MapSprite m in mapSprites.Values)
            {
                hudSprites.Remove(m);
            }
            hudSprites.Remove(mapBorder);
            hudSprites.Remove(mapBG);
            hudSprites.Remove(mapSelect);
        }

        private void GlControl_MouseMove(object sender, MouseEventArgs e)
        {
            mouseIn = true;
            bufferMove = true;
            mouseX = (int)((e.X - xOffset) / scaleSize);
            mouseY = (int)((e.Y - yOffset) / scaleSize);
            if (showingContextMenu)
            {
                int x = mouseX - (int)contextBase.X - 2;
                int y = mouseY - (int)contextBase.Y - 2;
                if (x > 0 && x < contextBase.Width - 4 && y > 0 && y < contextBase.Height - 4)
                {
                    y += (int)contextBase.Y + 2;
                    int index = 0;
                    while (y > contextMenuSprites[index].Bottom)
                    {
                        index += 1;
                    }
                    if (index < contextMenuItems.Count)
                    {
                        selectedContextItem = index;
                        if (contextSelect is null)
                        {
                            contextSelect = new RectangleSprite(0, 0, 0, 0)
                            {
                                Color = Color.FromArgb(0, 0, 127),
                                Layer = 61
                            };
                            hudSprites.Add(contextSelect);
                        }
                        contextSelect.SetSize(contextBase.Width - 4, contextMenuSprites[index].Height);
                        contextSelect.X = contextMenuSprites[index].X;
                        contextSelect.Y = contextMenuSprites[index].Y;
                        contextSelect.Visible = true;
                    }
                }
                else
                {
                    selectedContextItem = -1;
                    if (contextSelect is object)
                        contextSelect.Visible = false;
                }
            }
        }

        private void OpenScript(Script s)
        {
            float size = 1;
            //if (scaleSize > 2)
            //{
            //    size = 2 / scaleSize;
            //}
            scriptEditor = new StringHighlighter(8, 8, FontTexture, this, size);
            CurrentEditingFocus = FocusOptions.ScriptEditor;
            scriptEditor.SetBuffers2(s.Contents);
            StringDrawable sd = new StringDrawable(8, 8, FontTexture, s.Contents);
            sd.Size = size;
            scriptEditor.SetSelectionSprite(sd);
            singleLine = false;
            StartTyping(sd);
            textChanged = (str) =>
            {
                scriptEditor.SetBuffers2(str);
                checkScriptScroll();
                scriptEditor.UpdateChoices(seScrollX, seScrollY);
            };
            FinishTyping = (r) =>
            {
                s.Contents = sd.Text;
                s.Commands = Command.ParseScript(this, sd.Text);
                CurrentEditingFocus = FocusOptions.Level;
                scriptEditor = null;
            };
            checkScriptScroll();
        }

        private void OpenContextMenu(int x, int y)
        {
            if (contextMenuSprites.Count > 0)
            {
                foreach (Sprite sprite in contextMenuSprites)
                {
                    hudSprites.Remove(sprite);
                }
                contextMenuSprites.Clear();
            }
            int cy = y + 2;
            int maxw = 0;
            if (contextBase is object)
                hudSprites.Remove(contextBase);
            foreach (VMenuItem item in contextMenuItems)
            {
                StringDrawable sdi = new StringDrawable(x + 2, cy, NonMonoFont, item.Text, Color.White);
                if (sdi.Width > maxw) maxw = (int)sdi.Width;
                cy += (int)sdi.Height;
                sdi.Layer = 62;
                contextMenuSprites.Add(sdi);
                hudSprites.Add(sdi);
            }
            contextBase = new RectangleSprite(x, y, maxw + 4, cy - y + 2);
            contextBase.Layer = 60;
            contextBase.Color = Color.FromArgb(100, 100, 100);
            hudSprites.Add(contextBase);
            if (contextSelect is object && !hudSprites.Contains(contextSelect))
                hudSprites.Add(contextSelect);
            int toMoveX = 0;
            int toMoveY = 0;
            if (contextBase.Bottom > RESOLUTION_HEIGHT - 4)
            {
                toMoveY = (int)contextBase.Bottom - RESOLUTION_HEIGHT + 4;
            }
            else if (contextBase.Y < 4)
            {
                toMoveY = (int)contextBase.Y - 4;
            }
            if (contextBase.Right > RESOLUTION_WIDTH - 4)
            {
                toMoveX = (int)contextBase.Right - RESOLUTION_WIDTH + 4;
            }
            else if (contextBase.X < 4)
            {
                toMoveX = (int)contextBase.X - 4;
            }
            if (toMoveX != 0 || toMoveY != 0)
            {
                contextBase.X -= toMoveX;
                contextBase.Y -= toMoveY;
                foreach (Sprite s in contextMenuSprites)
                {
                    s.X -= toMoveX;
                    s.Y -= toMoveY;
                }
            }
            showingContextMenu = true;
        }

        private void CloseContextMenu()
        {
            if (contextMenuSprites.Count > 0)
            {
                foreach (Sprite sprite in contextMenuSprites)
                {
                    hudSprites.Remove(sprite);
                }
                contextMenuSprites.Clear();
            }
            hudSprites.Remove(contextBase);
            if (contextSelect is object)
            {
                hudSprites.Remove(contextSelect);
                contextSelect.Visible = false;
            }
            showingContextMenu = false;
        }

        //INITIALIZE
        #region "Init"
        private void InitGlProgram()
        {
            program = new TextureProgram(GLProgram.Load("shaders/v2dTexTransform.vsh", "shaders/f2dTex.fsh"));
            RectangleSprite.BaseProgram = program;

            GL.UseProgram(program.ID);
            int modelMatrixLoc = GL.GetUniformLocation(program.ID, "model");
            Matrix4 identity = Matrix4.Identity;
            GL.UniformMatrix4(modelMatrixLoc, false, ref identity);

            // origin at top-left
            camera = Matrix4.CreateScale(2f / RESOLUTION_WIDTH, -2f / RESOLUTION_HEIGHT, 1);
            camera *= Matrix4.CreateTranslation(-1, 1, 0f);
            hudView = camera;
            int viewMatrixLoc = GL.GetUniformLocation(program.ID, "view");
            GL.UniformMatrix4(viewMatrixLoc, false, ref camera);
        }

        private void LoadAllTextures()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("textures/").ToList();
            if (System.IO.Directory.Exists("levels/" + currentLevelPath + "/textures"))
            {
                files.AddRange(System.IO.Directory.EnumerateFiles("levels/" + currentLevelPath + "/textures"));
            }
            LoadTextures(files);
        }

        private void LoadTextures(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                if (file.EndsWith(".png"))
                {
                    string fName = file.Split(new char[] { '/', '\\' }).Last();
                    fName = fName.Substring(0, fName.Length - 4);

                    string dataPath = file.Substring(0, file.Length - 4) + "_data.txt";
                    if (System.IO.File.Exists(dataPath))
                    {
                        JObject jObject = JObject.Parse(System.IO.File.ReadAllText(dataPath));
                        int gridSize = (int)jObject["GridSize"];
                        int gridSize2;
                        if (jObject.ContainsKey("GridSize2"))
                            gridSize2 = (int)jObject["GridSize2"];
                        else
                            gridSize2 = gridSize;
                        Texture tex = CreateTexture(fName, file, gridSize, gridSize2);
                        if (!Textures.ContainsKey(fName))
                            Textures.Add(tex.Name, tex);

                        //TextBox
                        tex.TextBoxColor = Color.FromArgb((int)(jObject["TextBox"] ?? -1));

                        //Squeak
                        tex.Squeak = (string)jObject["Squeak"] ?? "";

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
                                            frames.Add(f);
                                    i++;
                                }
                                JArray hitbox = (JArray)anim["Hitbox"];
                                Rectangle r = hitbox.Count == 4 ? new Rectangle((int)hitbox[0], (int)hitbox[1], (int)hitbox[2], (int)hitbox[3]) : Rectangle.Empty;
                                Animation animation = new Animation(frames.ToArray(), r, tex);
                                animation.Name = (string)anim["Name"] ?? "";
                                int ls = (int)(anim["LoopStart"] ?? 0);
                                bool rand = (bool)(anim["Random"] ?? false);
                                animation.LoopStart = ls * speed;
                                animation.BaseSpeed = speed;
                                animation.Random = rand;
                                anims.Add(animation.Name, animation);
                            }
                            tex.Animations = anims;
                        }
                        else
                            tex.Animations = new SortedList<string, Animation>();

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
                        else
                            tex.TileSolidStates = new Sprite.SolidState[0, 0];

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
                        else
                            tex.CharacterWidths = new SortedList<int, int>();

                        //Auto Tiles
                        JArray aut = (JArray)jObject["RoomPresets"];
                        if (aut != null)
                        {
                            foreach (JToken group in aut)
                            {
                                string groupName = (string)group["GroupName"];
                                string backgroundName = (string)group["Background"];
                                if (!RoomPresets.ContainsKey(groupName))
                                    RoomPresets.Add(groupName, new AutoTileSettings.PresetGroup(groupName, backgroundName));
                                int groundSize = (int)group["GroundSize"];
                                Point groundSize2 = new Point(1, 1);
                                JArray s2 = (JArray)group["GroundSize2"];
                                if (s2 is object && s2.Count == 2)
                                {
                                    groundSize2 = new Point((int)s2[0], (int)s2[1]);
                                }
                                int backgroundSize = (int)group["BackgroundSize"];
                                Point backgroundSize2 = new Point(1, 1);
                                s2 = (JArray)group["GroundSize2"];
                                if (s2 is object && s2.Count == 2)
                                {
                                    backgroundSize2 = new Point((int)s2[0], (int)s2[1]);
                                }
                                int spikesSize = (int)group["SpikesSize"];
                                JArray grp = (JArray)group["Contents"];
                                if (grp is object)
                                {
                                    for (int i = 0; i < grp.Count;)
                                    {
                                        string name = (string)grp[i++];
                                        int x = (int)grp[i++];
                                        int y = (int)grp[i++];
                                        int x2 = (int)grp[i++];
                                        int y2 = (int)grp[i++];
                                        int x3 = (int)grp[i++];
                                        int y3 = (int)grp[i++];
                                        int r = (int)grp[i++];
                                        int g = (int)grp[i++];
                                        int b = (int)grp[i++];
                                        AutoTileSettings.RoomPreset preset = new AutoTileSettings.RoomPreset(
                                            new AutoTileSettings.Initializer(name, new Point(x, y), groundSize, groundSize2),
                                            new AutoTileSettings.Initializer(name, new Point(x2, y2), backgroundSize, backgroundSize2),
                                            new AutoTileSettings.Initializer(name, new Point(x3, y3), spikesSize, new Point(1, 1)),
                                            Color.FromArgb(r, g, b), tex);
                                        preset.Name = name;
                                        RoomPresets[groupName].Add(preset);
                                    }
                                }
                            }
                        }
                    }
                    else // no _data file, create with default grid size
                    {
                        Texture newTex = CreateTexture(fName, file, 32, 32);
                        if (!Textures.ContainsKey(fName))
                            Textures.Add(newTex.Name, newTex);
                        newTex.Animations = new SortedList<string, Animation>();
                    }
                }
            }
        }
        private Texture CreateTexture(string texture, string fullPath, int gridSize, int gridSize2)
        {
            //SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.Decode(fullPath);
            Bitmap bmp = new Bitmap(fullPath);
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Texture currentTex = TextureFromName(texture);
            int texa;
            int texb;
            if (currentTex is null || currentTex.TileSizeX != gridSize || currentTex.TileSizeY != gridSize2)
            {
                GL.CreateVertexArrays(1, out texa);
                GL.BindVertexArray(texa);
                GL.CreateBuffers(1, out texb);
                GL.BindBuffer(BufferTarget.ArrayBuffer, texb);
                float[] fls = new float[]
                {
                    0f,       0f,        0f, 0f,
                    0f,       gridSize2, 0f, 1f,
                    gridSize, gridSize2, 1f, 1f,
                    gridSize, 0f,        1f, 0f
                };
                GL.BufferData(BufferTarget.ArrayBuffer, fls.Length * sizeof(float), fls, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)0);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                GL.EnableVertexAttribArray(0);
                GL.EnableVertexAttribArray(1);
            }
            else
            {
                texa = currentTex.baseVAO;
                texb = currentTex.baseVBO;
                GL.BindVertexArray(texa);
                GL.BindBuffer(BufferTarget.ArrayBuffer, texb);
            }


            int tex;
            if (currentTex is null)
                GL.CreateTextures(TextureTarget.Texture2D, 1, out tex);
            else
                tex = currentTex.ID;
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 0f });

            // instancing
            GL.CreateBuffers(1, out int ibo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            float[] empty = new float[] { 0f, 0f, 0f, 0f };
            GL.BufferData(BufferTarget.ArrayBuffer, empty.Length * sizeof(float), empty, BufferUsageHint.DynamicDraw);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)0);
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribDivisor(2, 1);
            GL.VertexAttribDivisor(3, 1);

            if (currentTex is null)
                return new Texture(tex, bmp.Width, bmp.Height, gridSize, gridSize2, texture, program, texa, texb);
            else
            {
                currentTex.Update(bmp.Width, bmp.Height, gridSize, gridSize2, texa, texb);
                return currentTex;
            }
        }

        private void InitOpenGLSettings()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.One);

            glControl_Resize(null, null);

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
                se.Initialize();
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
                if (!(file.EndsWith(".ogg") || file.EndsWith(".wav"))) continue;
                Music m = new Music(file);
                m.Initialize();
                Songs.Add(m.Name, m);
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
#endif
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
#endregion
        //Dialog
#region "Dialog"
        private void CloseDialog(bool result)
        {
            choices = new string[] { };
            availableChoices = null;
            hudSprites.Remove(dialog);
            hudSprites.Remove(prompt);
            hudSprites.Remove(input);
            if (colorPreview != null && hudSprites.Contains(colorPreview))
                hudSprites.Remove(colorPreview);
            updateColor = false;
            RefreshChoices();
            CurrentEditingFocus = FocusOptions.Level;
            closeDialog?.Invoke(result);
        }
        private void ShowDialog(string prompt, string defaultResponse, string[] choices, Action<bool> action = null)
        {
            if (dialog is null)
                dialog = new BoxSprite(16, 16, TextureFromName("dialog"), 36, 26, Color.White);
            dialog.Layer = 1;
            if (this.prompt == null)
            {
                this.prompt = new StringDrawable(0, 0, NonMonoFont, "", Color.Black);
            }
            if (input == null)
            {
                input = new StringDrawable(0, 0, FontTexture, "", Color.Black);
            }
            float y = 40f;
            this.prompt.MaxWidth = RESOLUTION_WIDTH - 48;
            this.prompt.Text = prompt;
            input.Text = defaultResponse;
            this.prompt.X = 24;
            this.prompt.Y = y;
            y += this.prompt.Height + 8;
            this.prompt.Layer = 2;
            input.X = 24;
            input.Y = y;
            input.Layer = 2;
            y += 8;
            MaxChoices = (192 - (int)y) / 8;
            if (choices is null) choices = new string[] { };
            this.choices = choices;
            selection.Visible = false;
            hudSprites.Add(dialog);
            hudSprites.Add(this.prompt);
            hudSprites.Add(input);
            closeDialog = action;
            choiceScroll = 0;
            selectedChoice = -1;
            StartTyping(input);
            input.SelectionStart = 0;
            input.SelectionLength = input.Text.Length;
            input.Text = input.Text;
            singleLine = true;
            FinishTyping = (b) =>
            {
                CloseDialog(b);
            };
            CurrentEditingFocus = FocusOptions.Dialog;
            availableChoices = null;
            if (choices.Length > 0) textChanged = (s) => RefreshChoices(s);
            RefreshChoices();
        }
        private void ShowColorDialog(string prompt, string defaultResponse, Action<bool> action)
        {
            dialog = new BoxSprite(16, 16, TextureFromName("dialog"), 36, 26, Color.White);
            dialog.Layer = 1;
            if (this.prompt == null)
            {
                this.prompt = new StringDrawable(0, 0, NonMonoFont, "", Color.Black);
            }
            if (input == null)
            {
                input = new StringDrawable(0, 0, FontTexture, "", Color.Black);
            }
            if (colorPreview == null)
            {
                colorPreview = new RectangleSprite(0, 0, 256, 64);
            }
            float y = 40f;
            this.prompt.Text = prompt;
            input.Text = defaultResponse;
            input.SelectionStart = 0;
            input.SelectionLength = input.Text.Length;
            input.Text = input.Text;
            this.prompt.X = 24;
            this.prompt.Y = y;
            y += this.prompt.Height + 8;
            this.prompt.Layer = 2;
            input.X = 24;
            input.Y = y;
            input.Layer = 2;
            y += 8;
            colorPreview.X = 32;
            colorPreview.Y = y + 8;
            colorPreview.Layer = 2;
            selection.Visible = false;
            hudSprites.Add(dialog);
            hudSprites.Add(this.prompt);
            hudSprites.Add(input);
            hudSprites.Add(colorPreview);
            closeDialog = action;
            choiceScroll = 0;
            selectedChoice = -1;
            availableChoices = null;
            choices = new string[] { };
            RefreshChoices();
            StartTyping(input);
            singleLine = true;
            updateColor = true;
            FinishTyping = (b) =>
            {
                CloseDialog(b);
            };
            CurrentEditingFocus = FocusOptions.Dialog;
        }
        private void RefreshChoices(string s = null)
        {
            foreach (StringDrawable choice in choiceSprites)
            {
                hudSprites.Remove(choice);
            }
            choiceSprites.Clear();
            if (s is object)
            {
                availableChoices = choices.ToList();
                availableChoices.RemoveAll((c) => !c.ToLower().Contains(s.ToLower()));
                int v = 0;
                for (int i = 0; i < availableChoices.Count; i++)
                {
                    if (availableChoices[i].StartsWith(s))
                    {
                        v = i;
                        break;
                    }
                }
                selectedChoice = v;
            }
            DisplayChoices();
        }
        private List<string> availableChoices;
        private void DisplayChoices()
        {
            if (availableChoices is null) availableChoices = choices.ToList();
            int y = 192 - (MaxChoices * 8);
            for (int i = choiceScroll; i < choiceScroll + MaxChoices && i < availableChoices.Count; i++)
            {
                StringDrawable ch = new StringDrawable(24, y, FontTexture, availableChoices[i], i == selectedChoice ? Color.Blue : Color.Black);
                ch.Layer = 2;
                hudSprites.Add(ch);
                choiceSprites.Add(ch);
                y += 8;
            }
        }
#endregion

        //MAIN MANU
#region "Main Menu"
        private void MainMenu()
        {
            MenuItems.Clear();
            SelectedItem = 0;
            MenuItems.Add(new VMenuItem("Play Game", () =>
            {
                GetSound("hurt")?.Play();
                VTextBox tb = new VTextBox(0, 4, FontTexture, "This option is not\n  yet available!  ", Color.Gray);
                tb.Markers.Add(15, 1);
                tb.Markers.Add(18, 0);
                tb.Text = tb.Text;
                tb.CenterX = RESOLUTION_WIDTH / 2;
                tb.frames = 200;
                hudSprites.Add(tb);
                tb.Disappeared += (t) => { hudSprites.Remove(t); };
                tb.Appear();
            }));
            MenuItems.Add(new VMenuItem("Player Levels", () => {
                GetSound("crew1")?.Play();
                PlayerLevelsMenu();
            }));
            MenuItems.Add(new VMenuItem("Options", () => {
                GetSound("hurt")?.Play();
                VTextBox tb = new VTextBox(0, 4, FontTexture, "This option is not\n  yet available!  ", Color.Gray);
                tb.CenterX = RESOLUTION_WIDTH / 2;
                tb.frames = 200;
                hudSprites.Add(tb);
                tb.Disappeared += (t) => { hudSprites.Remove(t); };
                tb.Appear();
            }));
            MenuItems.Add(new VMenuItem("Credits", () => {
                GetSound("crew1")?.Play();
                VTextBox tb = new VTextBox(0, 4, FontTexture, "Made by DaKook. More credits\n     to come later.", Color.Gray);
                tb.CenterX = RESOLUTION_WIDTH / 2;
                tb.frames = 200;
                hudSprites.Add(tb);
                tb.Disappeared += (t) => { hudSprites.Remove(t); };
                tb.Appear();
            }));
            MenuItems.Add(new VMenuItem("Exit Game", () =>
            {
                QuitGame?.Invoke(this, new EventArgs());
            }));
            UpdateMenu();
        }

        private void PlayerLevelsMenu()
        {
            MenuItems.Clear();
            SelectedItem = 0;
            MenuItems.Add(new VMenuItem("Play a Level", () =>
            {

            }));
            MenuItems.Add(new VMenuItem("Level Editor", () =>
            {
                fadeHud = true;
                FadeSpeed = -5;
                CurrentSong.FadeOut();
                WhenFaded = () =>
                {
                    ClearMenu();
                    NewLevel();
                    CurrentState = GameStates.Editing;
                    isEditor = true;
                    FadeSpeed = 5;
                    WhenFaded = () => { fadeHud = false; };
                };
            }));
            MenuItems.Add(new VMenuItem("Back", () =>
            {
                GetSound("crew1")?.Play();
                MainMenu();
            }));
            UpdateMenu();
        }

        private void LevelEditorMenu()
        {
            MenuItems.Clear();
            SelectedItem = 0;
            MenuItems.Add(new VMenuItem("Set Level Size", () =>
            {
                ClearMenu();
                CurrentState = GameStates.Editing;
                ShowDialog("Level Size (Format: x, y)", WidthRooms.ToString() + ", " + HeightRooms.ToString(), null, (r) =>
                {
                    if (r)
                    {
                        int w = 0;
                        int h = 0;
                        string[] s = input.Text.Split(new char[] { ',', 'x', 'X' });
                        if (int.TryParse(s.First(), out w))
                        {
                            WidthRooms = Math.Max(Math.Min(w, 100), 1);
                            if (int.TryParse(s.Last(), out h))
                            {
                                HeightRooms = Math.Max(Math.Min(h, 100), 1);
                            }
                        }
                    }
                    CurrentState = GameStates.Menu;
                    LevelEditorMenu();
                });
            }));
            MenuItems.Add(new VMenuItem("Set Music", () =>
            {
                ClearMenu();
                CurrentState = GameStates.Editing;
                List<string> ch = new List<string>();
                foreach (Music m in Songs.Values)
                {
                    ch.Add(m.Name);
                }
                ShowDialog("Change music", LevelMusic.Name, ch.ToArray(), (r) =>
                {
                    if (r)
                    {
                        Music m = GetMusic(input.Text);
                        if (m is object)
                            LevelMusic = m;
                    }
                    CurrentState = GameStates.Menu;
                    LevelEditorMenu();
                });
            }));
            MenuItems.Add(new VMenuItem("Misc. Options", () =>
            {
                MiscOptions();
            }));
            MenuItems.Add(new VMenuItem("Back", () =>
            {
                ClearMenu();
                sprites.Color = Color.White;
                CurrentState = GameStates.Editing;
                BGSprites.BaseColor = Color.Gray;
                BGSprites.Visible = true;
            }));
            UpdateMenu();
        }

        private void MiscOptions()
        {
            MenuItems.Clear();
            SelectedItem = 0;
            MenuItems.Add(new VMenuItem("Lose Trinkets: " + (LoseTrinkets ? "On" : "Off"), () =>
            {
                ClearMenu();
                CurrentState = GameStates.Editing;
                ShowDialog("Lose trinkets? When this option is on, the player will lose any trinkets collected since the last checkpoint on death. This means the player will have to get safely to a checkpoint after collecting a trinket in order to keep it.", LoseTrinkets ? "On" : "Off", new string[] { "On", "Off" }, (r) =>
                {
                    if (r)
                    {
                        if (!bool.TryParse(input.Text, out bool lose))
                        {
                            lose = input.Text.ToLower() == "on";
                        }
                        LoseTrinkets = lose;
                    }
                    CurrentState = GameStates.Menu;
                    MiscOptions();
                });
            }));
            MenuItems.Add(new VMenuItem("Back", () => { LevelEditorMenu(); }));
            UpdateMenu();
        }

        private void ClearMenu()
        {
            MenuItems.Clear();
            UpdateMenu();
            hudSprites.Remove(ItemSelector);
        }
#endregion

        //TEXTURE EDITOR
#region TextureEditor
        private void SelectTexture()
        {
            ShowDialog("Choose a texture to modify.", "", Textures.Keys.ToArray(), (r) =>
            {
                if (r)
                {
                    Texture t = TextureFromName(input.Text);
                    if (t is object)
                    {
                        OpenTextureEditor(t);
                    }
                }
            });
        }

        private void OpenTextureEditor(Texture texture)
        {
            MenuItems.Clear();
            MenuItems.Add(new VMenuItem("Set Tile Size", () =>
            {

            }));
            MenuItems.Add(new VMenuItem("Set Tile Types", () =>
            {

            }));
        }
#endregion

        float scaleSize = 1;
        float xOffset = 0;
        float yOffset = 0;
        private void glControl_Resize(object sender, EventArgs e)
        {
            float relX = (float)gameWindow.Width / RESOLUTION_WIDTH;
            float relY = (float)gameWindow.Height / RESOLUTION_HEIGHT;
            scaleSize = (int)Math.Min(relX, relY);
            int w = (int)(RESOLUTION_WIDTH * scaleSize);
            int h = (int)(RESOLUTION_HEIGHT * scaleSize);
            xOffset = (gameWindow.Width - w) / 2;
            yOffset = (gameWindow.Height - h) / 2;
            GL.Viewport((int)xOffset, (int)yOffset, w, h);
        }
        //   _  __________     __  _____   ______          ___   _   //
        //  | |/ /  ____\ \   / / |  __ \ / __ \ \        / / \ | |  //
        //  | ' /| |__   \ \_/ /  | |  | | |  | \ \  /\  / /|  \| |  //
        //  |  < |  __|   \   /   | |  | | |  | |\ \/  \/ / | . ` |  //
        //  | . \| |____   | |    | |__| | |__| | \  /\  /  | |\  |  //
        //  |_|\_\______|  |_|    |_____/ \____/   \/  \/   |_| \_|  //

        private void GlControl_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            bufferKeys.Add(e);
            if (e.Key == Key.Enter && typing)
            {
                keys += '\n';
            }
            else if (e.Key == Key.Escape && typing)
            {
                keys += (char)27;
            }
            if (e.Key == Key.F9) Screenshot();
            if (inputMap.ContainsKey(e.Key) && !heldKeys.Contains(e.Key))
            {
                bufferInputs.Add(inputMap[e.Key]);
            }
            if (!heldKeys.Contains(e.Key))
                heldKeys.Add(e.Key);
        }

        private void HandleKey(KeyboardKeyEventArgs e)
        {
            if (!isInitialized && !isLoading)
            {
                if (inputMap.ContainsKey(e.Key) && inputMap[e.Key] == Inputs.Jump)
                {
                    Shake(40, 2);
                    Flash(10);
                    GetSound("gamesaved").Play();
                    CurrentSong = GetMusic("Regio Pelagus");
                    CurrentSong.Play();
                    hudSprites.Remove(loadingSprite);
                    MainMenu();
                    isInitialized = true;
                }
                return;
            }
            if (isLoading) return;
            if (typing)
            {
                if (e.Key == Key.Right)
                {
                    if (e.Shift)
                    {
                        if (typingTo.SelectingFromLeft)
                        {
                            if (typingTo.SelectionStart + typingTo.SelectionLength < typingTo.Text.Length)
                            {
                                typingTo.SelectionLength += 1;
                            }
                        }
                        else
                        {
                            typingTo.SelectionLength -= 1;
                            typingTo.SelectionStart += 1;
                            if (typingTo.SelectionLength == 0)
                                typingTo.SelectingFromLeft = true;
                        }
                    }
                    else
                    {
                        if (typingTo.SelectionLength > 0)
                        {
                            typingTo.SelectionStart += typingTo.SelectionLength;
                            typingTo.SelectionLength = 0;
                            typingTo.SelectingFromLeft = true;
                        }
                        else
                        {
                            if (typingTo.SelectionStart < typingTo.Text.Length)
                            {
                                typingTo.SelectionStart += 1;
                            }
                        }
                    }
                    typingTo.Text = typingTo.Text;
                }
                else if (e.Key == Key.Left)
                {
                    if (e.Shift)
                    {
                        if (typingTo.SelectingFromLeft && typingTo.SelectionLength > 0)
                        {
                            typingTo.SelectionLength -= 1;
                        }
                        else
                        {
                            if (typingTo.SelectionStart > 0)
                            {
                                typingTo.SelectionLength += 1;
                                typingTo.SelectionStart -= 1;
                                typingTo.SelectingFromLeft = false;
                            }
                        }
                    }
                    else
                    {
                        if (typingTo.SelectionLength > 0)
                        {
                            typingTo.SelectionLength = 0;
                            typingTo.SelectingFromLeft = true;
                        }
                        else
                        {
                            if (typingTo.SelectionStart > 0)
                            {
                                typingTo.SelectionStart -= 1;
                            }
                        }
                    }
                    typingTo.Text = typingTo.Text;
                }
                if (!singleLine && !(CurrentEditingFocus == FocusOptions.ScriptEditor && scriptEditor.ChoicesVisible))
                {
                    if (e.Key == Key.Up)
                    {
                        int curLine = 0;
                        int index = 0;
                        List<int> lineStarts = new List<int>();
                        while (index <= typingTo.SelectionStart && index > -1 && index < typingTo.Text.Length)
                        {
                            lineStarts.Add(index == 0 ? -1 : index);
                            index = typingTo.Text.IndexOf('\n', index + 1);
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
                            int cv = typingTo.SelectionStart - lineStarts[curLine];
                            v = Math.Min(lineStarts[curLine], lineStarts[curLine - 1] + cv);
                        }
                        typingTo.SelectionStart = Math.Max(v, 0);
                        typingTo.SelectionLength = 0;

                        typingTo.Text = typingTo.Text;
                    }
                    else if (e.Key == Key.Down)
                    {
                        int curLine = 0;
                        int index = 0;
                        List<int> lineStarts = new List<int>();
                        while (index != -1 && index < typingTo.Text.Length)
                        {
                            lineStarts.Add(index == 0 ? -1 : index);
                            bool ind = index <= typingTo.SelectionStart;
                            index = typingTo.Text.IndexOf('\n', index + 1);
                            if (index > -1 && ind)
                                curLine++;
                        }
                        curLine--;
                        if (curLine < 0) curLine = 0;
                        int v;
                        if (curLine == lineStarts.Count - 1)
                        {
                            v = typingTo.Text.Length;
                        }
                        else
                        {
                            int cv = typingTo.SelectionStart - lineStarts[curLine];
                            v = Math.Min(lineStarts.Count > curLine + 2 ? lineStarts[curLine + 2] : typingTo.Text.Length, lineStarts[curLine + 1] + cv);
                        }
                        typingTo.SelectionStart = v;
                        typingTo.SelectionLength = 0;

                        typingTo.Text = typingTo.Text;
                    }
                }
                else if (CurrentEditingFocus == FocusOptions.Dialog)
                {
                    if (e.Key == Key.Down && availableChoices is object && availableChoices.Count > 0)
                    {
                        selectedChoice += 1;
                        if (selectedChoice >= availableChoices.Count)
                        {
                            selectedChoice = 0;
                            choiceScroll = 0;
                        }
                        if (selectedChoice > choiceScroll + MaxChoices)
                        {
                            choiceScroll = selectedChoice - (MaxChoices - 1);
                        }
                        string s = availableChoices[selectedChoice];
                        input.SelectionStart = s.Length;
                        input.SelectionLength = 0;
                        input.SelectingFromLeft = true;
                        input.Text = s;
                        RefreshChoices();
                    }
                    else if (e.Key == Key.Up && availableChoices is object && availableChoices.Count > 0)
                    {
                        selectedChoice -= 1;
                        if (selectedChoice < 0)
                        {
                            selectedChoice = availableChoices.Count - 1;
                            choiceScroll = Math.Max(0, availableChoices.Count - MaxChoices);
                        }
                        if (selectedChoice < choiceScroll)
                        {
                            choiceScroll = selectedChoice;
                        }
                        string s = availableChoices[selectedChoice];
                        input.SelectionStart = s.Length;
                        input.SelectionLength = 0;
                        input.SelectingFromLeft = true;
                        input.Text = s;
                        RefreshChoices();
                    }
                    else if (e.Key == Key.Tab && availableChoices is object && availableChoices.Count > 0)
                    {
                        string s = availableChoices[selectedChoice];
                        input.SelectionStart = s.Length;
                        input.SelectionLength = 0;
                        input.SelectingFromLeft = true;
                        input.Text = s;
                        RefreshChoices();
                    }
                }
                if (CurrentEditingFocus == FocusOptions.ScriptEditor)
                {
                    if (e.Control && e.Key == Key.R)
                    {
                        typing = false;
                        ShowMap(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                        CurrentEditingFocus = FocusOptions.Map;
                        clickMap = (p) =>
                        {
                            CurrentEditingFocus = FocusOptions.ScriptEditor;
                            TypeText(p.X.ToString() + "," + p.Y.ToString());
                            textChanged?.Invoke(typingTo.Text);
                            typing = true;
                        };
                    }
                    else if (e.Control && e.Key == Key.P)
                    {
                        CurrentEditingFocus = FocusOptions.Level;
                        tool = Tools.Point;
                        editorTool.Text = "- Select Point -";
                        toolPrompt.Text = "Click a point...";
                        hudSprites.Remove(previewTile);
                        typing = false;
                    }
                    if (e.Key == Key.Right || e.Key == Key.Left || e.Key == Key.Up || e.Key == Key.Down)
                    {
                        checkScriptScroll();
                        if (!scriptEditor.ShowingChoices)
                            scriptEditor.ShowChoices(seScrollX, seScrollY);
                        scriptEditor.UpdateChoices(seScrollX, seScrollY);
                    }
                    if (e.Key == Key.Tab)
                    {
                        scriptEditor.CompleteChoice();
                    }
                    if (scriptEditor.ChoicesVisible)
                    {
                        if (e.Key == Key.Up)
                        {
                            scriptEditor.SelectedChoice -= 1;
                            if (scriptEditor.SelectedChoice < 0)
                                scriptEditor.SelectedChoice = scriptEditor.Choices.Count - 1;
                            scriptEditor.UpdateChoices(seScrollX, seScrollY);
                        }
                        else if (e.Key == Key.Down)
                        {
                            scriptEditor.SelectedChoice += 1;
                            if (scriptEditor.SelectedChoice >= scriptEditor.Choices.Count)
                                scriptEditor.SelectedChoice = 0;
                            scriptEditor.UpdateChoices(seScrollX, seScrollY);
                        }
                        else if (e.Key == Key.Escape)
                        {
                            scriptEditor.HideChoices();
                        }
                    }
                }
                return;
            }
            else if (showingContextMenu)
            {
                if (e.Key == Key.Enter && selectedContextItem > -1 && selectedContextItem < contextMenuItems.Count)
                {
                    contextMenuItems[selectedContextItem].Action();
                    CloseContextMenu();
                }
                else if (e.Key == Key.Escape)
                {
                    CloseContextMenu();
                }
                return;
            }
            if (CurrentState == GameStates.Editing)
            {
                if (CurrentEditingFocus == FocusOptions.Level)
                {
                    if (tool == Tools.Point)
                    {
                        if (e.Key == Key.Escape)
                        {
                            tool = prTool;
                            EditorTool t = EditorTools[(int)prTool];
                            editorTool.Text = t.Hotkey.ToString() + " - " + t.Name;
                            CurrentEditingFocus = FocusOptions.ScriptEditor;
                            typing = true;
                        }
                        return;
                    }
                    if (currentlyBinding)
                    {
                        if (e.Key == Key.Enter)
                        {
                            bindSprite?.Invoke(Rectangle.Empty);
                            currentlyBinding = false;
                            toolPromptImportant = false;
                        }
                        else if (e.Key == Key.Escape)
                        {
                            currentlyBinding = false;
                            toolPromptImportant = false;
                        }
                        return;
                    }
                    if (e.Control && e.Key == Key.S)
                    {
                        string p = "levels/" + currentLevelPath + "/" + currentLevelPath + ".lv7";
                        if (!System.IO.File.Exists(p))
                        {
                            List<string> files = System.IO.Directory.EnumerateFiles("levels").ToList();
                            files.RemoveAll((s) => !s.EndsWith(".lv7"));
                            files = files.ConvertAll((s) =>
                            {
                                s = s.Split('\\').Last();
                                s = s.Substring(0, s.Length - 4);
                                return s;
                            });
                            files.Sort();
                            ShowDialog("Level name:", currentLevelPath, files.ToArray(), (r) =>
                            {
                                if (r)
                                {
                                    currentLevelPath = input.Text;
                                    p = "levels/" + currentLevelPath + ".lv7";
                                    string sv = Newtonsoft.Json.JsonConvert.SerializeObject(SaveLevel());
                                    System.IO.File.WriteAllText(p, sv);
                                    currentLevelPath = input.Text;
                                }
                                else
                                    return;
                            });
                        }
                        else
                        {
                            string str = Newtonsoft.Json.JsonConvert.SerializeObject(SaveLevel());
                            System.IO.File.WriteAllText(p, str);
                        }
                        return;
                    }
                    else if (e.Control && e.Key == Key.O)
                    {
                        IEnumerable<string> dirs = System.IO.Directory.EnumerateDirectories("levels");
                        List<string> files = new List<string>();
                        foreach (string dir in dirs)
                        {
                            string s = dir.Split('\\').Last();
                            if (System.IO.File.Exists(dir + "\\" + s + ".lv7"))
                                files.Add(s);
                        }
                        files.Sort();
                        ShowDialog("Load level...", currentLevelPath, files.ToArray(), (r) =>
                        {
                            if (r)
                            {
                                if (System.IO.File.Exists("levels/" + input.Text + "/" + input.Text + ".lv7"))
                                {
                                    currentLevelPath = input.Text;
                                    LoadLevel("levels/" + input.Text, input.Text);
                                }
                                else
                                    NewLevel();
                            }
                        });
                        return;
                    }
                    else if (e.Control && e.Key == Key.N)
                    {
                        NewLevel();
                        currentLevelPath = "";
                    }
                    else if (e.Shift && e.Key == Key.S)
                    {
                        OpenScripts();
                        return;
                    }
                    else if (e.Key == Key.Q)
                    {
                        bool shift = e.Shift;
                        string[] scriptNames = new string[Scripts.Count];
                        for (int i = 0; i < scriptNames.Length; i++)
                        {
                            scriptNames[i] = Scripts.Values[i].Name;
                        }
                        string message = shift ? "Room Exit Script?" : "Room Enter Script?";
                        string name = shift ? CurrentRoom.ExitScript?.Name ?? "" : CurrentRoom.EnterScript?.Name ?? "";
                        ShowDialog(message, name, scriptNames, (r) =>
                        {
                            if (r)
                            {
                                Script s = ScriptFromName(input.Text);
                                if (s is null)
                                {
                                    s = new Script(new Command[] { }, input.Text, "");
                                    Scripts.Add(s.Name, s);
                                }
                                if (shift)
                                    CurrentRoom.ExitScript = s;
                                else
                                    CurrentRoom.EnterScript = s;
                            }
                        });
                    }
                    else if (e.Key == Key.F1)
                    {
                        List<string> textureNames = new List<string>();
                        foreach (AutoTileSettings.PresetGroup grp in RoomPresets.Values)
                        {
                            textureNames.Add(grp.Name);
                        }
                        ShowDialog("Change room tileset?", CurrentRoom.GroupName ?? "", textureNames.ToArray(), (r) =>
                        {
                            if (r)
                            {
                                string answer = input.Text;
                                AutoTileSettings.PresetGroup g = RoomPresets[answer];
                                int ind = 0;
                                if (RoomPresets.ContainsKey(CurrentRoom.GroupName ?? ""))
                                    ind = RoomPresets[CurrentRoom.GroupName].IndexOfKey(CurrentRoom.PresetName ?? "");
                                ind %= g.Count;
                                if (ind == -1) ind = 0;
                                AutoTileSettings.RoomPreset p = g.Values[ind];
                                Texture t = p.Texture;
                                if (t != null && CurrentRoom.TileTexture != t)
                                {
                                    foreach (Sprite tile in sprites)
                                    {
                                        if (tile is Tile)
                                        {
                                            (tile as Tile).ChangeTexture(t);
                                        }
                                    }
                                    CurrentRoom.TileTexture = t;
                                }
                                CurrentRoom.UsePreset(p, g.Name);
                                replaceTiles = true;
                            }
                        });
                    }
                    else if (e.Key == Key.F2)
                    {
                        List<string> textureNames = new List<string>();
                        if (!RoomPresets.ContainsKey(CurrentRoom.GroupName ?? "")) CurrentRoom.GroupName = "Space Station";
                        PreparePreviewScreen();
                        int x = 20;
                        int y = 20;
                        float maxHeight = 0;
                        foreach (AutoTileSettings.RoomPreset grp in RoomPresets[CurrentRoom.GroupName].Values)
                        {
                            AutoTileSettings.Initializer init = grp.Ground;
                            Sprite s = new Sprite(x, y, grp.Texture, init.Origin.X, init.Origin.Y);
                            switch (init.Size)
                            {
                                case 3:
                                    s.ExtendTexture(3, 1);
                                    break;
                                case 4:
                                    s.ExtendTexture(4, 1);
                                    break;
                                case 13:
                                    s.ExtendTexture(3, 5);
                                    break;
                                case 47:
                                    s.ExtendTexture(8, 6);
                                    break;
                            }
                            if (s.Height > maxHeight) maxHeight = s.Height;
                            if (s.Right > RESOLUTION_WIDTH - 20)
                            {
                                y += (int)s.Height + 16;
                                x = 20;
                                s.X = x;
                                s.Y = y;
                            }
                            StringDrawable name = new StringDrawable(x, y + s.Height + 2, NonMonoFont, init.Name, Color.White);
                            x += (int)s.Width + 8;
                            s.Name = grp.Name;
                            previews.Add(s);
                            previews.Add(name);
                        }
                        clickPreview = (s) =>
                        {
                            ChangeRoomColour(s.Name);
                        };
                        previewMaxScroll = Math.Max(y + (int)maxHeight + 20 - RESOLUTION_HEIGHT, 0);
                        
                    }
                    else if (e.Key == Key.F3)
                    {
                        LoadAllTextures();
                        VTextBox tb = new VTextBox(0, 0, FontTexture, "Reloaded Textures", Color.Gray);
                        tb.CenterX = RESOLUTION_WIDTH / 2;
                        tb.CenterY = RESOLUTION_HEIGHT / 2;
                        tb.Layer = 100;
                        tb.frames = 75;
                        tb.Disappeared += (t) => hudSprites.Remove(t);
                        hudSprites.Add(tb);
                        tb.Appear();
                    }
                    else if (e.Key == Key.F11)
                    {
                        hideToolbars = !hideToolbars;
                    }
                    if (selectedSprites.Count == 0)
                    {
                        if (GiveDirection is null)
                        {
                            if (e.Control)
                            {
                                if (e.Key == Key.Right)
                                {
                                    ShowMap(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                                    CurrentEditingFocus = FocusOptions.Map;
                                    clickMap = (p) => CurrentRoom.RoomRight = p;
                                }
                                else if (e.Key == Key.Left)
                                {
                                    ShowMap(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                                    CurrentEditingFocus = FocusOptions.Map;
                                    clickMap = (p) => CurrentRoom.RoomLeft = p;
                                }
                                else if (e.Key == Key.Up)
                                {
                                    ShowMap(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                                    CurrentEditingFocus = FocusOptions.Map;
                                    clickMap = (p) => CurrentRoom.RoomUp = p;
                                }
                                else if (e.Key == Key.Down)
                                {
                                    ShowMap(0, 0, RESOLUTION_WIDTH, RESOLUTION_HEIGHT);
                                    CurrentEditingFocus = FocusOptions.Map;
                                    clickMap = (p) => CurrentRoom.RoomDown = p;
                                }
                            }
                            else
                            {
                                if (e.Key == Key.Right)
                                {
                                    RoomDatas[FocusedRoom] = CurrentRoom.Save(this);
                                    if (e.Shift && CurrentRoom.X == WidthRooms - 1 && WidthRooms < 100)
                                    {
                                        WidthRooms += 1;
                                    }
                                    LoadRoom((CurrentRoom.X + 1) % WidthRooms, CurrentRoom.Y);
                                }
                                else if (e.Key == Key.Left)
                                {
                                    RoomDatas[FocusedRoom] = CurrentRoom.Save(this);
                                    int x = CurrentRoom.X - 1;
                                    if (x < 0) x = WidthRooms - 1;
                                    LoadRoom(x, CurrentRoom.Y);
                                }
                                else if (e.Key == Key.Down)
                                {
                                    RoomDatas[FocusedRoom] = CurrentRoom.Save(this);
                                    if (e.Shift && CurrentRoom.Y == HeightRooms - 1 && HeightRooms < 100)
                                    {
                                        HeightRooms += 1;
                                    }
                                    LoadRoom(CurrentRoom.X, (CurrentRoom.Y + 1) % HeightRooms);
                                }
                                else if (e.Key == Key.Up)
                                {
                                    RoomDatas[FocusedRoom] = CurrentRoom.Save(this);
                                    int y = CurrentRoom.Y - 1;
                                    if (y < 0) y = HeightRooms - 1;
                                    LoadRoom(CurrentRoom.X, y);
                                }
                            }
                        }
                        else
                        {
                            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Enter || e.Key == Key.Escape)
                            {
                                GiveDirection(e.Key);
                                GiveDirection = null;
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (e.Key == Key.Right)
                        {
                            int m = e.Alt ? 1 : 8;
                            for (int i = 0; i < selectedSprites.Count; i++)
                            {
                                selectedSprites[i].X += m;
                                selectBoxes[i].X += m;
                            }
                        }
                        else if (e.Key == Key.Left)
                        {
                            int m = e.Alt ? 1 : 8;
                            for (int i = 0; i < selectedSprites.Count; i++)
                            {
                                selectedSprites[i].X -= m;
                                selectBoxes[i].X -= m;
                            }
                        }
                        else if (e.Key == Key.Down)
                        {
                            int m = e.Alt ? 1 : 8;
                            for (int i = 0; i < selectedSprites.Count; i++)
                            {
                                selectedSprites[i].Y += m;
                                selectBoxes[i].Y += m;
                            }
                        }
                        else if (e.Key == Key.Up)
                        {
                            int m = e.Alt ? 1 : 8;
                            for (int i = 0; i < selectedSprites.Count; i++)
                            {
                                selectedSprites[i].Y -= m;
                                selectBoxes[i].Y -= m;
                            }
                        }
                        else if (e.Key == Key.Escape)
                        {
                            if (tool == Tools.Attach)
                            {
                                tool = Tools.Select;
                                toolPrompt.Text = "";
                                toolPromptImportant = false;
                                return;
                            }
                            ClearSelection();
                            return;
                        }
                        else if (e.Key == Key.Delete)
                            {
                                for (int i = 0; i < selectedSprites.Count; i++)
                                {
                                    DeleteSprite(selectedSprites[i]);
                                    sprites.Remove(selectBoxes[i]);
                                }
                                selectedSprites.Clear();
                                selectBoxes.Clear();
                            }
                    }
                    if (!e.Control)
                    {
                        if (e.Key == Key.Escape)
                        {
                            CurrentState = GameStates.Menu;
                            sprites.Color = Color.FromArgb(70, 70, 70);
                            BGSprites.Visible = false;
                            LevelEditorMenu();
                        }
                        if (e.Key == Key.Number1)
                        {
                            tool = Tools.Ground;
                            editorTool.Text = "1 - Ground";
                            prefix = 'g';
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number2)
                        {
                            tool = Tools.Background;
                            editorTool.Text = "2 - Background";
                            prefix = 'b';
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number3)
                        {
                            tool = Tools.Spikes;
                            editorTool.Text = "3 - Spikes";
                            prefix = 's';
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number4)
                        {
                            tool = Tools.Trinket;
                            editorTool.Text = "4 - Trinket";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number5)
                        {
                            tool = Tools.Checkpoint;
                            editorTool.Text = "5 - Checkpoint";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number6)
                        {
                            tool = Tools.Disappear;
                            editorTool.Text = "6 - Disappear";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number7)
                        {
                            tool = Tools.Conveyor;
                            editorTool.Text = "7 - Conveyor";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number8)
                        {
                            tool = Tools.Platform;
                            editorTool.Text = "8 - Platform";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number9)
                        {
                            tool = Tools.Enemy;
                            editorTool.Text = "9 - Enemy";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Number0)
                        {
                            tool = Tools.GravityLine;
                            editorTool.Text = "0 - Grav Line";
                            ClearSelection();
                        }
                        else if (e.Key == Key.P)
                        {
                            tool = Tools.Start;
                            editorTool.Text = "P - Start";
                            ClearSelection();
                        }
                        else if (e.Key == Key.O)
                        {
                            tool = Tools.Crewman;
                            editorTool.Text = "O - Crewmate";
                            ClearSelection();
                        }
                        else if (e.Key == Key.I)
                        {
                            tool = Tools.WarpLine;
                            editorTool.Text = "I - Warp Line";
                            ClearSelection();
                        }
                        else if (e.Key == Key.U)
                        {
                            tool = Tools.WarpToken;
                            editorTool.Text = "U - Warp Token";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Y)
                        {
                            tool = Tools.ScriptBox;
                            editorTool.Text = "Y - Script Box";
                            ClearSelection();
                        }
                        else if (e.Key == Key.T)
                        {
                            tool = Tools.Terminal;
                            editorTool.Text = "T - Terminal";
                            ClearSelection();
                        }
                        else if (e.Key == Key.R)
                        {
                            tool = Tools.RoomText;
                            editorTool.Text = "R - Roomtext";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Semicolon)
                        {
                            tool = Tools.PushBlock;
                            editorTool.Text = "; - PushBlock";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Minus)
                        {
                            tool = Tools.Tiles;
                            tileSelection.X = currentTile.X * 8 - tileScroll.X;
                            tileSelection.Y = currentTile.Y * 8 - tileScroll.Y;
                            tileSelection.SetSize(1, 1);
                            editorTool.Text = "- - Tiles";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Plus)
                        {
                            tool = Tools.Select;
                            editorTool.Text = "= - Select";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Tilde)
                        {
                            tool = Tools.CustomSprite;
                            editorTool.Text = "` - Custom Sprite";
                            ClearSelection();
                        }
                        else if (e.Key == Key.Period)
                        {
                            int t = (int)tool;
                            t = (t + 1) % EditorTools.Length;
                            tool = (Tools)t;
                            ClearSelection();
                            EditorTool et = EditorTools[t];
                            editorTool.Text = et.Hotkey.ToString() + " - " + et.Name;
                            if (tool == Tools.Ground) prefix = 'g';
                            else if (tool == Tools.Background) prefix = 'b';
                            else if (tool == Tools.Spikes) prefix = 's';
                        }
                        else if (e.Key == Key.Comma)
                        {
                            int t = (int)tool;
                            t = (t - 1 + EditorTools.Length) % EditorTools.Length;
                            tool = (Tools)t;
                            ClearSelection();
                            EditorTool et = EditorTools[t];
                            editorTool.Text = et.Hotkey.ToString() + " - " + et.Name;
                            if (tool == Tools.Ground) prefix = 'g';
                            else if (tool == Tools.Background) prefix = 'b';
                            else if (tool == Tools.Spikes) prefix = 's';
                        }
                        else if (e.Key == Key.Space)
                        {
                            PreparePreviewScreen();
                            RectangleSprite rs = null;
                            Texture t = TextureFromName("tools");
                            for (int i = 0; i < EditorTools.Length; i++)
                            {
                                rs = new RectangleSprite(16, 16 + (i * 32), RESOLUTION_WIDTH - 32, 30);
                                rs.Color = Color.Gray;
                                rs.Layer = -1;
                                rs.Name = i.ToString();
                                previews.Add(rs);
                                StringDrawable sd = new StringDrawable(52, 0, FontTexture, EditorTools[i].Hotkey.ToString() + " - " + EditorTools[i].Name);
                                sd.Color = Color.Black;
                                sd.CenterY = rs.CenterY;
                                previews.Add(sd);
                                Sprite sp = new Sprite(rs.X + 2, rs.Y - 1, t, 0, i);
                                previews.Add(sp);
                            }
                            previewMaxScroll = (int)Math.Max(rs.Bottom + 16 - RESOLUTION_HEIGHT, 0);
                            clickPreview = (s) =>
                            {
                                if (int.TryParse(s.Name, out int tn))
                                {
                                    ClearSelection();
                                    EditorTool et = EditorTools[tn];
                                    tool = (Tools)tn;
                                    editorTool.Text = et.Hotkey.ToString() + " - " + et.Name;
                                    if (tool == Tools.Ground) prefix = 'g';
                                    else if (tool == Tools.Background) prefix = 'b';
                                    else if (tool == Tools.Spikes) prefix = 's';
                                }
                            };
                        }
                        else if (e.Key == Key.Tab && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes || tool == Tools.Tiles))
                        {
                            CurrentEditingFocus = FocusOptions.Tileset;
                            if (tileset.Texture != currentTexture)
                            {
                                tileset.ChangeTexture(currentTexture);
                                tileset.X = 0;
                                tileset.Y = 0;
                                tileScroll = new Point(0, 0);
                            }
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
                                tileSelection.X = autoTiles.Origin.X * currentTexture.TileSizeX - tileScroll.X;
                                tileSelection.Y = autoTiles.Origin.Y * currentTexture.TileSizeY - tileScroll.Y;
                            }
                            else if (tool == Tools.Tiles)
                            {
                                tileSelection.SetSize(1, 1);
                                tileSelection.X = currentTile.X * currentTexture.TileSizeX - tileScroll.X;
                                tileSelection.Y = currentTile.Y * currentTexture.TileSizeY - tileScroll.Y;
                            }
                            hudSprites.Add(tileSelection);
                            hudSprites.Add(tileset);
                        }
                        else if (e.Key == Key.Enter)
                        {
                            ClearSelection();
                            CurrentSong = LevelMusic;
                            CurrentSong.Play();
                            ActivePlayer.Visible = true;
                            if (saveRoom) SaveCurrentRoom();
                            CurrentState = GameStates.Playing;
                            LoadRoom(CurrentRoom.X, CurrentRoom.Y);
                            if (!sprites.Contains(ActivePlayer))
                                sprites.Add(ActivePlayer);
                        }
                        else if (e.Key == Key.E)
                        {
                            if (!hudSprites.Contains(RoomName))
                            {
                                hudSprites.Add(RoomName);
                                hudSprites.Add(RoomNameBar);
                                RoomName.Text = CurrentRoom.Name;
                            }
                            singleLine = true;
                            StartTyping(RoomName);
                            string rn = CurrentRoom.Name;
                            FinishTyping = (r) =>
                            {
                                if (!r)
                                    RoomName.Text = rn;
                                CurrentRoom.Name = RoomName.Text;
                                if (RoomName.Text == "")
                                {
                                    hudSprites.Remove(RoomName);
                                    hudSprites.Remove(RoomNameBar);
                                }
                            };
                        }
                        else if (e.Key == Key.M)
                        {
                            LoadRoom(CurrentRoom.X, CurrentRoom.Y);
                            ShowMap(15, 15, RESOLUTION_WIDTH - 30, RESOLUTION_HEIGHT - 30);
                            CurrentEditingFocus = FocusOptions.Map;
                        }
                        else if (e.Key == Key.B)
                        {
                            string[] choices = new string[Backgrounds.Count];
                            for (int i = 0; i < Backgrounds.Count; i++)
                            {
                                choices[i] = Backgrounds.Keys[i];
                            }
                            ShowDialog("Set background...", BGSprites.Name, choices, (r) =>
                            {
                                if (r)
                                {
                                    JToken bg = GetBackground(input.Text);
                                    if (bg is object)
                                    {
                                        CurrentRoom.BG = bg;
                                        BGSprites.Load(bg, this);
                                    }
                                }
                            });
                        }
                        else if (e.Key == Key.Slash)
                        {
                            if (descBack is null || !hudSprites.Contains(descBack))
                            {
                                if (descBack is null)
                                    descBack = new RectangleSprite(20, 20, RESOLUTION_WIDTH - 40, RESOLUTION_HEIGHT - 40);
                                descBack.Color = Color.Black;
                                descBack.Layer = 60;
                                if (descText is null)
                                {
                                    descText = new StringDrawable(25, 25, NonMonoFont, "", Color.White);
                                    descText.MaxWidth = RESOLUTION_WIDTH - 50;
                                    descText.Layer = 61;
                                }
                                EditorTool et = EditorTools[(int)tool];
                                descText.Text = "Press Space for a list of tools and their keybinds.\nPress < and > to select the previous/next tool.\n\n" + et.Hotkey.ToString() + " - " + et.Name + "\n\n" + et.Description;
                                descBack.SetHeight(descText.Height + 10);
                                descBack.CenterY = RESOLUTION_HEIGHT / 2;
                                descText.CenterY = RESOLUTION_HEIGHT / 2;
                                hudSprites.Add(descBack);
                                hudSprites.Add(descText);
                            }
                            else
                            {
                                hudSprites.Remove(descBack);
                                hudSprites.Remove(descText);
                            }
                        }
                        else if (e.Shift && e.Alt && e.Key == Key.F7)
                        {
                            ShowDialog("FPS?", _fps.ToString(), null, (r) =>
                            {
                                if (r)
                                {
                                    if (int.TryParse(input.Text, out int f))
                                        fps = f;
                                }
                            });
                        }
                        else if (tool == Tools.Tiles)
                        {
                            if (e.Key == Key.S)
                            {
                                currentTile.Y = (currentTile.Y + 1) % ((int)currentTexture.Height / currentTexture.TileSizeY);
                            }
                            else if (e.Key == Key.W)
                            {
                                currentTile.Y -= 1;
                                if (currentTile.Y < 0) currentTile.Y += (int)currentTexture.Height / currentTexture.TileSizeY;
                            }
                            else if (e.Key == Key.D)
                            {
                                currentTile.X = (currentTile.X + 1) % ((int)currentTexture.Width / currentTexture.TileSizeX);
                            }
                            else if (e.Key == Key.A)
                            {
                                currentTile.X -= 1;
                                if (currentTile.X < 0) currentTile.X += (int)currentTexture.Width / currentTexture.TileSizeX;
                            }
                        }
                    }
                    else
                    {
                        if (e.Key == Key.T)
                        {
                            showIndicators = !showIndicators;
                            ShowTileIndicators();
                        }
                    }
                    if (tool == Tools.Enemy)
                    {
                        if (e.Key == Key.A)
                        {
                            PrepareAnimationPreviews(enemyTexture);
                            clickPreview = (s) =>
                            {
                                enemyAnimation = s.Animation.Name;
                            };
                        }
                        else if (e.Key == Key.S)
                        {
                            List<string> texList = new List<string>();
                            foreach (Texture tex in Textures.Values)
                            {
                                texList.Add(tex.Name);
                            }
                            ShowDialog("Enemy texture?", enemyTexture.Name, texList.ToArray(), (r) =>
                            {
                                Texture t = TextureFromName(input.Text);
                                if (t is object && t.Animations is object && t.Animations.Count > 0)
                                {
                                    enemyTexture = t;
                                    enemyAnimation = t.Animations.Values[0].Name;
                                }
                            });
                        }
                    }
                    else if (tool == Tools.Tiles || tool == Tools.Ground || tool == Tools.Background || tool == Tools.Spikes)
                    {
                        if (e.Control && e.Key == Key.V)
                        {
                            string[] tiles = new string[] { };
                            if (tiles.Length > 1)
                            {
                                float w = currentTexture.Width / currentTexture.TileSizeX;
                                float h = currentTexture.Height / currentTexture.TileSizeY;
                                float max = w * h;
                                int curX = (int)CurrentRoom.Right - 8;
                                int curY = (int)CurrentRoom.Bottom - 8;
                                sprites.RemoveAll((s) => s is Tile);
                                for (int i = tiles.Length - 1; i > -1; i--)
                                {
                                    string t = tiles[i];
                                    if (int.TryParse(t, out int tile))
                                    {
                                        if (tile > 0)
                                        {
                                            if (tile < max)
                                            {
                                                int y = tile / (int)w;
                                                int x = tile % (int)w;
                                                Tile newTile = new Tile(curX, curY, currentTexture, x, y);
                                                sprites.AddForCollisions(newTile);
                                            }
                                        }
                                    }
                                    curX -= 8;
                                    if (curX <= CurrentRoom.GetX - 8)
                                    {
                                        curX = (int)CurrentRoom.Right - 8;
                                        curY -= 8;
                                        if (curY <= CurrentRoom.GetY - 8)
                                            break;
                                    }
                                }
                            }
                        }
                        if (e.Key == Key.L)
                        {
                            ShowDialog("Layer for tiles?", tileLayer.ToString(), new string[] { }, (r) =>
                            {
                                int l = -2;
                                if (r && int.TryParse(input.Text, out l))
                                    tileLayer = l;
                            });
                        }
                        else if (e.Shift && e.Key == Key.Z)
                        {
                            if (tileToolDefW == 3)
                                tileToolDefW = tileToolDefH = 1;
                            else
                                tileToolDefW = tileToolDefH = 3;
                        }
                        else if (e.Shift && e.Key == Key.X)
                        {
                            if (tileToolDefW == 5)
                                tileToolDefW = tileToolDefH = 1;
                            else
                                tileToolDefW = tileToolDefH = 5;
                        }
                        else if (e.Shift && e.Key == Key.C)
                        {
                            ShowDialog("Brush size? (Format = x, y)", tileToolDefW.ToString() + ", " + tileToolDefH.ToString(), null, (r) =>
                            {
                                if (r)
                                {
                                    string[] s = input.Text.Split(',');
                                    int w = 1;
                                    int h = 1;
                                    int.TryParse(s.First().Trim(), out w);
                                    int.TryParse(s.Last().Trim(), out h);
                                    tileToolDefW = w;
                                    tileToolDefH = h;
                                }
                            });
                        }
                        else if (e.Shift && e.Key == Key.F)
                        {
                            fillLock = !fillLock;
                        }
                    }
                    else if (tool == Tools.CustomSprite)
                    {
                        if (e.Key == Key.A)
                        {
                            List<string> choices = new List<string>();
                            foreach (Animation animation in customSpriteTexture.Animations.Values)
                            {
                                choices.Add(animation.Name);
                            }
                            ShowDialog("Sprite animation?", customSpriteAnimation, choices.ToArray(), (r) =>
                            {
                                if (r)
                                {
                                    if (customSpriteTexture.AnimationFromName(input.Text) is object)
                                    {
                                        customSpriteAnimation = input.Text;
                                    }
                                }
                            });
                        }
                        else if (e.Key == Key.S)
                        {
                            List<string> texList = new List<string>();
                            foreach (Texture tex in Textures.Values)
                            {
                                texList.Add(tex.Name);
                            }
                            ShowDialog("Sprite texture?", customSpriteTexture.Name, texList.ToArray(), (r) =>
                            {
                                Texture t = TextureFromName(input.Text);
                                if (t is object)
                                {
                                    customSpriteTexture = t;
                                }
                            });
                        }
                    }
                    else if (tool == Tools.Select)
                    {
                        if (e.Key == Key.A && e.Control)
                        {
                            ClearSelection();
                            List<Sprite> spr = new List<Sprite>(sprites);
                            foreach (Sprite s in spr)
                            {
                                if (s is BoxSprite && !(s is ScriptBox)) continue;
                                if (!selectedSprites.Contains(s))
                                {
                                    selectedSprites.Add(s);
                                    BoxSprite b = new BoxSprite(s.X, s.Y, BoxTexture, 1, 1, Color.Cyan);
                                    b.Layer = int.MaxValue;
                                    b.SetSize((int)Math.Ceiling(s.Width / 8), (int)Math.Ceiling(s.Height / 8));
                                    b.CenterX = s.CenterX;
                                    b.CenterY = s.CenterY;
                                    selectBoxes.Add(b);
                                    sprites.Add(b);

                                }
                                else
                                {
                                    int i = selectedSprites.IndexOf(s);
                                    BoxSprite b = selectBoxes[i];
                                    sprites.Remove(b);
                                    selectBoxes.RemoveAt(i);
                                    selectedSprites.RemoveAt(i);
                                }
                            }
                        }
                    }
                    else if (tool == Tools.PushBlock)
                    {
                        if (e.Key == Key.A)
                        {
                            PrepareAnimationPreviews(pushTexture);
                            clickPreview = (s) =>
                            {
                                pushAnimation = s.Animation.Name;
                            };
                        }
                        else if (e.Key == Key.S)
                        {
                            List<string> texList = new List<string>();
                            foreach (Texture tex in Textures.Values)
                            {
                                texList.Add(tex.Name);
                            }
                            ShowDialog("Push Block texture?", pushTexture.Name, texList.ToArray(), (r) =>
                            {
                                Texture t = TextureFromName(input.Text);
                                if (t is object && t.Animations is object && t.Animations.Count > 0)
                                {
                                    pushTexture = t;
                                    pushAnimation = t.Animations.Values[0].Name;
                                }
                            });
                        }
                    }

                }
                else if (CurrentEditingFocus == FocusOptions.Tileset)
                {
                    if (e.Key == Key.Tab || e.Key == Key.Escape)
                    {
                        CurrentEditingFocus = FocusOptions.Level;
                        hudSprites.Remove(tileset);
                        hudSprites.Remove(tileSelection);
                        selection.SetSize(1, 1);
                    }
                    else if (e.Key == Key.Number1 && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes))
                    {
                        selection.SetSize(3, 5);
                    }
                    else if (e.Key == Key.Number2 && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes))
                    {
                        selection.SetSize(3, 1);
                    }
                    else if (e.Key == Key.Number3 && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes))
                    {
                        selection.SetSize(8, 6);
                    }
                    else if (e.Key == Key.Number4 && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes))
                    {
                        selection.SetSize(4, 1);
                    }
                    else if (e.Key == Key.Right)
                    {
                        float tx = tileset.X;
                        tileset.X -= 32;
                        if (tileset.Right < RESOLUTION_WIDTH)
                            tileset.Right = RESOLUTION_WIDTH;
                        tx -= tileset.X;
                        tileScroll.X += (int)tx;
                        tileSelection.X -= tx;
                    }
                    else if (e.Key == Key.Left)
                    {
                        float tx = tileset.X;
                        tileset.X += 32;
                        if (tileset.X > 0)
                            tileset.X = 0;
                        tx -= tileset.X;
                        tileScroll.X += (int)tx;
                        tileSelection.X -= tx;
                    }
                    else if (e.Key == Key.Down)
                    {
                        float ty = tileset.Y;
                        tileset.Y -= 32;
                        if (tileset.Bottom < RESOLUTION_HEIGHT)
                            tileset.Bottom = RESOLUTION_HEIGHT;
                        ty -= tileset.Y;
                        tileScroll.Y += (int)ty;
                        tileSelection.Y -= ty;
                    }
                    else if (e.Key == Key.Up)
                    {
                        float ty = tileset.Y;
                        tileset.Y -= 32;
                        if (tileset.Y > 0)
                            tileset.Y = 0;
                        ty -= tileset.Y;
                        tileScroll.Y += (int)ty;
                        tileSelection.Y -= ty;
                    }
                }
                else if (CurrentEditingFocus == FocusOptions.Dialog)
                {
                    if (e.Key == Key.Enter)
                    {
                        CloseDialog(true);
                    }
                    else if (e.Key == Key.Escape)
                    {
                        CloseDialog(false);
                    }
                }
                else if (CurrentEditingFocus == FocusOptions.Map)
                {
                    if (e.Key == Key.Escape)
                    {
                        HideMap();
                        CurrentEditingFocus = FocusOptions.Level;
                    }
                    else if (e.Key == Key.C && e.Control)
                    {
                        if (RoomDatas.ContainsKey(selectedMap.X + selectedMap.Y * 100))
                        {
                            //Clipboard.SetText(Newtonsoft.Json.JsonConvert.SerializeObject(RoomDatas[selectedMap.X + selectedMap.Y * 100]));
                        }
                    }
                    else if (e.Key == Key.V && e.Control)
                    {
                        try
                        {
                            JObject j = JObject.Parse("");
                            Room r = Room.LoadRoom(j, this);
                            j["X"] = selectedMap.X;
                            j["Y"] = selectedMap.Y;
                            if (RoomDatas.ContainsKey(selectedMap.X + selectedMap.Y * 100))
                            {
                                RoomDatas.Remove(selectedMap.X + selectedMap.Y * 100);
                            }
                            RoomDatas.Add(selectedMap.X + selectedMap.Y * 100, j);
                        }
                        catch (Exception)
                        {
                            GetSound("hurt")?.Play();
                        }
                    }
                }
                else if (CurrentEditingFocus == FocusOptions.Previews)
                {
                    if (e.Key == Key.Escape)
                    {
                        ExitPreviews();
                    }
                    else if (e.Key == Key.Up)
                    {
                        previewScroll = Math.Max(previewScroll - 40, 0);
                    }
                    else if (e.Key == Key.Down)
                    {
                        previewScroll = Math.Min(previewScroll + 40, previewMaxScroll);
                    }
                }
            }
            else if (CurrentState == GameStates.Playing && isEditor)
            {
                if (e.Key == Key.F1)
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
                    FinishTyping = (r) =>
                    {
                        if (r)
                        {
                            Script s = new Script(Command.ParseScript(this, sd.Text), "customScript", "");
                            ExecuteScript(s, ActivePlayer, ActivePlayer);
                        }
                        hudSprites.Remove(sd);
                        hudSprites.Remove(rs);
                        Freeze = FreezeOptions.Unfrozen;
                    };
                }
                else if (e.Key == Key.F2)
                {
                    if (System.IO.Directory.Exists("levels/" + currentLevelPath))
                    {
                        string sv = Newtonsoft.Json.JsonConvert.SerializeObject(CreateSave());
                        System.IO.File.WriteAllText("levels/" + currentLevelPath + "/editorsave.v7s", sv);
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
                else if (e.Key == Key.F3)
                {
                    if (System.IO.File.Exists("levels/" + currentLevelPath + "/editorsave.v7s"))
                    {
                        JObject jo = JObject.Parse(System.IO.File.ReadAllText("levels/" + currentLevelPath + "/editorsave.v7s"));
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
            else if (CurrentState == GameStates.Menu)
            {
                if (e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.D || e.Key == Key.S)
                {
                    SelectedItem += 1;
                    SelectedItem %= MenuItems.Count;
                    UpdateMenuSelection();
                }
                else if (e.Key == Key.Left || e.Key == Key.Up || e.Key == Key.A || e.Key == Key.W)
                {
                    SelectedItem -= 1;
                    if (SelectedItem < 0)
                        SelectedItem = MenuItems.Count - 1;
                    UpdateMenuSelection();
                }
                else if (e.Key == Key.Z || e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.V)
                {
                    MenuItems[SelectedItem].Action?.Invoke();
                }
                else if (e.Key == Key.Escape)
                {
                    MenuItems.Last().Action();
                }
            }
        }

        private void checkScriptScroll()
        {
            if (typingTo.SelectionY < seScrollY - 8)
            {
                seScrollY = typingTo.SelectionY + 8;
            }
            else if (typingTo.SelectionY > seScrollY + RESOLUTION_HEIGHT - 16)
            {
                seScrollY = typingTo.SelectionY - RESOLUTION_HEIGHT + 16;
            }
            if (typingTo.SelectionX < seScrollX - 8)
            {
                seScrollX = typingTo.SelectionX + 8;
            }
            else if (typingTo.SelectionX > seScrollX + RESOLUTION_WIDTH - 16)
            {
                seScrollX = typingTo.SelectionX - RESOLUTION_WIDTH + 16;
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

        private void GlControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CurrentEditingFocus == FocusOptions.ScriptEditor)
            {
                if (e.Delta > 0)
                {
                    if (heldKeys.Contains(Key.LShift))
                        seScrollX = Math.Max(seScrollX - 10, 0);
                    else
                        seScrollY = Math.Max(seScrollY - 10, 0);
                }
                else if (e.Delta < 0)
                {
                    if (heldKeys.Contains(Key.LShift))
                        seScrollX = Math.Max(Math.Min(seScrollX + 10, typingTo.Width + 16), 0);
                    else
                        seScrollY = Math.Max(Math.Min(seScrollY + 10, typingTo.Height + 16), 0);
                }
            }
            else if (previews is object)
            {
                if (e.Delta > 0)
                {
                    previewScroll = Math.Max(previewScroll - 10, 0);
                }
                else if (e.Delta < 0)
                {
                    previewScroll = Math.Min(previewScroll + 10, previewMaxScroll);
                }
            }
        }

        private void PrepareAnimationPreviews(Texture texture)
        {
            PreparePreviewScreen();
            int x = 20;
            int y = 20;
            float maxHeight = 0;
            foreach (Animation animation in texture.Animations.Values)
            {
                Sprite s = new Sprite(x, y, texture, animation);
                s.ColorModifier = AnimatedColor.Default;
                if (s.Height > maxHeight) maxHeight = s.Height;
                if (s.Right > RESOLUTION_WIDTH - 20)
                {
                    y += (int)maxHeight + 18;
                    x = 20;
                    s.X = x;
                    s.Y = y;
                }
                s.Name = animation.Name;
                previews.Add(s);
                StringDrawable animName = new StringDrawable(s.X, s.Bottom + 2, NonMonoFont, animation.Name);
                if (animName.Right > RESOLUTION_WIDTH - 5)
                {
                    y += (int)maxHeight + 18;
                    x = 20;
                    s.X = x;
                    s.Y = y;
                    animName.X = x;
                    animName.Y = s.Bottom + 2;
                }
                x += (int)Math.Max(s.Width, animName.Width) + 8;
                previews.Add(animName);
            }
            previewMaxScroll = (int)Math.Max(0, y + maxHeight + 24);
        }

        private void PreparePreviewScreen()
        {
            CurrentEditingFocus = FocusOptions.Previews;
            previewScroll = 0;
            sprites.Color = Color.FromArgb(70, 70, 70);
            hudSprites.Visible = false;
            BGSprites.Visible = false;
            previews = new SpriteCollection();
        }

        private void OpenScripts()
        {
            PreparePreviewScreen();
            StringDrawable search = new StringDrawable(0, 20, FontTexture, "", Color.LightGray);
            search.CenterX = RESOLUTION_WIDTH / 2;
            previews.Add(search);
            StartTyping(search);
            singleLine = true;
            textChanged = (s) =>
            {
                search.CenterX = RESOLUTION_WIDTH / 2;
                RefreshScripts(s);
            };
            RefreshScripts();
            clickPreview = (p) =>
            {
                OpenScript(ScriptFromName(p.Name));
            };
            FinishTyping = (r) =>
            {
                if (r)
                {
                    ExitPreviews();
                    if (ScriptFromName(search.Text) is null)
                        Scripts.Add(search.Text, new Script(new Command[] { }, search.Text, ""));
                    OpenScript(ScriptFromName(search.Text));
                }
                else
                {
                    ExitPreviews();
                }
            };
        }

        private void RefreshScripts(string searchFor = "")
        {
            previews.Clear();
            previews.Add(typingTo);
            int y = 32;
            foreach (Script script in Scripts.Values)
            {
                if (script.Name.Contains(searchFor))
                {
                    VTextBox tb = new VTextBox(0, y, FontTexture, script.Name, Color.White);
                    tb.Visible = true;
                    tb.CenterX = RESOLUTION_WIDTH / 2;
                    tb.Name = script.Name;
                    previews.Add(tb);
                    y += (int)tb.Height + 8;
                }
            }
            previewMaxScroll = (int)Math.Max(y + 4 - RESOLUTION_HEIGHT, 0);
        }

        private void ChangeRoomColour(string colour)
        {
            AutoTileSettings.PresetGroup g = RoomPresets[CurrentRoom.GroupName];
            AutoTileSettings.RoomPreset p;
            if (!g.ContainsKey(colour)) return;
            p = g[colour];
            Texture t = p.Texture;
            if (t != null && CurrentRoom.TileTexture != t)
            {
                foreach (Sprite tile in sprites)
                {
                    if (tile is Tile)
                    {
                        (tile as Tile).ChangeTexture(t);
                    }
                }
                CurrentRoom.TileTexture = t;
            }
            CurrentRoom.UsePreset(p, g.Name);
            foreach (Sprite sprite in sprites)
            {
                if (sprite is Platform)
                {
                    Color c = roomColor;
                    int tr = c.R + (255 - c.R) / 2;
                    int tg = c.G + (255 - c.G) / 2;
                    int tb = c.B + (255 - c.B) / 2;
                    sprite.Color = Color.FromArgb(tr, tg, tb);
                }
                else if (sprite is Enemy)
                {
                    sprite.Color = roomColor;
                }
            }
            replaceTiles = true;
        }

        private void StartTyping(StringDrawable t)
        {
            typing = true;
            typingTo = t;
            t.SelectionStart = t.Text.Length;
            t.SelectionLength = 0;
            t.Text = t.Text;
        }

        private void ClearSelection()
        {
            hudSprites.Remove(previewTile);
            foreach (BoxSprite sprite in selectBoxes)
            {
                sprites.Remove(sprite);
            }
            selectBoxes.Clear();
            selectedSprites.Clear();
        }
        private void GlControl_KeyUp(object sender, KeyboardKeyEventArgs e)
        {
            if (heldKeys.Contains(e.Key) && inputMap.ContainsKey(e.Key))
            {
                inputs[(int)inputMap[e.Key]]--;
            }
            heldKeys.Remove(e.Key);
        }

        public Script.Executor ExecuteScript(Script script, Sprite sender, Sprite target)
        {
            if (script is null) return null;
            Script.Executor scr = new Script.Executor(script);
            CurrentScripts.Add(scr);
            scr.Finished += (s) => { CurrentScripts.Remove(s); };
            scr.ExecuteFromBeginning(sender, target);
            return scr;
        }

        // GAME LOOP - GAME LOOP - GAME LOOP - GAME LOOP - GAME LOOP - GAME LOOP - GAME LOOP - GAME LOOP - GAME LOOP - GAME LOOP - GAME LOOP
//        private void GameLoop()
//        {
//            Stopwatch stp = new Stopwatch();
//            long nextFrame = ticksPerFrame;
//            stp.Start();

//            while (IsPlaying)
//            {
//                // frame rate limiting
//                while (stp.ElapsedTicks < nextFrame)
//                {
//                    int msToSleep = (int)((float)(nextFrame - stp.ElapsedTicks) / Stopwatch.Frequency - 0.5f);
//                    if (msToSleep > 0)
//                        Thread.Sleep(msToSleep);
//                }
//                long ticksElapsed = stp.ElapsedTicks - nextFrame;
//                int framesDropped = (int)(ticksElapsed / ticksPerFrame);
//                nextFrame += ticksPerFrame * (framesDropped + 1);
//#if TEST
//                long fStart = stp.ElapsedTicks;
//#endif
                
//            }
//        }

        private void ProcessMenu()
        {
            if (ItemSprites.Count > 0)
            {
                if (mouseX >= ItemSelector.X && mouseX <= ItemSelector.Right && justMoved)
                {
                    for (int i = 0; i < ItemSprites.Count; i++)
                    {
                        if (mouseY >= ItemSprites[i].Y && mouseY <= ItemSprites[i].Bottom)
                        {
                            if (SelectedItem != i)
                            {
                                SelectedItem = i;
                                UpdateMenuSelection();
                            }
                            break;
                        }
                    }
                }
                float target = ItemSprites[SelectedItem].CenterY;
                if (ItemSelector.CenterY < target)
                {
                    ItemSelector.Y += (float)Math.Ceiling((target - ItemSelector.CenterY) / 3);
                    if (ItemSelector.CenterY > target)
                        ItemSelector.CenterY = target;
                }
                else if (ItemSelector.CenterY > target)
                {
                    ItemSelector.Y += (float)Math.Floor((target - ItemSelector.CenterY) / 3);
                    if (ItemSelector.CenterY < target)
                        ItemSelector.CenterY = target;
                }
            }
        }

        private void UpdateMenu()
        {
            int clrIndex = Array.IndexOf(MenuColors, MenuColor);
            clrIndex += 1;
            clrIndex %= MenuColors.Length;
            MenuColor = MenuColors[clrIndex];
            BGSprites.BaseColor = Color.FromArgb(MenuColor.R / 4, MenuColor.G / 4, MenuColor.B / 4);
            if (!hudSprites.Contains(ItemSelector))
                hudSprites.Add(ItemSelector);
            foreach (StringDrawable sd in ItemSprites)
            {
                hudSprites.Remove(sd);
            }
            ItemSprites.Clear();
            float y = 0;
            MaxMenuWidth = 0;
            for (int i = 0; i < MenuItems.Count; i++)
            {
                StringDrawable sd = new StringDrawable(0, y, FontTexture, MenuItems[i].Text);
                if (SelectedItem == i)
                    sd.Color = MenuColor;
                else
                    sd.Color = Color.FromArgb(MenuColor.R / 2, MenuColor.G / 2, MenuColor.B / 2);
                sd.CenterX = RESOLUTION_WIDTH / 2;
                y += sd.Height + 4;
                sd.Layer = 1;
                if (sd.Width > MaxMenuWidth) MaxMenuWidth = sd.Width;
                ItemSprites.Add(sd);
                hudSprites.Add(sd);
            }
            if (ItemSprites.Count > 0)
            {
                float remainder = RESOLUTION_HEIGHT - y;
                remainder /= 2;
                foreach (StringDrawable sd in ItemSprites)
                {
                    sd.Y += remainder;
                }
                ItemSelector.SetSize(MaxMenuWidth + 16, ItemSprites[SelectedItem].Height + 4);
                ItemSelector.Color = Color.FromArgb(MenuColor.R / 4, MenuColor.G / 4, MenuColor.B / 4);
                ItemSelector.CenterX = RESOLUTION_WIDTH / 2;
                ItemSelector.CenterY = ItemSprites[SelectedItem].CenterY;
            }
        }

        private void UpdateMenuSelection()
        {
            for (int i = 0; i < MenuItems.Count; i++)
            {
                StringDrawable sd = ItemSprites[i];
                if (SelectedItem == i)
                    sd.Color = MenuColor;
                else
                    sd.Color = Color.FromArgb(MenuColor.R / 2, MenuColor.G / 2, MenuColor.B / 2);
            }
            ItemSelector.SetHeight(ItemSprites[SelectedItem].Height + 4);
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
                    if (spr[i] is Tile && spr[i].Layer == tileLayer) return spr[i] as Tile;
                }
            }
            return ret;
        }

        private void HandleHUD()
        {
            if (CurrentState == GameStates.Playing)
            {
                topEditor.Visible = selection.Visible = roomLoc.Visible = editorTool.Visible = previewTile.Visible = toolPrompt.Visible =
                    false;
                RoomName.Visible = RoomNameBar.Visible = true;
                RoomNameBar.Color = Color.Black;
            }
            else if (CurrentState == GameStates.Editing)
            {
                topEditor.Visible = selection.Visible = roomLoc.Visible = editorTool.Visible = previewTile.Visible = toolPrompt.Visible = RoomName.Visible = RoomNameBar.Visible =
                    true;
                if (!hudSprites.Contains(selection))
                    hudSprites.Add(selection);
                if (!hudSprites.Contains(editorTool))
                    hudSprites.Add(editorTool);
                if (!hudSprites.Contains(topEditor))
                    hudSprites.Add(topEditor);
                if (!hudSprites.Contains(roomLoc))
                    hudSprites.Add(roomLoc);
                if (CurrentEditingFocus == FocusOptions.Tileset || CurrentEditingFocus == FocusOptions.Map)
                {
                    topEditor.Visible = editorTool.Visible = roomLoc.Visible = previewTile.Visible = toolPrompt.Visible = RoomName.Visible = RoomNameBar.Visible =
                    false;
                    selection.Visible = tileSelection.Visible =
                        CurrentEditingFocus == FocusOptions.Tileset;
                }
                RoomNameBar.Color = Color.FromArgb(100, 0, 0, 0);
                if (selection.Y > 200 || hideToolbars)
                {
                    if (RoomNameBar.Y < RESOLUTION_HEIGHT)
                    {
                        RoomName.Y += 1;
                        RoomNameBar.Y += 1;
                    }
                }
                else if (RoomNameBar.Bottom > RESOLUTION_HEIGHT)
                {
                    RoomName.Y -= 1;
                    RoomNameBar.Y -= 1;
                }
                if ((selection.Y < 56 && mouseIn && !toolPromptImportant) || hideToolbars)
                {
                    if (topEditor.Bottom > 0)
                    {
                        topEditor.Y -= 2;
                        editorTool.Y -= 2;
                        toolPrompt.Y -= 2;
                        previewTile.Y -= 2;
                        roomLoc.Y -= 2;
                    }
                }
                else if (topEditor.Y < 0)
                {
                    topEditor.Y += 2;
                    editorTool.Y += 2;
                    toolPrompt.Y += 2;
                    previewTile.Y += 2;
                    roomLoc.Y += 2;
                }
                if (toolPromptImportant)
                    toolPrompt.Color = Color.Red;
                else
                    toolPrompt.Color = Color.LightBlue;
            }
            else if (CurrentState == GameStates.Menu)
            {
                topEditor.Visible = selection.Visible = roomLoc.Visible = editorTool.Visible = previewTile.Visible = toolPrompt.Visible = RoomName.Visible = RoomNameBar.Visible =
                    false;
            }
            RoomName.CenterX = RESOLUTION_WIDTH / 2;
            RoomName.Y = RoomNameBar.Y + 1;
        }

        //   ______ _____ _____ _______ ____  _____    //
        //  |  ____|  __ \_   _|__   __/ __ \|  __ \   //
        //  | |__  | |  | || |    | | | |  | | |__) |  //
        //  |  __| | |  | || |    | | | |  | |  _  /   //
        //  | |____| |__| || |_   | | | |__| | | \ \   //
        //  |______|_____/_____|  |_|  \____/|_|  \_\  //
        private void HandleEditingInputs()
        {
            if (replaceTiles)
                ReplaceTiles();
            bool shift = heldKeys.Contains(Key.LShift);
            sprites.SortForCollisions();
            if (mouseIn || CurrentEditingFocus == FocusOptions.Dialog)
            {
                selection.Color = Color.Blue;
                if (CurrentState == GameStates.Editing)
                    selection.Visible = true;
                if (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Tiles || tool == Tools.Spikes)
                {
                    if (isFill)
                        selection.Color = Color.Magenta;
                    if (tool != Tools.Spikes)
                    {
                        if (heldKeys.Contains(Key.Z))
                        {
                            tileToolW = 3;
                            tileToolH = 3;
                        }
                        else if (heldKeys.Contains(Key.X))
                        {
                            tileToolW = 5;
                            tileToolH = 5;
                        }
                        else
                        {
                            tileToolW = tileToolDefW;
                            tileToolH = tileToolDefH;
                        }
                    }
                }
                else if (tool == Tools.Checkpoint || tool == Tools.Start || tool == Tools.Terminal || tool == Tools.CustomSprite)
                {
                    if (heldKeys.Contains(Key.Z))
                    {
                        flipToolY = true;
                    }
                    else
                    {
                        flipToolY = false;
                    }
                    if (heldKeys.Contains(Key.X))
                    {
                        flipToolX = true;
                    }
                    else
                    {
                        flipToolX = false;
                    }
                }
                if (CurrentEditingFocus == FocusOptions.Level && !currentlyBinding)
                    switch (tool)
                    {
                        case Tools.Ground:
                        case Tools.Background:
                        case Tools.Spikes:
                        case Tools.Tiles:
                            {
                                selection.SetSize(tileToolW, tileToolH);
                                int x = tool == Tools.Tiles ? currentTile.X : autoTiles.Origin.X;
                                int y = tool == Tools.Tiles ? currentTile.Y : autoTiles.Origin.Y;
                                if (previewTile.Texture != currentTexture)
                                {
                                    previewTile.ChangeTexture(currentTexture);
                                }
                                if (previewTile.TextureX != x || previewTile.TextureY != y)
                                {
                                    previewTile.Animation = Animation.Static(x, y, currentTexture);
                                    previewTile.ResetAnimation();
                                }
                                string s = "  " + (tool == Tools.Tiles ? "Tile" : "Auto Tiles") + " {" + x.ToString() + ", " + y.ToString() + "}";
                                if (tileLayer != -2)
                                {
                                    s += " (Layer = " + tileLayer.ToString() + ")";
                                }
                                toolPrompt.Text = s;
                                if (!hudSprites.Contains(previewTile))
                                    hudSprites.Add(previewTile);
                                if (!hudSprites.Contains(toolPrompt))
                                    hudSprites.Add(toolPrompt);
                            }
                            break;
                        case Tools.Checkpoint:
                            selection.SetSize(2, 2);
                            toolPrompt.Text = "";
                            break;
                        case Tools.Trinket:
                            selection.SetSize(2, 2);
                            toolPrompt.Text = "Total Trinkets: " + LevelTrinkets.Count.ToString();
                            break;
                        case Tools.Enemy:
                            {
                                Rectangle r = new Rectangle(0, 0, 8, 8);
                                Animation a = enemyTexture.AnimationFromName(enemyAnimation);
                                if (a is object) r = a.Hitbox;
                                selection.SetSize((int)Math.Ceiling(r.Width / 8f), (int)Math.Ceiling(r.Height / 8f));
                                if (GiveDirection == null)
                                    toolPrompt.Text = enemyAnimation;
                                else
                                    toolPrompt.Text = "Press arrow key for enemy direction";
                                if (!hudSprites.Contains(toolPrompt))
                                    hudSprites.Add(toolPrompt);
                            }
                            break;
                        case Tools.Disappear:
                            {
                                Platform defaultDisappear = Sprite.LoadSprite(SpriteTemplates["defaultDisappear"], this) as Platform;
                                selection.SetSize((int)Math.Ceiling(defaultDisappear.Width / 8), (int)Math.Ceiling(defaultDisappear.Height / 8));
                                toolPrompt.Text = "";
                            }
                            break;
                        case Tools.Platform:
                            {
                                Platform defaultPlatform = Sprite.LoadSprite(SpriteTemplates["defaultPlatform"], this) as Platform;
                                selection.SetSize((int)Math.Ceiling(defaultPlatform.Width / 8), (int)Math.Ceiling(defaultPlatform.Height / 8));
                                if (!toolPromptImportant)
                                    toolPrompt.Text = "";
                            }
                            break;
                        case Tools.PushBlock:
                            {
                                Rectangle r = new Rectangle(0, 0, 8, 8);
                                Animation a = pushTexture.AnimationFromName(pushAnimation);
                                if (a is object) r = a.Hitbox;
                                selection.SetSize((int)Math.Ceiling(r.Width / 8f), (int)Math.Ceiling(r.Height / 8f));
                                toolPrompt.Text = pushAnimation;
                                if (!hudSprites.Contains(toolPrompt))
                                    hudSprites.Add(toolPrompt);
                            }
                            break;
                        case Tools.Conveyor:
                            {
                                if (shift)
                                    selection.SetSize(1, 1);
                                else
                                {
                                    Platform defaultConveyor = Sprite.LoadSprite(SpriteTemplates["defaultConveyor"], this) as Platform;
                                    selection.SetSize((int)Math.Ceiling(defaultConveyor.Width / 8), (int)Math.Ceiling(defaultConveyor.Height / 8));
                                }
                                if (!toolPromptImportant)
                                    toolPrompt.Text = "";
                            }
                            break;
                        case Tools.Terminal:
                            {
                                Terminal defaultTerminal = Sprite.LoadSprite(SpriteTemplates["defaultTerminal"], this) as Terminal;
                                selection.SetSize((int)Math.Ceiling(defaultTerminal.Width / 8), (int)Math.Ceiling(defaultTerminal.Height / 8));
                                if (!toolPromptImportant)
                                    toolPrompt.Text = "";
                            }
                            break;
                        case Tools.WarpToken:
                            {
                                WarpToken defaultWarp = Sprite.LoadSprite(SpriteTemplates["defaultWarpToken"], this) as WarpToken;
                                selection.SetSize((int)Math.Ceiling(defaultWarp.Width / 8), (int)Math.Ceiling(defaultWarp.Height / 8));
                                if (!toolPromptImportant)
                                    toolPrompt.Text = "";
                            }
                            break;
                        case Tools.ScriptBox:
                            selection.SetSize(1, 1);
                            if (!toolPromptImportant)
                                toolPrompt.Text = "";
                            break;
                        case Tools.GravityLine:
                        case Tools.WarpLine:
                            selection.SetSize(1, 1);
                            if (!toolPromptImportant)
                                toolPrompt.Text = "";
                            break;
                        case Tools.Start:
                            selection.SetSize((int)Math.Ceiling(ActivePlayer.Width / 8), (int)Math.Ceiling(ActivePlayer.Height / 8));
                            if (!toolPromptImportant)
                                toolPrompt.Text = "";
                            break;
                        case Tools.Crewman:
                            selection.SetSize(2, 3);
                            if (!toolPromptImportant)
                                toolPrompt.Text = "";
                            break;
                        case Tools.RoomText:
                            selection.SetSize(1, 1);
                            if (!toolPromptImportant)
                                toolPrompt.Text = "";
                            break;
                        case Tools.CustomSprite:
                            {
                                Animation anim = customSpriteTexture.AnimationFromName(customSpriteAnimation);
                                if (anim is null)
                                    selection.SetSize(1, 1);
                                else
                                    selection.SetSize((int)Math.Ceiling((float)anim.Hitbox.Width / 8), (int)Math.Ceiling((float)anim.Hitbox.Height / 8));
                                toolPrompt.Text = customSpriteAnimation + " (" + customSpriteTexture.Name + ")";
                            }
                            break;
                        case Tools.Select:
                            selection.SetSize(1, 1);
                            if (!toolPromptImportant)
                                toolPrompt.Text = "";
                            break;
                        default:
                            break;
                    }
                if (mouseIn)
                {
                    if (!heldKeys.Contains(Key.RBracket))
                        selection.X = (int)Math.Floor((mouseX - (8 * (selection.WidthTiles / 2f))) / 8 + 0.5) * 8;
                    if (!heldKeys.Contains(Key.LBracket))
                        selection.Y = (int)Math.Floor((mouseY - (8 * (selection.HeightTiles / 2f))) / 8 + 0.5) * 8;
                }
                else
                {
                    selection.X = -8;
                    selection.Y = -8;
                }
            }
            else
            {
                selection.Visible = false;
            }
            if (CurrentEditingFocus == FocusOptions.Level)
            {
                if (currentlyBinding)
                {
                    selection.SetSize(1, 1);
                    if (selecting)
                    {
                        int w = (int)Math.Floor((mouseX - selectOrigin.X) / 8);
                        int h = (int)Math.Floor((mouseY - selectOrigin.Y) / 8);
                        if (w >= 0) w += 1;
                        else w -= 1;
                        if (h >= 0) h += 1;
                        else h -= 1;
                        selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                        selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                        selection.SetSize(Math.Abs(w), Math.Abs(h));
                        if (!leftMouse)
                        {
                            bindSprite?.Invoke(new Rectangle((int)selection.X + (int)CameraX, (int)selection.Y + (int)CameraY, (int)selection.Width, (int)selection.Height));
                            selecting = false;
                            toolPromptImportant = false;
                        }
                    }
                    else if (leftMouse && !selecting)
                    {
                        selecting = true;
                        selectOrigin = new PointF(selection.X, selection.Y);
                    }
                }
                else
                {
                    if ((heldKeys.Contains(Key.LControl) && leftMouse && tool != Tools.Attach) || tool == Tools.Select)
                    {
                        if (selecting)
                        {
                            int w = (int)Math.Floor((mouseX - selectOrigin.X) / 8);
                            int h = (int)Math.Floor((mouseY - selectOrigin.Y) / 8);
                            if (w >= 0) w += 1;
                            else w -= 1;
                            if (h >= 0) h += 1;
                            else h -= 1;
                            selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                            selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                            selection.SetSize(Math.Abs(w), Math.Abs(h));
                            if (!leftMouse)
                            {
                                selecting = false;
                                List<Sprite> col = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY, selection.Width, selection.Height);
                                foreach (Sprite s in col)
                                {
                                    if (s is BoxSprite && !(s is ScriptBox)) continue;
                                    if (!heldKeys.Contains(Key.LControl) && (s is Tile || s is ScriptBox)) continue;
                                    if (!selectedSprites.Contains(s))
                                    {
                                        selectedSprites.Add(s);
                                        BoxSprite b = new BoxSprite(s.X, s.Y, BoxTexture, 1, 1, Color.Cyan);
                                        b.Layer = int.MaxValue;
                                        b.SetSize((int)Math.Ceiling(s.Width / 8), (int)Math.Ceiling(s.Height / 8));
                                        b.CenterX = s.CenterX;
                                        b.CenterY = s.CenterY;
                                        selectBoxes.Add(b);
                                        sprites.Add(b);

                                    }
                                    else if (selectedSprites.Contains(s))
                                    {
                                        int i = selectedSprites.IndexOf(s);
                                        BoxSprite b = selectBoxes[i];
                                        sprites.Remove(b);
                                        selectBoxes.RemoveAt(i);
                                        selectedSprites.RemoveAt(i);
                                    }
                                }
                            }
                        }
                        else if (dragging)
                        {
                            float x = (int)(mouseX - selectOrigin.X);
                            float y = (int)(mouseY - selectOrigin.Y);
                            if (!heldKeys.Contains(Key.Menu))
                            {
                                x = (int)(x / 8) * 8;
                                y = (int)(y / 8) * 8;
                            }
                            for (int i = 0; i < selectedSprites.Count; i++)
                            {
                                selectedSprites[i].X += x;
                                selectBoxes[i].X += x;
                                selectedSprites[i].Y += y;
                                selectBoxes[i].Y += y;
                            }
                            selectOrigin.X += x;
                            selectOrigin.Y += y;
                            if (!leftMouse)
                            {
                                dragging = false;
                            }
                        }
                        else
                        {
                            if (leftMouse)
                            {
                                bool drag = false;
                                if (!heldKeys.Contains(Key.LShift))
                                {
                                    List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 1, 1);
                                    foreach (Sprite s in spr)
                                    {
                                        if (selectedSprites.Contains(s))
                                        {
                                            drag = true;
                                            break;
                                        }
                                    }
                                }
                                if (drag)
                                {
                                    dragging = true;
                                    selectOrigin = new PointF(mouseX, mouseY);
                                }
                                else
                                {
                                    tool = Tools.Select;
                                    editorTool.Text = "= - Select";
                                    if (!heldKeys.Contains(Key.LShift))
                                        ClearSelection();
                                    selecting = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                            }
                            else if (middleMouse)
                            {
                                bool isSelected = false;
                                List<Sprite> colliders = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                Sprite s = null;
                                foreach (Sprite sprite in colliders)
                                {
                                    if (selectedSprites.Contains(sprite))
                                        isSelected = true;
                                    s = sprite;
                                }
                                if (!isSelected && s is object)
                                {
                                    ClearSelection();
                                    selectedSprites.Add(s);
                                    BoxSprite b = new BoxSprite(s.X, s.Y, BoxTexture, 1, 1, Color.Cyan);
                                    b.Layer = int.MaxValue;
                                    b.SetSize((int)Math.Ceiling(s.Width / 8), (int)Math.Ceiling(s.Height / 8));
                                    b.CenterX = s.CenterX;
                                    b.CenterY = s.CenterY;
                                    selectBoxes.Add(b);
                                    sprites.Add(b);
                                }
                                SetProperty();
                            }
                            else if (rightMouse && !stillHolding)
                            {
                                stillHolding = true;
                                bool isSelected = false;
                                List<Sprite> colliders = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                Sprite s = null;
                                foreach (Sprite sprite in colliders)
                                {
                                    if (selectedSprites.Contains(sprite))
                                        isSelected = true;
                                    s = sprite;
                                }
                                if (!isSelected)
                                {
                                    ClearSelection();
                                    if (s is object)
                                    {
                                        selectedSprites.Add(s);
                                        BoxSprite b = new BoxSprite(s.X, s.Y, BoxTexture, 1, 1, Color.Cyan);
                                        b.Layer = int.MaxValue;
                                        b.SetSize((int)Math.Ceiling(s.Width / 8), (int)Math.Ceiling(s.Height / 8));
                                        b.CenterX = s.CenterX;
                                        b.CenterY = s.CenterY;
                                        selectBoxes.Add(b);
                                        sprites.Add(b);
                                    }
                                }
                                if (selectedSprites.Count > 0)
                                {
                                    contextMenuItems.Clear();
                                    List<Type> typesMulti = new List<Type> { typeof(IBoundSprite), typeof(IPlatform), typeof(IScriptExecutor) };
                                    List<Type> typesSingle = new List<Type> { typeof(StringDrawable), typeof(WarpToken) };
                                    if (selectedSprites.Count > 0)
                                    {
                                        for (int i = 0; i < typesMulti.Count; i++)
                                        {
                                            if (selectedSprites.Any((sp) => !typesMulti[i].IsAssignableFrom(sp.GetType())))
                                            {
                                                typesMulti.RemoveAt(i--);
                                            }
                                        }
                                    }
                                    contextMenuItems.Add(new VMenuItem("Flip X", () =>
                                    {
                                        foreach (Sprite sprite in selectedSprites)
                                        {
                                            sprite.FlipX = !sprite.FlipX;
                                        }
                                    }));
                                    contextMenuItems.Add(new VMenuItem("Flip Y", () =>
                                    {
                                        foreach (Sprite sprite in selectedSprites)
                                        {
                                            sprite.FlipY = !sprite.FlipY;
                                        }
                                    }));
                                    if (typesMulti.Contains(typeof(IBoundSprite)))
                                    {
                                        contextMenuItems.Add(new VMenuItem("Set Speed", () =>
                                        {
                                            float speedX = (selectedSprites[0] as IBoundSprite).XVel;
                                            float speedY = (selectedSprites[0] as IBoundSprite).YVel;
                                            float speedV = (float)Math.Sqrt((selectedSprites[0] as IBoundSprite).XVel * (selectedSprites[0] as IBoundSprite).XVel + (selectedSprites[0] as IBoundSprite).YVel * (selectedSprites[0] as IBoundSprite).YVel);
                                            bool canXY = true;
                                            bool canVel = true;
                                            for (int i = 1; i < selectedSprites.Count; i++)
                                            {
                                                IBoundSprite sprite = selectedSprites[i] as IBoundSprite;
                                                if (sprite.XVel != speedX || sprite.YVel != speedY) canXY = false;
                                                if (Math.Sqrt(sprite.XVel * sprite.XVel + sprite.YVel * sprite.YVel) != speedV)
                                                {
                                                    canXY = false;
                                                    canVel = false;
                                                    break;
                                                }
                                            }
                                            string da = canXY ? speedX.ToString() + ", " + speedY.ToString() : (canVel ? speedV.ToString() : "");
                                            ShowDialog("Set speed (format: x, y) - Negative values for up/left.\nYou can also type only one value to keep the direction the same.", da, null, (r) =>
                                            {
                                                string[] a = input.Text.Split(new char[] { ',', 'x', 'X' });
                                                if (a.Length == 2)
                                                {
                                                    if (float.TryParse(a[0], out float xs) && float.TryParse(a[1], out float ys))
                                                    {
                                                        foreach (IBoundSprite sprite in selectedSprites)
                                                        {
                                                            sprite.XVel = xs;
                                                            sprite.YVel = ys;
                                                        }
                                                    }
                                                }
                                                else if (a.Length == 1)
                                                {
                                                    if (float.TryParse(a[0], out float vs))
                                                    {
                                                        foreach (IBoundSprite sprite in selectedSprites)
                                                        {
                                                            double direction = Math.Atan2(sprite.YVel, sprite.XVel);
                                                            sprite.XVel = (float)Math.Round((vs * Math.Cos(direction)), 5);
                                                            sprite.YVel = (float)Math.Round((vs * Math.Sin(direction)), 5);
                                                        }
                                                    }
                                                }
                                            });
                                        }));
                                        contextMenuItems.Add(new VMenuItem("Set Bounds", () =>
                                        {
                                            currentlyBinding = true;
                                            bindSprite = (r) =>
                                            {
                                                foreach (IBoundSprite sprite in selectedSprites)
                                                {
                                                    sprite.Bounds = new Rectangle(r.X - (int)sprite.InitialX, r.Y - (int)sprite.InitialY, r.Width, r.Height);
                                                }
                                                currentlyBinding = false;
                                            };
                                            toolPromptImportant = true;
                                            toolPrompt.Text = "Bounds (esc: cancel, enter: no bounds)";
                                            if (!hudSprites.Contains(toolPrompt))
                                                hudSprites.Add(toolPrompt);
                                        }));
                                    }

                                    contextMenuItems.Add(new VMenuItem("Attach to Platform...", () =>
                                    {
                                        toolPrompt.Text = "Click a platform to attach to";
                                        toolPromptImportant = true;
                                        tool = Tools.Attach;
                                    }));
                                    contextMenuItems.Add(new VMenuItem("Change Layer...", () =>
                                    {
                                        if (selectedSprites.Count == 0) return;
                                        int def = selectedSprites[0].Layer;
                                        string ans = def.ToString();
                                        for (int i = 1; i < selectedSprites.Count; i++)
                                        {
                                            if (selectedSprites[i].Layer != def)
                                            {
                                                ans = "";
                                                break;
                                            }
                                        }
                                        ShowDialog("Change Layer", ans, new string[] { }, (r) =>
                                        {
                                            if (r)
                                            {
                                                if (int.TryParse(input.Text, out int l))
                                                {
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprites.RemoveFromCollisions(sprite);
                                                        sprite.Layer = l;
                                                        sprites.AddForCollisions(sprite);
                                                    }
                                                }
                                            }
                                        });
                                    }));
                                    contextMenuItems.Add(new VMenuItem("Change Size...", () =>
                                    {
                                        float def = selectedSprites[0].Size;
                                        string ans = def.ToString();
                                        for (int i = 1; i < selectedSprites.Count; i++)
                                        {
                                            if (selectedSprites[i].Size != def)
                                            {
                                                ans = "";
                                                break;
                                            }
                                        }
                                        ShowDialog("Change Size", ans, new string[] { }, (r) =>
                                        {
                                            if (r)
                                            {
                                                if (float.TryParse(input.Text, out float l))
                                                {
                                                    for (int i = 0; i < selectedSprites.Count; i++)
                                                    {
                                                        Sprite sprite = selectedSprites[i];
                                                        sprite.Size = l;
                                                        BoxSprite sb = selectBoxes[i];
                                                        sb.SetSize((int)Math.Ceiling(sprite.Width / 8), (int)Math.Ceiling(sprite.Height / 8));
                                                        sb.CenterX = sprite.CenterX;
                                                        sb.CenterY = sprite.CenterY;
                                                    }
                                                }
                                            }
                                        });
                                    }));
                                    contextMenuItems.Add(new VMenuItem("Set Property...", () =>
                                    {
                                        SetProperty();
                                    }));
                                    OpenContextMenu(mouseX, mouseY);
                                }
                            }
                            else if (!rightMouse && stillHolding)
                            {
                                stillHolding = false;
                            }
                        }
                    }
                    else
                    {
                        if (tool == Tools.Tiles)
                        {
                            if (leftMouse || rightMouse)
                            {
                                bool lm = leftMouse;
                                if (!isFill)
                                {
                                    for (int tileX = 0; tileX < tileToolW; tileX++)
                                    {
                                        for (int tileY = 0; tileY < tileToolH; tileY++)
                                        {
                                            TileTool(selection.X + CameraX + tileX * 8, selection.Y + CameraY + tileY * 8, lm);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!selecting)
                                    {
                                        selecting = true;
                                        TileFillTool(selection.X + CameraX, selection.Y + CameraY, lm, false, false, lr: !heldKeys.Contains(Key.RBracket), ud: !heldKeys.Contains(Key.LBracket));
                                    }
                                }
                            }
                            else
                            {
                                selecting = false;
                                if (middleMouse)
                                {
                                    Tile t = GetTile((int)(selection.X + CameraX), (int)(selection.Y + CameraY));
                                    if (t != null)
                                    {
                                        currentTile = new Point(t.TextureX, t.TextureY);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Ground || tool == Tools.Background || tool == Tools.Spikes && autoTiles != null)
                        {
                            if (leftMouse || rightMouse || dragging)
                            {
                                bool lm = leftMouse;
                                if (!isFill)
                                {
                                    if (heldKeys.Contains(Key.LShift) && !dragging)
                                    {
                                        dragging = true;
                                        selectOrigin = new PointF(selection.X, selection.Y);
                                    }
                                    else if (dragging)
                                    {
                                        int w = (int)Math.Floor((mouseX - selectOrigin.X) / 8);
                                        int h = (int)Math.Floor((mouseY - selectOrigin.Y) / 8);
                                        if (w >= 0) w += 1;
                                        else w -= 1;
                                        if (h >= 0) h += 1;
                                        else h -= 1;
                                        selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                        selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                                        selection.SetSize(Math.Abs(w), Math.Abs(h));
                                        if (!leftMouse)
                                        {
                                            dragging = false;
                                            List<PointF> points = new List<PointF>();
                                            for (int x = (int)selection.X; x < selection.Right; x += 8)
                                            {
                                                for (int y = (int)selection.Y; y < selection.Bottom; y += 8)
                                                {
                                                    points.Add(new PointF(x + CameraX, y + CameraY));
                                                }
                                            }
                                            AutoTilesToolMulti(points, true, true);
                                        }
                                    }
                                    else
                                    {
                                        List<PointF> toFill = new List<PointF>();
                                        for (int tileX = 0; tileX < tileToolW; tileX++)
                                        {
                                            for (int tileY = 0; tileY < tileToolH; tileY++)
                                            {
                                                float x = selection.X + CameraX + tileX * 8;
                                                float y = selection.Y + CameraY + tileY * 8;
                                                toFill.Add(new PointF(x, y));
                                            }
                                        }
                                        AutoTilesToolMulti(toFill, leftMouse);
                                    }
                                }
                                else
                                {
                                    if (!selecting)
                                    {
                                        selecting = true;
                                        TileFillTool(selection.X + CameraX, selection.Y + CameraY, lm, true, tool == Tools.Background, lr: !heldKeys.Contains(Key.RBracket), ud: !heldKeys.Contains(Key.LBracket));
                                    }
                                }
                            }
                            else
                            {
                                selecting = false;
                            }
                        }
                        else if (tool == Tools.Trinket)
                        {
                            if (leftMouse)
                            {
                                if (!selecting)
                                {
                                    int id = 0;
                                    while (LevelTrinkets.ContainsKey(id))
                                    {
                                        id++;
                                    }
                                    Trinket t = Sprite.LoadSprite(SpriteTemplates["defaultTrinket"], this) as Trinket;
                                    if (t is Trinket)
                                    {
                                        t.CenterX = selection.CenterX + CameraX;
                                        t.CenterY = selection.CenterY + CameraY;
                                        t.InitializePosition();
                                        t.ID = id;
                                        sprites.Add(t);
                                        selecting = true;
                                    }
                                }
                            }
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Trinket)
                                    {
                                        DeleteSprite(sprite);
                                    }
                                }
                            }
                            else if (selecting && !leftMouse)
                            {
                                selecting = false;
                            }
                        }
                        else if (tool == Tools.Checkpoint)
                        {
                            if (leftMouse & !selecting)
                            {
                                selecting = true;
                                Texture sp32 = TextureFromName("sprites32");
                                Checkpoint cp = new Checkpoint(selection.X + CameraX, selection.Y + CameraY, sp32, sp32.AnimationFromName("CheckOff"), sp32.AnimationFromName("CheckOn"), flipToolX, flipToolY);
                                cp.CenterX = selection.CenterX + CameraX;
                                if (!flipToolY)
                                    cp.Bottom = selection.Bottom + CameraY;
                                cp.InitializePosition();
                                sprites.Add(cp);
                            }
                            else if (!leftMouse & selecting)
                                selecting = false;
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Checkpoint)
                                    {
                                        sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Disappear)
                        {
                            if (leftMouse && !selecting)
                            {
                                selecting = true;
                                Platform platform = Sprite.LoadSprite(SpriteTemplates["defaultDisappear"], this) as Platform;
                                if (platform != null)
                                {
                                    platform.CenterX = selection.CenterX + CameraX;
                                    platform.CenterY = selection.CenterY + CameraY;
                                    Color c = roomColor;
                                    int r = c.R + (255 - c.R) / 2;
                                    int g = c.G + (255 - c.G) / 2;
                                    int b = c.B + (255 - c.B) / 2;
                                    platform.Color = Color.FromArgb(r, g, b);
                                    platform.InitializePosition();
                                    sprites.AddForCollisions(platform);
                                }
                            }
                            else if (!leftMouse && selecting)
                                selecting = false;
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Platform)
                                    {
                                        sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Conveyor)
                        {
                            if (shift && !dragging)
                            {
                                selection.SetSize(1, 1);
                                if (leftMouse)
                                {
                                    dragging = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                            }
                            else if (dragging)
                            {
                                int w = (int)Math.Floor((mouseX - selectOrigin.X) / 8);
                                if (w >= 0) w += 1;
                                else w -= 1;
                                selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                selection.Y = selectOrigin.Y;
                                selection.SetSize(Math.Abs(w), 1);
                                if (!leftMouse)
                                {
                                    ConveyorTool(selection.CenterX + CameraX, selection.CenterY + CameraY, Math.Abs(w));
                                    dragging = false;
                                }
                            }
                            else if (leftMouse && !selecting)
                            {
                                selecting = true;
                                ConveyorTool(selection.CenterX + CameraX, selection.CenterY + CameraY);
                            }
                            else if (!leftMouse && selecting)
                                selecting = false;
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Platform)
                                    {
                                        sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Platform)
                        {
                            if (leftMouse && !selecting)
                            {
                                selecting = true;
                                Platform platform = Sprite.LoadSprite(SpriteTemplates["defaultPlatform"], this) as Platform;
                                if (platform != null)
                                {
                                    platform.X = selection.X + CameraX;
                                    platform.Y = selection.Y + CameraY;
                                    Color c = roomColor;
                                    int r = c.R + (255 - c.R) / 2;
                                    int g = c.G + (255 - c.G) / 2;
                                    int b = c.B + (255 - c.B) / 2;
                                    platform.Color = Color.FromArgb(r, g, b);
                                    platform.ResetAnimation();
                                    platform.InitializePosition();
                                    sprites.AddForCollisions(platform);
                                    toolPrompt.Text = "Press arrow key for platform direction";
                                    if (!hudSprites.Contains(toolPrompt))
                                        hudSprites.Add(toolPrompt);
                                    toolPromptImportant = true;
                                    GiveDirection = (d) =>
                                    {
                                        if (d == Key.Up)
                                            platform.YVel = -2;
                                        else if (d == Key.Down)
                                            platform.YVel = 2;
                                        else if (d == Key.Left)
                                            platform.XVel = -2;
                                        else if (d == Key.Right)
                                            platform.XVel = 2;
                                        else if (d == Key.Escape)
                                            sprites.Remove(platform);
                                        toolPromptImportant = false;
                                    };
                                }
                            }
                            else if (!leftMouse && selecting)
                                selecting = false;
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Platform)
                                    {
                                        sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                            else if (middleMouse && !stillHolding)
                            {
                                stillHolding = true;
                                List<Sprite> spr = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Platform)
                                    {
                                        currentlyBinding = true;
                                        bindSprite = (r) =>
                                        {
                                            IBoundSprite p = sprite as Platform;
                                            p.Bounds = new Rectangle(r.X - (int)p.InitialX, r.Y - (int)p.InitialY, r.Width, r.Height);
                                            currentlyBinding = false;
                                        };
                                        toolPromptImportant = true;
                                        toolPrompt.Text = "Bounds (esc: cancel, enter: no bounds)";
                                        if (!hudSprites.Contains(toolPrompt))
                                            hudSprites.Add(toolPrompt);
                                        break;
                                    }
                                }
                            }
                            else if (!middleMouse && stillHolding)
                                stillHolding = false;
                        }
                        else if (tool == Tools.Enemy)
                        {
                            if (leftMouse && !selecting)
                            {
                                selecting = true;
                                Animation a = enemyTexture.AnimationFromName(enemyAnimation);
                                Enemy enemy = new Enemy(0, 0, enemyTexture, a, 0, 0);
                                if (enemy != null)
                                {
                                    enemy.Animation = enemy.Texture.AnimationFromName(enemyAnimation) ?? Animation.Static(0, 0, enemy.Texture);
                                    enemy.CenterX = selection.CenterX + CameraX;
                                    enemy.CenterY = selection.CenterY + CameraY;
                                    enemy.InitializePosition();
                                    enemy.Color = roomColor;
                                    enemy.ResetAnimation();
                                    sprites.AddForCollisions(enemy);
                                    if (!hudSprites.Contains(toolPrompt))
                                        hudSprites.Add(toolPrompt);
                                    toolPromptImportant = true;
                                    GiveDirection = (d) =>
                                    {
                                        if (d == Key.Up)
                                            enemy.YVel = -2;
                                        else if (d == Key.Down)
                                            enemy.YVel = 2;
                                        else if (d == Key.Left)
                                            enemy.XVel = -2;
                                        else if (d == Key.Right)
                                            enemy.XVel = 2;
                                        else if (d == Key.Escape)
                                            sprites.Remove(enemy);
                                        toolPromptImportant = false;
                                    };
                                }
                            }
                            else if (!leftMouse && selecting)
                                selecting = false;
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Enemy)
                                    {
                                        sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                            else if (middleMouse && !stillHolding)
                            {
                                stillHolding = true;
                                List<Sprite> spr = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Enemy)
                                    {
                                        currentlyBinding = true;
                                        bindSprite = (r) =>
                                        {
                                            IBoundSprite p = sprite as Enemy;
                                            p.Bounds = new Rectangle(r.X - (int)p.InitialX, r.Y - (int)p.InitialY, r.Width, r.Height);
                                            currentlyBinding = false;
                                        };
                                        toolPromptImportant = true;
                                        toolPrompt.Text = "Bounds (esc: cancel, enter: no bounds)";
                                        break;
                                    }
                                }
                            }
                            else if (!middleMouse && stillHolding)
                                stillHolding = false;
                        }
                        else if (tool == Tools.GravityLine || tool == Tools.WarpLine)
                        {
                            if (selecting)
                            {
                                int w = (int)Math.Floor((mouseX - selectOrigin.X) / 8);
                                int h = (int)Math.Floor((mouseY - selectOrigin.Y) / 8);
                                if (Math.Abs(w) > Math.Abs(h)) h = 0;
                                else w = 0;
                                if (w >= 0) w += 1;
                                else w -= 1;
                                if (h >= 0) h += 1;
                                else h -= 1;
                                selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                                selection.SetSize(Math.Abs(w), Math.Abs(h));
                                if (!leftMouse)
                                {
                                    selecting = false;
                                    Texture tex = TextureFromName("lines");
                                    bool hor = h == 1;
                                    float x = selection.X;
                                    float y = selection.Y;
                                    if (tool == Tools.GravityLine)
                                    {
                                        GravityLine gl = new GravityLine(x + CameraX, y + CameraY, tex, hor ? tex.AnimationFromName("HGravLine") : tex.AnimationFromName("VGravLine"), hor, (int)Math.Max(selection.Width / 8, selection.Height / 8));
                                        if (hor)
                                            gl.CenterY = (int)(selection.CenterY + CameraY);
                                        else
                                            gl.CenterX = (int)(selection.CenterX + CameraX);
                                        gl.InitializePosition();
                                        sprites.AddForCollisions(gl);
                                    }
                                    else if (tool == Tools.WarpLine)
                                    {
                                        if (hor && selection.Bottom == RESOLUTION_HEIGHT)
                                        {
                                            y = RESOLUTION_HEIGHT - 1;
                                        }
                                        else if (!hor && selection.Right == RESOLUTION_WIDTH)
                                        {
                                            x = RESOLUTION_WIDTH - 1;
                                        }
                                        WarpLine wl = new WarpLine(x + CameraX, y + CameraY, tex, hor ? tex.AnimationFromName("HWarpLine") : tex.AnimationFromName("VWarpLine"), (int)Math.Max(selection.Width / 8, selection.Height / 8), hor, 0, 0, 0);
                                        if (hor)
                                        {
                                            if (y == 0)
                                            {
                                                wl.Offset = new PointF(0, RESOLUTION_HEIGHT);
                                                wl.Direction = -1;
                                            }
                                            else if (y == RESOLUTION_HEIGHT - 1)
                                            {
                                                wl.Offset = new PointF(0, -RESOLUTION_HEIGHT);
                                                wl.Direction = 1;
                                            }
                                        }
                                        else
                                        {
                                            if (x == 0)
                                            {
                                                wl.Offset = new PointF(RESOLUTION_WIDTH, 0);
                                                wl.Direction = -1;
                                            }
                                            else if (x == RESOLUTION_WIDTH - 1)
                                            {
                                                wl.Offset = new PointF(-RESOLUTION_WIDTH, 0);
                                                wl.Direction = 1;
                                            }
                                        }
                                        wl.InitializePosition();
                                        sprites.Add(wl);
                                    }
                                }
                            }
                            else
                            {
                                if (leftMouse)
                                {
                                    selecting = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                                else if (rightMouse)
                                {
                                    List<Sprite> spr = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                    foreach (Sprite sprite in spr)
                                    {
                                        if ((tool == Tools.GravityLine && sprite is GravityLine) || (tool == Tools.WarpLine && sprite is WarpLine))
                                        {
                                            sprites.RemoveFromCollisions(sprite);
                                        }
                                    }
                                }
                                else if (tool == Tools.GravityLine && middleMouse && !stillHolding)
                                {
                                    stillHolding = true;
                                    List<Sprite> spr = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                    foreach (Sprite sprite in spr)
                                    {
                                        if (sprite is GravityLine)
                                        {
                                            currentlyBinding = true;
                                            bindSprite = (r) =>
                                            {
                                                IBoundSprite p = sprite as GravityLine;
                                                p.Bounds = new Rectangle(r.X - (int)p.InitialX, r.Y - (int)p.InitialY, r.Width, r.Height);
                                                currentlyBinding = false;
                                            };
                                            toolPromptImportant = true;
                                            toolPrompt.Text = "Bounds (esc: cancel, enter: no bounds)";
                                            break;
                                        }
                                    }
                                }
                                else if (!middleMouse && stillHolding)
                                    stillHolding = false;
                            }
                        }
                        else if (tool == Tools.Start)
                        {
                            if (leftMouse)
                            {
                                if (!sprites.Contains(ActivePlayer))
                                {
                                    sprites.Add(ActivePlayer);
                                }
                                ActivePlayer.Visible = true;
                                ActivePlayer.CenterX = selection.CenterX + CameraX;
                                ActivePlayer.Bottom = selection.Bottom + CameraY;
                                ActivePlayer.FlipX = flipToolX;
                                ActivePlayer.FlipY = flipToolY;
                                if (flipToolY) ActivePlayer.Gravity = -Math.Abs(ActivePlayer.Gravity);
                                else ActivePlayer.Gravity = Math.Abs(ActivePlayer.Gravity);
                                StartX = (int)ActivePlayer.X;
                                StartY = (int)ActivePlayer.Y;
                                StartRoomX = CurrentRoom.X;
                                StartRoomY = CurrentRoom.Y;
                                defaultPlayer = ActivePlayer.Name;
                            }
                            else if (rightMouse)
                            {
                                if (!(CurrentRoom.X == StartRoomX && CurrentRoom.Y == StartRoomY))
                                {
                                    LoadRoom(StartRoomX, StartRoomY);
                                }
                                if (!sprites.Contains(ActivePlayer))
                                {
                                    sprites.Add(ActivePlayer);
                                    ActivePlayer.Visible = true;
                                }
                                ActivePlayer.X = StartX;
                                ActivePlayer.Y = StartY;
                            }
                            else
                            {

                            }
                        }
                        else if (tool == Tools.Crewman)
                        {
                            if (leftMouse && !selecting)
                            {
                                selecting = true;
                                List<string> crewmen = new List<string>();
                                foreach (Texture texture in Textures.Values)
                                {
                                    if (texture.AnimationFromName("Standing") != null && texture.AnimationFromName("Walking") != null && texture.AnimationFromName("Dying") != null)
                                    {
                                        crewmen.Add(texture.Name);
                                    }
                                }
                                float x = selection.CenterX + CameraX;
                                float y = selection.Y + CameraY;
                                float bottom = selection.Bottom + CameraY;
                                bool flipX = flipToolX;
                                bool flipY = flipToolY;
                                ShowDialog("Which crewmate do you wish to place?", "", crewmen.ToArray(), (r) =>
                                {
                                    if (r)
                                    {
                                        Texture tex = TextureFromName(input.Text);
                                        if (tex != null)
                                        {
                                            string name = tex.Name.First().ToString().ToUpper() + tex.Name.Substring(1);
                                            if (!UserAccessSprites.ContainsKey(name))
                                            {
                                                Animation stand = tex.AnimationFromName("Standing"), walk = tex.AnimationFromName("Walking"),
                                                fall = tex.AnimationFromName("Falling"), jump = tex.AnimationFromName("Jumping"), die = tex.AnimationFromName("Dying");
                                                Crewman c = new Crewman(0, 0, tex, this, name, stand, walk, fall, jump, die);
                                                c.CenterX = x;
                                                if (flipY)
                                                {
                                                    c.FlipY = true;
                                                    c.Y = y;
                                                }
                                                else
                                                    c.Bottom = bottom;
                                                c.FlipX = flipX;
                                                c.TextBoxColor = tex.TextBoxColor;
                                                UserAccessSprites.Add(name, c);
                                                c.InitializePosition();
                                                sprites.Add(c);
                                            }
                                            else
                                            {
                                                Sprite c = UserAccessSprites[name];
                                                c.CenterX = x;
                                                if (flipY)
                                                {
                                                    c.FlipY = true;
                                                    c.Y = y;
                                                }
                                                else
                                                    c.Bottom = bottom;
                                                c.FlipX = flipX;
                                                c.InitializePosition();
                                                sprites.Add(c);
                                            }
                                        }
                                    }
                                });
                            }
                            else if (!leftMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sp in spr)
                                {
                                    if (sp is Crewman && sp != ActivePlayer)
                                    {
                                        DeleteSprite(sp);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.WarpToken)
                        {
                            if (leftMouse && !selecting)
                            {
                                selecting = true;
                                if (currentWarp is null)
                                {
                                    WarpToken wt = Sprite.LoadSprite(SpriteTemplates["defaultWarpToken"], this) as WarpToken;
                                    if (wt is object)
                                    {
                                        wt.CenterX = selection.CenterX + CameraX;
                                        wt.CenterY = selection.CenterY + CameraY;
                                        sprites.Add(wt);
                                        currentWarp = wt;
                                        warpRoom = CurrentRoom;
                                        toolPrompt.Text = "Choose Warp Token output...";
                                        toolPromptImportant = true;
                                        if (!hudSprites.Contains(toolPrompt))
                                            hudSprites.Add(toolPrompt);
                                    }
                                }
                                else
                                {
                                    currentWarp.OutX = selection.X + CameraX;
                                    currentWarp.OutY = selection.Y + CameraY;
                                    currentWarp.OutRoomX = CurrentRoom.X;
                                    currentWarp.OutRoomY = CurrentRoom.Y;
                                    RoomDatas[warpRoom.X + warpRoom.Y * 100] = warpRoom.Save(this);
                                    Texture sp32 = TextureFromName("sprites32");
                                    WarpToken.WarpData data = new WarpToken.WarpData(currentWarp, warpRoom.X, warpRoom.Y);
                                    WarpTokenOutput wto = new WarpTokenOutput(currentWarp.OutX, currentWarp.OutY, sp32, sp32.AnimationFromName("WarpToken"), data);
                                    Warps.Add(data);
                                    currentWarp = null;
                                    warpRoom = null;
                                    sprites.Add(wto);
                                }
                            }
                            else if (middleMouse & !selecting)
                            {
                                selecting = true;
                                List<Sprite> col = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sprite in col)
                                {
                                    if (sprite is WarpToken)
                                    {
                                        WarpToken w = sprite as WarpToken;
                                        int x = (int)w.OutX / Room.ROOM_WIDTH;
                                        int y = (int)w.OutY / Room.ROOM_HEIGHT;
                                        if (x != CurrentRoom.X || y != CurrentRoom.Y)
                                        {
                                            LoadRoom(x, y);
                                        }
                                    }
                                    else if (sprite is WarpTokenOutput)
                                    {
                                        WarpTokenOutput w = sprite as WarpTokenOutput;
                                        int x = w.Parent.InRoom.X;
                                        int y = w.Parent.InRoom.Y;
                                        if (x != CurrentRoom.X || y != CurrentRoom.Y)
                                        {
                                            LoadRoom(x, y);
                                        }
                                    }
                                }
                            }
                            else if (!leftMouse && !middleMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is WarpToken)
                                    {
                                        DeleteSprite(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.ScriptBox)
                        {
                            if (selecting)
                            {
                                int w = (int)Math.Floor((mouseX - selectOrigin.X) / 8);
                                int h = (int)Math.Floor((mouseY - selectOrigin.Y) / 8);
                                if (w >= 0) w += 1;
                                else w -= 1;
                                if (h >= 0) h += 1;
                                else h -= 1;
                                selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                                selection.SetSize(Math.Abs(w), Math.Abs(h));
                                if (!leftMouse)
                                {
                                    selecting = false;
                                    if (currentlyResizing is null)
                                    {
                                        ScriptBox sb = new ScriptBox(selection.X + CameraX, selection.Y + CameraY, BoxTexture, selection.WidthTiles, selection.HeightTiles, null, this);
                                        sprites.Add(sb);
                                        singleLine = true;
                                        toolPrompt.Text = "[Script Name]";
                                        toolPromptImportant = true;
                                        StartTyping(toolPrompt);
                                        toolPrompt.SelectionStart = 0;
                                        toolPrompt.SelectionLength = toolPrompt.Text.Length;
                                        toolPrompt.Text = toolPrompt.Text;
                                        if (!hudSprites.Contains(toolPrompt))
                                            hudSprites.Add(toolPrompt);
                                        FinishTyping = (r) =>
                                        {
                                            if (r)
                                            {
                                                Script s = ScriptFromName(toolPrompt.Text);
                                                if (s is null)
                                                {
                                                    s = new Script(new Command[] { }, toolPrompt.Text, "");
                                                    Scripts.Add(s.Name, s);
                                                }
                                                sb.Script = s;
                                            }
                                            else
                                            {
                                                sprites.Remove(sb);
                                            }
                                            toolPromptImportant = false;
                                        };
                                    }
                                    else
                                    {
                                        currentlyResizing.X = selection.X + CameraX;
                                        currentlyResizing.Y = selection.Y + CameraY;
                                        currentlyResizing.SetSize(selection.WidthTiles, selection.HeightTiles);
                                        currentlyResizing = null;
                                        toolPromptImportant = false;
                                    }
                                }
                            }
                            else if (leftMouse && !stillHolding)
                            {
                                if (heldKeys.Contains(Key.LShift))
                                {
                                    stillHolding = true;
                                    List<Sprite> col = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY, 8, 8);
                                    ScriptBox resize = null;
                                    foreach (Sprite sprite in col)
                                    {
                                        if (sprite is ScriptBox)
                                        {
                                            resize = sprite as ScriptBox;
                                            break;
                                        }
                                    }
                                    if (resize is object)
                                    {
                                        currentlyResizing = resize;
                                        toolPromptImportant = true;
                                        toolPrompt.Text = "Resize Script Box...";
                                        if (!hudSprites.Contains(toolPrompt))
                                        {
                                            hudSprites.Add(toolPrompt);
                                        }
                                    }
                                }
                                else
                                {
                                    selecting = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                            }
                            else if (stillHolding)
                            {
                                stillHolding = false;
                            }
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is ScriptBox)
                                    {
                                        DeleteSprite(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Terminal)
                        {
                            if (leftMouse && !selecting)
                            {
                                selecting = true;
                                Terminal t = Sprite.LoadSprite(SpriteTemplates["defaultTerminal"], this) as Terminal;
                                t.CenterX = selection.CenterX + CameraX;
                                t.FlipX = flipToolX;
                                if (flipToolY)
                                {
                                    t.FlipY = true;
                                    t.Y = selection.Y + CameraY;
                                }
                                else
                                    t.Bottom = selection.Bottom + CameraY;
                                sprites.Add(t);
                                if (!hudSprites.Contains(toolPrompt))
                                    hudSprites.Add(toolPrompt);
                                singleLine = true;
                                toolPrompt.Text = "[Script Name]";
                                toolPromptImportant = true;
                                StartTyping(toolPrompt);
                                toolPrompt.SelectionStart = 0;
                                toolPrompt.SelectionLength = toolPrompt.Text.Length;
                                toolPrompt.Text = toolPrompt.Text;
                                FinishTyping = (r) =>
                                {
                                    if (r)
                                    {
                                        Script s = ScriptFromName(toolPrompt.Text);
                                        if (s is null)
                                        {
                                            s = new Script(new Command[] { }, toolPrompt.Text, "");
                                            Scripts.Add(s.Name, s);
                                        }
                                        t.Script = s;
                                    }
                                    else
                                    {
                                        sprites.Remove(t);
                                    }
                                    toolPromptImportant = false;
                                };
                            }
                            else if (!leftMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Terminal)
                                    {
                                        sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.RoomText)
                        {
                            if (leftMouse && !selecting)
                            {
                                selecting = true;
                                StringDrawable s = new StringDrawable(selection.X + CameraX, selection.Y + CameraY, FontTexture, "", Color.White);
                                if (typing)
                                {
                                    EscapeTyping();
                                    FinishTyping?.Invoke(true);
                                }
                                StartTyping(s);
                                singleLine = true;
                                sprites.Add(s);
                                FinishTyping = (b) =>
                                {
                                    if (!b)
                                    {
                                        sprites.Remove(s);
                                    }
                                };
                            }
                            else if (!leftMouse && !middleMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (middleMouse && !selecting)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sp in spr)
                                {
                                    if (sp is StringDrawable)
                                    {
                                        StringDrawable s = sp as StringDrawable;
                                        if (typing)
                                        {
                                            EscapeTyping();
                                            FinishTyping?.Invoke(true);
                                        }
                                        StartTyping(s);
                                        singleLine = true;
                                        selecting = true;
                                        string prText = s.Text;
                                        FinishTyping = (b) =>
                                        {
                                            if (!b)
                                            {
                                                s.Text = prText;
                                            }
                                        };
                                        break;
                                    }
                                }
                            }
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sp in spr)
                                {
                                    if (sp is StringDrawable)
                                    {
                                        sprites.RemoveFromCollisions(sp);
                                        if (sp == typingTo)
                                        {
                                            EscapeTyping();
                                        }
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.PushBlock)
                        {
                            if (leftMouse && !selecting)
                            {
                                selecting = true;
                                Animation a = pushTexture.AnimationFromName(pushAnimation);
                                PushSprite push = new PushSprite(0, 0, pushTexture, a);
                                if (push != null)
                                {
                                    push.CenterX = selection.CenterX + CameraX;
                                    push.CenterY = selection.CenterY + CameraY;
                                    if (flipToolY)
                                        push.Gravity = -0.6875f;
                                    push.FlipX = flipToolX;
                                    push.FlipY = flipToolY;
                                    push.InitializePosition();
                                    push.Color = roomColor;
                                    push.ResetAnimation();
                                    sprites.AddForCollisions(push);
                                }
                            }
                            else if (!leftMouse && selecting)
                                selecting = false;
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(mouseX + CameraX, mouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is PushSprite)
                                    {
                                        sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.CustomSprite)
                        {
                            if (leftMouse & !selecting)
                            {
                                selecting = true;
                                Animation animation = customSpriteTexture.AnimationFromName(customSpriteAnimation);
                                if (animation is object)
                                {
                                    Sprite cs = new Sprite(0, 0, customSpriteTexture, customSpriteTexture.AnimationFromName(customSpriteAnimation));
                                    cs.ColorModifier = AnimatedColor.Default;
                                    cs.FlipX = flipToolX;
                                    cs.FlipY = flipToolY;
                                    cs.CenterX = selection.CenterX + CameraX;
                                    cs.CenterY = selection.CenterY + CameraY;
                                    cs.InitializePosition();
                                    sprites.AddForCollisions(cs);
                                }
                            }
                            else if (!leftMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (rightMouse)
                            {
                                List<Sprite> spr = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sp in spr)
                                {
                                    if (sp.GetType() == typeof(Sprite))
                                    {
                                        sprites.RemoveFromCollisions(sp);
                                        if (sp == typingTo)
                                        {
                                            EscapeTyping();
                                        }
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Point)
                        {
                            if (leftMouse)
                            {
                                string x = selection.X.ToString();
                                string y = selection.Y.ToString();
                                typing = true;
                                tool = prTool;
                                EditorTool t = EditorTools[(int)prTool];
                                editorTool.Text = t.Hotkey.ToString() + " - " + t.Name;
                                CurrentEditingFocus = FocusOptions.ScriptEditor;
                                TypeText(x + "," + y);
                                textChanged?.Invoke(typingTo.Text);
                            }
                        }
                        else if (tool == Tools.Attach)
                        {
                            if (leftMouse)
                            {
                                tool = Tools.Select;
                                toolPrompt.Text = "";
                                toolPromptImportant = false;
                                IPlatform att = null;
                                List<Sprite> col = sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY, 8, 8);
                                foreach (Sprite c in col)
                                {
                                    if (c is IPlatform)
                                    {
                                        att = c as IPlatform;
                                        break;
                                    }
                                }
                                if (att is object)
                                {
                                    foreach (Sprite sprite in selectedSprites)
                                    {
                                        if (!sprite.Static)
                                            sprite.AttachToPlatform(att);
                                    }
                                }
                            }
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
                        currentTile = new Point((int)(selection.X + tileScroll.X) / 8, (int)(selection.Y + tileScroll.Y) / 8);
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
                            autoTiles = AutoTileSettings.Default3((int)(selection.X + tileScroll.X) / 8, (int)(selection.Y + tileScroll.Y) / 8);
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(3, 1);
                        }
                        else if (selection.HeightTiles == 5)
                        {
                            autoTiles = AutoTileSettings.Default13((int)(selection.X + tileScroll.X) / 8, (int)(selection.Y + tileScroll.Y) / 8);
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(3, 5);
                        }
                        else if (selection.WidthTiles == 8)
                        {
                            autoTiles = AutoTileSettings.Default47((int)(selection.X + tileScroll.X) / 8, (int)(selection.Y + tileScroll.Y) / 8);
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(8, 6);
                        }
                        else if (selection.WidthTiles == 4)
                        {
                            autoTiles = AutoTileSettings.Default4((int)(selection.X + tileScroll.X) / 8, (int)(selection.Y + tileScroll.Y) / 8);
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(4, 1);
                        }
                    }
                }
            }
            else if (CurrentEditingFocus == FocusOptions.Map)
            {
                if (mapSelect is null)
                {
                    mapSelect = new RectangleSprite(0, 0, 0, 0);
                    mapSelect.Color = Color.FromArgb(100, Color.White);
                    mapSelect.Layer = 26;
                }
                if (!hudSprites.Contains(mapSelect))
                {
                    hudSprites.Add(mapSelect);
                }
                float w = mapBG.Width / mapWidth;
                float h = mapBG.Height / mapHeight;
                int x = (int)Math.Floor((mouseX - mapBG.X) / w);
                int y = (int)Math.Floor((mouseY - mapBG.Y) / h);
                if (x >= 0 && y >= 0 && x < mapWidth && y < mapHeight)
                {
                    selectedMap = new Point(x, y);
                    mapSelect.X = mapBG.X + (x * w);
                    mapSelect.Y = mapBG.Y + (y * h);
                    mapSelect.SetSize(w, h);
                    mapSelect.Visible = true;
                }
                else
                    mapSelect.Visible = false;
                if (leftMouse && !selecting)
                {
                    selecting = true;
                    selectOrigin = new PointF(x, y);
                }
                else if (selecting)
                {
                    if (!dragging && selectOrigin != new PointF(x, y) && mapSprites.ContainsKey((int)selectOrigin.X + (int)selectOrigin.Y * 100))
                    {
                        dragging = true;
                        mapDragging = mapSprites[(int)selectOrigin.X + (int)selectOrigin.Y * 100];
                        mapDragging.SetTarget(mouseX - mapDragging.Width / 2, mouseY - mapDragging.Height / 2);
                        if (mapSprites.ContainsKey(x + y * 100))
                        {
                            mapMoving = mapSprites[x + y * 100];
                            mapOrigin = new Point(x, y);
                            mapMoving.SetTarget(mapBG.X + (int)selectOrigin.X * w, mapBG.Y + (int)selectOrigin.Y * h);
                        }
                    }
                    else if (dragging && leftMouse)
                    {
                        mapDragging.SetTarget(mouseX - mapDragging.Width / 2, mouseY - mapDragging.Height / 2);
                        if (mapSprites.ContainsKey(x + y * 100) && mapSprites[x + y * 100] != mapMoving)
                        {
                            if (mapMoving is object)
                                mapMoving.SetTarget(mapBG.X + mapOrigin.X * w, mapBG.Y + mapOrigin.Y * h);
                            mapMoving = mapSprites[x + y * 100];
                            if (mapMoving != mapDragging)
                            {
                                mapOrigin = new Point(x, y);
                                mapMoving.SetTarget(mapBG.X + (int)selectOrigin.X * w, mapBG.Y + (int)selectOrigin.Y * h);
                            }
                            else
                                mapMoving = null;
                        }
                    }
                    else if (!leftMouse && dragging)
                    {
                        selecting = false;
                        dragging = false;
                        if (mapMoving is object && mapDragging is object)
                        {
                            mapDragging.SetTarget(mapBG.X + mapOrigin.X * w, mapBG.Y + mapOrigin.Y * h);
                            int rs = x + y * 100;
                            JObject roomSwap = null;
                            if (RoomDatas.ContainsKey(rs))
                                roomSwap = RoomDatas[rs];
                            int sw = (int)selectOrigin.X + (int)selectOrigin.Y * 100;
                            JObject swapWith = null;
                            if (RoomDatas.ContainsKey(sw))
                                swapWith = RoomDatas[sw];
                            RoomDatas.Remove(rs);
                            RoomDatas.Remove(sw);
                            mapSprites.Remove(sw);
                            mapSprites.Remove(rs);
                            mapSprites.Add(rs, mapDragging);
                            mapSprites.Add(sw, mapMoving);
                            if (roomSwap is object)
                            {
                                roomSwap["X"] = (int)selectOrigin.X;
                                roomSwap["Y"] = (int)selectOrigin.Y;
                                RoomDatas.Add(sw, roomSwap);
                            }
                            if (swapWith is object)
                            {
                                swapWith["X"] = x;
                                swapWith["Y"] = y;
                                RoomDatas.Add(rs, swapWith);
                            }
                            mapMoving = null;
                            mapDragging = null;
                        }
                        else if (mapDragging is object)
                        {
                            mapDragging.SetTarget(mapBG.X + (int)selectOrigin.X * w, mapBG.Y + (int)selectOrigin.Y * h);
                            mapDragging = null;
                            mapMoving = null;
                        }
                    }
                    else if (!leftMouse && !dragging)
                    {
                        selecting = false;
                        if (clickMap is null)
                            LoadRoom(x, y);
                        HideMap();
                        CurrentEditingFocus = FocusOptions.Level;
                        clickMap?.Invoke(new Point(x, y));
                        clickMap = null;
                    }
                }

            }
            if (updateColor)
            {
                Color? c = GetColor(input.Text);
                if (c.HasValue)
                {
                    colorPreview.Color = c.Value;
                }
            }
        }

        private void SetProperty()
        {
            if (selectedSprites.Count > 0)
            {
                List<string> properties = new List<string>();
                foreach (Sprite sprite in selectedSprites)
                {
                    SortedList<string, SpriteProperty> pr = sprite.Properties;
                    if (properties.Count == 0)
                    {
                        foreach (SpriteProperty prop in pr.Values)
                        {
                            if (prop.CanSet)
                                properties.Add(prop.Name);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < properties.Count; i++)
                        {
                            string prop = properties[i];
                            if (!pr.Values.Any((p) => p.Name == prop))
                            {
                                properties.RemoveAt(i);
                            }
                        }
                    }
                }
                ShowDialog("Change which property?", "", properties.ToArray(), (r) =>
                {
                    if (r)
                    {
                        string answer = input.Text;
                        if (properties.Contains(answer))
                        {
                            string val = null;
                            for (int i = 0; i < selectedSprites.Count; i++)
                            {
                                if (val is object && selectedSprites[i].GetProperty(answer).ToString() != val)
                                {
                                    val = "";
                                    break;
                                }
                                else
                                {
                                    if (val is null)
                                    {
                                        val = selectedSprites[i].GetProperty(answer).ToString();
                                    }
                                }
                            }
                            List<string> choices = null;
                            SpriteProperty selProp = selectedSprites.First().Properties[answer];
                            switch (selProp.Type)
                            {
                                case SpriteProperty.Types.Int:
                                case SpriteProperty.Types.Float:
                                case SpriteProperty.Types.String:
                                    choices = new List<string>();
                                    break;
                                case SpriteProperty.Types.Bool:
                                    choices = new List<string> { "True", "False" };
                                    break;
                                case SpriteProperty.Types.Rectangle:
                                case SpriteProperty.Types.Color:
                                case SpriteProperty.Types.Point:
                                    break;
                                case SpriteProperty.Types.Texture:
                                    choices = new List<string>();
                                    foreach (Texture texture in Textures.Values)
                                    {
                                        choices.Add(texture.Name);
                                    }
                                    break;
                                case SpriteProperty.Types.Animation:
                                    choices = new List<string>();
                                    foreach (Animation animation in selectedSprites.First().Texture.Animations.Values)
                                    {
                                        choices.Add(animation.Name);
                                    }
                                    break;
                                case SpriteProperty.Types.Sound:
                                    choices = new List<string>();
                                    foreach (SoundEffect sound in Sounds.Values)
                                    {
                                        choices.Add(sound.Name);
                                    }
                                    break;
                                case SpriteProperty.Types.Script:
                                    choices = new List<string>();
                                    foreach (Script script in Scripts.Values)
                                    {
                                        choices.Add(script.Name);
                                    }
                                    break;
                            }
                            if (choices is object)
                            {
                                ShowDialog(answer + " - " + selectedSprites.First().Properties[answer].Description, val, choices.ToArray(), (r2) =>
                                {
                                    if (r2)
                                    {
                                        string ans = input.Text;
                                        switch (selectedSprites.First().Properties[answer].Type)
                                        {
                                            case SpriteProperty.Types.Int:
                                                if (int.TryParse(ans, out int intV))
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, intV, this);
                                                    }
                                                break;
                                            case SpriteProperty.Types.Float:
                                                if (float.TryParse(ans, out float floatV))
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, floatV, this);
                                                    }
                                                break;
                                            case SpriteProperty.Types.String:
                                                foreach (Sprite sprite in selectedSprites)
                                                {
                                                    sprite.SetProperty(answer, ans, this);
                                                }
                                                break;
                                            case SpriteProperty.Types.Bool:
                                                if (bool.TryParse(ans, out bool boolV))
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, boolV, this);
                                                    }
                                                break;
                                            case SpriteProperty.Types.Texture:
                                                Texture textureV = TextureFromName(ans);
                                                if (textureV is object)
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, ans, this);
                                                    }
                                                break;
                                            case SpriteProperty.Types.Animation:
                                                foreach (Sprite sprite in selectedSprites)
                                                {
                                                    if (sprite.Texture.AnimationFromName(ans) is object)
                                                        sprite.SetProperty(answer, ans, this);
                                                }
                                                break;
                                            case SpriteProperty.Types.Sound:
                                                SoundEffect soundV = GetSound(ans);
                                                if (soundV is object)
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, ans, this);
                                                    }
                                                break;
                                            case SpriteProperty.Types.Script:
                                                Script scriptV = ScriptFromName(ans);
                                                if (scriptV is object)
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, ans, this);
                                                    }
                                                break;
                                        }
                                    }
                                });
                            }
                            else if (selProp.Type == SpriteProperty.Types.Color)
                            {
                                string v = "White";
                                v = Color.FromArgb((int)selProp.GetValue()).ToKnownColor().ToString();
                                if (v == "0")
                                {
                                    v = ((int)selProp.GetValue()).ToString("X8");
                                }
                                ShowColorDialog(selProp.Name + " - " + selProp.Description, v, (r2) =>
                                {
                                    if (r2)
                                    {
                                        foreach (Sprite sprite in selectedSprites)
                                        {
                                            sprite.SetProperty(selProp.Name, colorPreview.Color.ToArgb(), this);
                                        }
                                    }
                                });
                            }
                        }
                    }
                });
            }
        }

        private void ConveyorTool(float x, float y, int length = -1)
        {
            Platform platform = Sprite.LoadSprite(SpriteTemplates["defaultConveyor"], this) as Platform;
            if (platform != null)
            {
                if (length > 0)
                    platform.Length = length;
                platform.CenterX = x;
                platform.CenterY = y;
                Color c = roomColor;
                int r = c.R + (255 - c.R) / 2;
                int g = c.G + (255 - c.G) / 2;
                int b = c.B + (255 - c.B) / 2;
                platform.Color = Color.FromArgb(r, g, b);
                platform.ResetAnimation();
                platform.InitializePosition();
                sprites.AddForCollisions(platform);
                StringDrawable gd = new StringDrawable(4, 12, FontTexture, "Press arrow key for platform direction");
                hudSprites.Add(gd);
                GiveDirection = (d) =>
                {
                    if (d == Key.Left)
                    {
                        platform.Conveyor = -2;
                        platform.Animation = platform.Texture.AnimationFromName("conveyor1l");
                    }
                    else if (d == Key.Right)
                    {
                        platform.Conveyor = 2;
                        platform.Animation = platform.Texture.AnimationFromName("conveyor1r");
                    }
                    else if (d == Key.Escape)
                        sprites.Remove(platform);
                    hudSprites.Remove(gd);
                };
            }
        }

        private void ReplaceTiles()
        {
            List<PointF> grounds = new List<PointF>();
            List<PointF> backgrounds = new List<PointF>();
            List<PointF> spikes = new List<PointF>();
            foreach (Tile tile in sprites.Where((s) => s is Tile))
            {
                if (tile.Tag is null || tile.Tag == "") continue;
                switch (tile.Tag.First())
                {
                    case 'g':
                        grounds.Add(new PointF(tile.X, tile.Y));
                        break;
                    case 'b':
                        backgrounds.Add(new PointF(tile.X, tile.Y));
                        break;
                    case 's':
                        spikes.Add(new PointF(tile.X, tile.Y));
                        break;
                }
            }
            Tools curTool = tool;
            char prf = prefix;
            if (grounds.Count > 0)
            {
                tool = Tools.Ground;
                prefix = 'g';
                AutoTilesToolMulti(grounds, true);
            }
            if (backgrounds.Count > 0)
            {
                tool = Tools.Background;
                prefix = 'b';
                AutoTilesToolMulti(backgrounds, true);
            }
            if (spikes.Count > 0)
            {
                tool = Tools.Spikes;
                prefix = 's';
                AutoTilesToolMulti(spikes, true);
            }
            tool = curTool;
            prefix = prf;
            replaceTiles = false;
        }

        private void DeleteSprite(Sprite s)
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
                        if (w.OutputSprite is object)
                            sprites.Remove(w.OutputSprite);
                        foreach (WarpToken.WarpData data in Warps)
                        {
                            WarpToken.WarpData other = new WarpToken.WarpData(w, CurrentRoom.X, CurrentRoom.Y);
                            if (data.Equals(other))
                            {
                                Warps.Remove(data);
                                break;
                            }
                        }
                        UserAccessSprites.Remove(w.Name);
                    }
                    break;
            }
        }

        private void TileFillTool(float x, float y, bool leftClick, bool isAuto, bool isBackground, List<PointF> alreadyFilled = null, int tileX = -1, int tileY = -1, string tag = null, bool lr = true, bool ud = true)
        {
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
                TileTool(x, y, leftClick);
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
                    TileFillTool(x + point.X, y + point.Y, leftClick, isAuto, isBackground, alreadyFilled, tileX, tileY, tag, lr, ud);
                }
            }
            if (initial && toFill.Count > 0)
            {
                AutoTilesToolMulti(alreadyFilled, leftClick);
            }
        }

        private bool IsOutsideRoom(float x, float y)
        {
            return x < CameraX || x >= CameraX + Room.ROOM_WIDTH || y < CameraY || y >= CameraY + Room.ROOM_HEIGHT;
        }

        private void TileTool(float x, float y, bool leftClick)
        {
            if (IsOutsideRoom(x, y)) return;
            Tile tile = GetTile((int)x, (int)y);
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

        private void AutoTilesToolMulti(List<PointF> points, bool leftClick, bool updateOthers = true)
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
                Tile tile = GetTile((int)point.X, (int)point.Y);
                if (tile != null)
                {
                    sprites.RemoveFromCollisions(tile);
                }
                if (leftClick)
                {
                    Point p = autoTiles.GetTile(AutoTilesPredicate((int)point.X, (int)point.Y, leftClick, pts));
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
                if (tool != Tools.Spikes)
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
                                if (GetTile(xx, yy)?.Tag == prefix + autoTiles.Name)
                                {
                                    tile = GetTile(xx, yy);
                                    int l = tile.Layer;
                                    if (tile != null)
                                    {
                                        sprites.RemoveFromCollisions(tile);
                                    }
                                    Point p = autoTiles.GetTile(AutoTilesPredicate(xx, yy, leftClick, pts));
                                    if (autoTiles.Size2 != new Point(1, 1))
                                    {
                                        int xAdd = (int)xx / 8 % autoTiles.Size2.X * 8;
                                        int yAdd = (int)yy / 8 % autoTiles.Size2.Y * 6;
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

        private Predicate<Point> AutoTilesPredicate(int x, int y, bool leftClick, SortedSet<PointF> accountFor = null)
        {
            bool bg = tool != Tools.Ground;
            bool sp = tool == Tools.Spikes;
            Tile gt;
            return (p) =>
            {
                if (!sp && accountFor is object && accountFor.Contains(new PointF(p.X + x, p.Y + y))) return leftClick;
                int s = autoTiles.Size;
                if (s == 4)
                {
                    if (p.X != 0 && heldKeys.Contains(Key.BracketLeft)) return false;
                    if (p.Y != 0 && heldKeys.Contains(Key.BracketRight)) return false;
                }
                else if (s == 3)
                {
                    if (p.X != 0 && heldKeys.Contains(Key.BracketRight)) return false;
                    if (p.Y != 0 && heldKeys.Contains(Key.BracketLeft)) return false;
                }
                return (gt = GetTile(p.X + x, p.Y + y)) != null && ((!sp && gt.Tag == prefix + autoTiles.Name) || (bg && gt.Solid == Sprite.SolidState.Ground)) ||
                     (p.X < 0 && x % Room.ROOM_WIDTH == 0) ||
                     (p.X > 0 && x % Room.ROOM_WIDTH == RESOLUTION_WIDTH - 8) ||
                     (p.Y < 0 && y % Room.ROOM_HEIGHT == 0) ||
                     (p.Y > 0 && y % Room.ROOM_HEIGHT == RESOLUTION_HEIGHT - 8);
            };
        }

        private void HandleUserInputs()
        {
            if (isEditor && IsInputNew(Inputs.Escape))
            {
                //foreach (Script.Executor exec in CurrentScripts)
                //{
                //    foreach (VTextBox tb in exec.TextBoxes)
                //    {
                //        tb.Disappear();
                //    }
                //}
                foreach (VTextBox textBox in TextBoxes)
                {
                    textBox.Disappear();
                }
                foreach (StringDrawable sd in hudText.Values)
                {
                    hudSprites.Remove(sd);
                }
                hudText.Clear();
                CurrentScripts.Clear();
                saveRoom = false;
                CurrentState = GameStates.Editing;
                sprites.Remove(ActivePlayer);
                LoadRoom(CurrentRoom.X, CurrentRoom.Y);
                ActivePlayer.Visible = false;
                ActivePlayer.IsWarpingH = ActivePlayer.IsWarpingV = ActivePlayer.MultiplePositions = false;
                ActivePlayer.Offsets.Clear();
                selection.Visible = true;
                editorTool.Visible = true;
                Freeze = FreezeOptions.Unfrozen;
                CollectedTrinkets.Clear();
                ActivePlayer.HeldTrinkets.Clear();
                ActivePlayer.PendingTrinkets.Clear();
                CurrentSong?.FadeOut();
            }
            if (PlayerControl && (Freeze == FreezeOptions.Unfrozen || Freeze == FreezeOptions.FreezeScreen))
            {
                if (IsInputActive(Inputs.Right))
                    ActivePlayer.InputDirection = 1;
                else if (IsInputActive(Inputs.Left))
                    ActivePlayer.InputDirection = -1;
                else
                    ActivePlayer.InputDirection = 0;

                if (IsInputActive(Inputs.Kill) && Freeze != FreezeOptions.OnlySprites)
                    ActivePlayer.KillSelf();

                if (ActivePlayer.Script is object && IsInputNew(Inputs.Special))
                {
                    Script s = ActivePlayer.Script;
                    ActivePlayer.Script = null;
                    ExecuteScript(s, ActivePlayer, ActivePlayer);
                }
                if (CurrentActivityZone is object && IsInputActive(Inputs.Pause))
                {
                    CurrentActivityZone.Activated = true;
                    ExecuteScript(CurrentActivityZone.Script, CurrentActivityZone.Sprite, ActivePlayer);
                }
            }

            if (IsInputActive(Inputs.Jump))
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
        }

        public void SaveCurrentRoom()
        {
            ActivePlayer.IsWarpingH = false;
            ActivePlayer.IsWarpingV = false;
            ActivePlayer.MultiplePositions = false;
            ActivePlayer.Offsets.Clear();
            JObject r = CurrentRoom.Save(this);
            RoomDatas[FocusedRoom] = r;
            if (mapSprites.ContainsKey(FocusedRoom))
                mapSprites[FocusedRoom].Dispose();
            mapSprites.Remove(FocusedRoom);
            mapSprites.Add(FocusedRoom, MapSprite.FromRoom(r, 1, this));
            if (RoomGroups.ContainsKey(FocusedRoom))
            {
                RoomGroups[FocusedRoom].RoomDatas[FocusedRoom] = RoomDatas[FocusedRoom];
            }
        }

        public void LoadRoom(int x, int y)
        {
            if (CurrentRoom != warpRoom)
            {
                CurrentRoom.Dispose();
            }
            Color fade = sprites?.Color ?? Color.White;
            foreach (ActivityZone activityZone in ActivityZones)
            {
                if (activityZone.TextBox is object)
                    hudSprites.Remove(activityZone.TextBox);
            }
            ActivityZones.Clear();
            if (CurrentState == GameStates.Playing && CurrentRoom.ExitScript is object)
            {
                ExecuteScript(CurrentRoom.ExitScript, ActivePlayer, ActivePlayer);
            }
            foreach (Sprite sprite in indic)
            {
                hudSprites.Remove(sprite);
            }
            indic.Clear();
            if (isEditor && CurrentState == GameStates.Editing && CurrentEditingFocus != FocusOptions.Map)
            {
                if (saveRoom)
                {
                    SaveCurrentRoom();
                }
                saveRoom = true;
            }
            FocusedRoom = x + y * 100;
            if (RoomGroups.ContainsKey(FocusedRoom) && CurrentState == GameStates.Playing)
            {
                RoomGroup load = RoomGroups[FocusedRoom].Load(this);
                if (load != CurrentRoom)
                {
                    roomLoc.Text = "Room " + load.X.ToString() + ", " + load.Y.ToString();
                    CurrentRoom = load;
                    if (!CurrentRoom.Objects.Contains(ActivePlayer))
                        CurrentRoom.Objects.Add(ActivePlayer);
                    ActivePlayer.CenterX = (ActivePlayer.CenterX + Room.ROOM_WIDTH) % Room.ROOM_WIDTH + x * Room.ROOM_WIDTH;
                    ActivePlayer.CenterY = (ActivePlayer.CenterY + Room.ROOM_HEIGHT) % Room.ROOM_HEIGHT + y * Room.ROOM_HEIGHT;
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
                roomLoc.Text = "Room " + x.ToString() + ", " + y.ToString();
                roomLoc.Right = RESOLUTION_WIDTH - 4;
                if (!RoomDatas.ContainsKey(FocusedRoom))
                {
                    Room r = new Room(new SpriteCollection(), Script.Empty, Script.Empty);
                    r.X = x;
                    r.Y = y;
                    r.TileTexture = TextureFromName("tiles");
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
                if (!(isEditor && CurrentState == GameStates.Editing))
                {
                    if (!sprites.Contains(ActivePlayer))
                        CurrentRoom.Objects.Add(ActivePlayer);
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
                    ShowTileIndicators();
                    foreach (WarpToken.WarpData data in Warps)
                    {
                        if (data.OutRoom == new Point(x, y))
                        {
                            Texture sp32 = TextureFromName("sprites32");
                            WarpTokenOutput wto = new WarpTokenOutput(data.Out.X, data.Out.Y, sp32, sp32.AnimationFromName("WarpToken"), data);
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
                ExecuteScript(CurrentRoom.EnterScript, ActivePlayer, ActivePlayer);
            }
        }

        private void ShowTileIndicators()
        {
            int x = CurrentRoom.X;
            int y = CurrentRoom.Y;
            if (showIndicators)
            {
                Texture tileIndicators = TextureFromName("tileindicator");
                Point[] toCheck = new Point[] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };
                foreach (Point p in toCheck)
                {
                    int x2 = p.X, y2 = p.Y;
                    int x3 = x + x2;
                    int y3 = y + y2;
                    if (x3 < 0) x3 = WidthRooms - 1;
                    if (x3 >= WidthRooms) x3 = 0;
                    if (y3 < 0) y3 = HeightRooms - 1;
                    if (y3 >= HeightRooms) y3 = 0;
                    switch (p.X + 2 * p.Y)
                    {
                        case -2:
                            if (CurrentRoom.RoomUp.HasValue)
                            {
                                x3 = CurrentRoom.RoomUp.Value.X;
                                y3 = CurrentRoom.RoomUp.Value.Y;
                            }
                            break;
                        case -1:
                            if (CurrentRoom.RoomLeft.HasValue)
                            {
                                x3 = CurrentRoom.RoomLeft.Value.X;
                                y3 = CurrentRoom.RoomLeft.Value.Y;
                            }
                            break;
                        case 1:
                            if (CurrentRoom.RoomRight.HasValue)
                            {
                                x3 = CurrentRoom.RoomRight.Value.X;
                                y3 = CurrentRoom.RoomRight.Value.Y;
                            }
                            break;
                        case 2:
                            if (CurrentRoom.RoomDown.HasValue)
                            {
                                x3 = CurrentRoom.RoomDown.Value.X;
                                y3 = CurrentRoom.RoomDown.Value.Y;
                            }
                            break;
                    }
                    if (RoomDatas.TryGetValue(x3 + y3 * 100, out JObject room))
                    {
                        List<Tile> tiles = Room.GetTiles(room, this);
                        int[] solids = new int[x2 == 0 ? 40 : 30];
                        foreach (Tile tile in tiles)
                        {
                            int ti;
                            if (x2 == 0)
                            {
                                if (y2 == -1 && tile.Y != Room.ROOM_HEIGHT - 8)
                                    continue;
                                else if (y2 == 1 && tile.Y != 0)
                                    continue;
                                ti = (int)tile.X / 8;
                            }
                            else
                            {
                                if (x2 == -1 && tile.X != Room.ROOM_WIDTH - 8)
                                    continue;
                                else if (x2 == 1 && tile.X != 0)
                                    continue;
                                ti = (int)tile.Y / 8;
                            }
                            int ss = (int)tile.Solid + 1;
                            if (ss < solids[ti] || solids[ti] == 0)
                            {
                                solids[ti] = ss;
                            }
                        }
                        for (int i = 0; i < solids.Length; i++)
                        {
                            if (solids[i] == 0) continue;
                            int x4 = 0;
                            int y4 = 0;
                            int tx = 0;
                            int ty = 0;
                            switch (x2 + (y2 * 2))
                            {
                                case -2:
                                    x4 = i * 8;
                                    y4 = 0;
                                    tx = 0;
                                    ty = 0;
                                    break;
                                case -1:
                                    x4 = 0;
                                    y4 = i * 8;
                                    tx = 1;
                                    ty = 1;
                                    break;
                                case 1:
                                    x4 = RESOLUTION_WIDTH - 8;
                                    y4 = i * 8;
                                    tx = 0;
                                    ty = 1;
                                    break;
                                case 2:
                                    x4 = i * 8;
                                    y4 = RESOLUTION_HEIGHT - 8;
                                    tx = 1;
                                    ty = 0;
                                    break;
                            }
                            int ox = 0;
                            int oy = 0;
                            switch (solids[i])
                            {
                                case 1:
                                    ox = oy = 0;
                                    break;
                                case 2:
                                    ox = 2;
                                    oy = 0;
                                    break;
                                case 3:
                                    ox = 0;
                                    oy = 2;
                                    break;
                                case 4:
                                    ox = oy = 2;
                                    break;
                            }
                            Sprite t = new Sprite(x4, y4, tileIndicators, tx + ox, ty + oy);
                            t.Color = Color.FromArgb(100, 255, 255, 255);
                            indic.Add(t);
                            hudSprites.Add(t);
                        }
                    }
                }
            }
            else
            {
                while (indic.Count > 0)
                {
                    Sprite i = indic[0];
                    hudSprites.Remove(i);
                    indic.RemoveAt(0);
                }
            }
        }

        private void ProcessWorld()
        {
            if (CurrentActivityZone is object && (!ActivePlayer.IsOverlapping(CurrentActivityZone as Sprite) || CurrentActivityZone.Activated))
            {
                CurrentActivityZone.TextBox.Disappear();
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
                if (ActivePlayer.CenterX > CurrentRoom.Right)
                {
                    if (CurrentRoom.RoomRight.HasValue)
                        LoadRoom(CurrentRoom.RoomRight.Value.X, CurrentRoom.RoomRight.Value.Y);
                    else
                        LoadCurrentRoom();
                }
                else if (ActivePlayer.CenterX < CurrentRoom.GetX)
                {
                    if (CurrentRoom.RoomLeft.HasValue)
                        LoadRoom(CurrentRoom.RoomLeft.Value.X, CurrentRoom.RoomLeft.Value.Y);
                    else
                        LoadCurrentRoom();
                }
                if (ActivePlayer.CenterY > CurrentRoom.Bottom)
                {
                    if (CurrentRoom.RoomDown.HasValue)
                        LoadRoom(CurrentRoom.RoomDown.Value.X, CurrentRoom.RoomDown.Value.Y);
                    else
                        LoadCurrentRoom();
                }
                else if (ActivePlayer.CenterY < CurrentRoom.GetY)
                {
                    if (CurrentRoom.RoomUp.HasValue)
                        LoadRoom(CurrentRoom.RoomUp.Value.X, CurrentRoom.RoomUp.Value.Y);
                    else
                        LoadCurrentRoom();
                }
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

        public void LoadCurrentRoom()
        {
            int x = (int)Math.Floor(ActivePlayer.CenterX / Room.ROOM_WIDTH);
            int y = (int)Math.Floor(ActivePlayer.CenterY / Room.ROOM_HEIGHT);
            while (x < 0)
                x += WidthRooms;
            while (y < 0)
                y += HeightRooms;
            x %= WidthRooms;
            y %= HeightRooms;
            LoadRoom(x, y);
        }

        private PointF GetCameraTarget()
        {
            float targetX = ActivePlayer.CenterX + (ActivePlayer.XVelocity * 30) - (RESOLUTION_WIDTH / 2);
            float targetY = ActivePlayer.CenterY + (ActivePlayer.YVelocity * 30) - (RESOLUTION_HEIGHT / 2);
            if (heldKeys.Contains(Key.L))
                targetX = ActivePlayer.CenterX - RESOLUTION_WIDTH / 10;
            else if (heldKeys.Contains(Key.J))
                targetX = ActivePlayer.CenterX - RESOLUTION_WIDTH / 10 * 9;
            if (heldKeys.Contains(Key.K))
                targetY = ActivePlayer.CenterY - RESOLUTION_HEIGHT / 10;
            else if (heldKeys.Contains(Key.I))
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
            if (sprite.IsOnPlatform && !sprite.Platform.DidCollision)
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

        public void StartGame()
        {
            if (!IsPlaying)
            {
                IsPlaying = true;
                //gameThread = new Thread(GameLoop);
                //gameThread.Start();
            }
        }
        public void StopGame() { IsPlaying = false; CurrentSong?.Stop(); }

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
                    sd.Visible = true;
                }
                else
                    sd = new StringDrawable(0, 0, FontTexture, text, color);
                sd.Name = name;
                hudText.Add(name, sd);
            }
            if (!hudSprites.Contains(sd))
                hudSprites.Add(sd);
            sd.Text = text;
            sd.X = position.X;
            sd.Y = position.Y;
            sd.Color = color;
        }

        public void HudRemove(string name)
        {
            if (hudText.ContainsKey(name))
            {
                StringDrawable sd = hudText[name];
                hudSprites.Remove(sd);
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
        }

        private void glControl_Render(object sender, FrameEventArgs e)
        {
#if TEST
            Stopwatch t = new Stopwatch();
            t.Start();
#endif

            // clear the color buffer
            Color c = Color.Black;
            if (flashFrames > 0)
            {
                isFlashing = true;
                c = flashColour;
                flashFrames -= 1;
            }
            else
            {
                isFlashing = false;
                if (BGSprites is object)
                {
                    c = BGSprites.BackgroundColor;
                }
            }
            if (c != currentColor)
            {
                currentColor = c;
                GL.ClearColor((float)c.R / 255, (float)c.G / 255, (float)c.B / 255, 1);
            }
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (!isFlashing)
            {
                Matrix4 cam = camera;
                int offsetX = 0;
                int offsetY = 0;
                if (shakeFrames > 0)
                {
                    offsetX = r.Next(-shakeIntensity, shakeIntensity + 1);
                    offsetY = r.Next(-shakeIntensity, shakeIntensity + 1);
                    shakeFrames -= 1;
                }
                cam = Matrix4.CreateTranslation(-((int)CameraX + offsetX), -((int)CameraY + offsetY), 0) * cam;
                int viewMatrixLoc = GL.GetUniformLocation(program.ID, "view");

                if (CurrentEditingFocus != FocusOptions.Tileset && CurrentEditingFocus != FocusOptions.ScriptEditor && isInitialized)
                {
                    BGSprites?.RenderPrep(viewMatrixLoc, camera);
                    BGSprites?.Render(FrameCount);

                    GL.UseProgram(program.ID);
                    GL.UniformMatrix4(viewMatrixLoc, false, ref cam);
                    sprites?.Render(FrameCount, CurrentState == GameStates.Editing);
                }

                if (CurrentEditingFocus != FocusOptions.ScriptEditor)
                {
                    hudView = camera;
                    hudView = Matrix4.CreateTranslation(-offsetX, -offsetY, 0) * hudView;
                    GL.UniformMatrix4(viewMatrixLoc, false, ref hudView);
                    hudSprites.Render(FrameCount);
                    if (previews is object)
                    {
                        hudView = Matrix4.CreateTranslation(0, -previewScroll, 0) * hudView;
                        GL.UniformMatrix4(viewMatrixLoc, false, ref hudView);
                        previews.Render(FrameCount);
                    }
                }
                else
                {
                    var scrCam = camera;
                    scrCam = Matrix4.CreateTranslation(-seScrollX, -seScrollY, 0) * scrCam;
                    scrCam = Matrix4.CreateScale(seZoom, seZoom, 1) * scrCam;
                    GL.UniformMatrix4(viewMatrixLoc, false, ref scrCam);
                    scriptEditor.AllTogether.Render(FrameCount);
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
                foreach (Number number in Vars.Values)
                {
                    jo.Add(number.Name, number.AssignedValue);
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
                    Vars[num.Name].SetValue((float)num.Value);
            }
            CollectedTrinkets.Clear();
            JArray trs = (JArray)loadFrom["Trinkets"];
            foreach (JToken tr in trs)
            {
                CollectedTrinkets.Add((int)tr);
            }
            CurrentRoom = Room.LoadRoom(loadFrom["Room"], this);
            FocusedRoom = CurrentRoom.X + CurrentRoom.Y * 100;
            PointF target = GetCameraTarget();
            CameraX = target.X;
            CameraY = target.Y;
            string song = (string)loadFrom["Music"] ?? "Silence";
            if (CurrentSong.Name != song)
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
            //Templates
            arr = new JArray(SpriteTemplates.Values.ToArray());
            ret.Add("Templates", arr);
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
                rooms.Add(g.X + g.WidthRooms);
                rooms.Add(g.Y + g.HeightRooms);
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
                "replace({c},?target:trinkets,true)\n" +
                "replace({t},?totaltrinkets,true)\n" +
                "position(centerx)\n" +
                "speak\n" +
                "endtext\n" +
                "musicfadein\n" +
                "unfreeze";
            Scripts.Add("trinket", new Script(Command.ParseScript(this, c), "trinket", c));
            WidthRooms = 5;
            HeightRooms = 5;
            RoomDatas.Clear();
            ActivePlayer = new Crewman(0, 0, TextureFromName("viridian"), this, "Viridian", textBoxColor: colors["viridian"]);
            UserAccessSprites.Clear();
            UserAccessSprites.Add("Viridian", ActivePlayer);
            StartX = 0;
            StartY = 0;
            StartRoomX = 0;
            StartRoomY = 0;
            LevelTrinkets.Clear();
            SpriteTemplates.Clear();
            Texture platforms = TextureFromName("platforms");
            Texture sprites32 = TextureFromName("sprites32");
            Texture background = TextureFromName("background");
            SpriteTemplates.Add("defaultDisappear", new Platform(0, 0, platforms, platforms.AnimationFromName("platform1"), 0, 0, 0, true, platforms.AnimationFromName("disappear")).Save(this));
            SpriteTemplates.Add("defaultPlatform", new Platform(0, 0, platforms, platforms.AnimationFromName("platform1"), 0, 0, 0, false, platforms.AnimationFromName("disappear")).Save(this));
            SpriteTemplates.Add("defaultConveyor", new Platform(0, 0, platforms, platforms.AnimationFromName("conveyor1r"), 0, 0, 0, false, platforms.AnimationFromName("disappear")).Save(this));
            SpriteTemplates.Add("defaultTrinket", new Trinket(0, 0, sprites32, sprites32.AnimationFromName("Trinket"), new Script(new Command[] { }, "trinket"), this, -1).Save(this));
            SpriteTemplates.Add("defaultWarpToken", new WarpToken(0, 0, sprites32, sprites32.AnimationFromName("WarpToken"), 0, 0, 0, 0, this).Save(this));
            SpriteTemplates.Add("defaultTerminal", new Terminal(0, 0, sprites32, sprites32.AnimationFromName("TerminalOff"), sprites32.AnimationFromName("TerminalOn"), null, false, this).Save(this));
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

        public void LoadLevel(string path, string levelName)
        {
            saveRoom = false;
            JObject loadFrom = JObject.Parse(System.IO.File.ReadAllText(path + "/" + levelName + ".lv7"));
            Scripts.Clear();
            if (System.IO.Directory.Exists(path + "/scripts"))
            {
                IEnumerable<string> scriptPaths = System.IO.Directory.EnumerateFiles(path + "/scripts");
                foreach (string sc in scriptPaths)
                {
                    string scName = sc.Split(new char[] { '/', '\\' }).Last();
                    scName = scName.Substring(0, scName.Length - 4);
                    Script script = new Script(null, scName, System.IO.File.ReadAllText(sc));
                    Scripts.Add(scName, script);
                }
            }
            LoadAllTextures();
            //Settings
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
            //Templates
            {
                //SpriteTemplates.Clear();
                JArray templates = (JArray)loadFrom["Templates"];
                if (templates is object)
                {
                    foreach (JToken template in templates)
                    {
                        if ((string)template["Name"] is null) continue;
                        SpriteTemplates.Add((string)template["Name"], template);
                    }
                }
            }
            //Objects
            {
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
                                Warps.Add(data);
                            }
                        }
                    }
                }
            }
            // Map
            foreach (MapSprite ms in mapSprites.Values)
            {
                ms.Dispose();
            }
            mapSprites.Clear();
            for (int i = 0; i < RoomDatas.Count; i++)
            {
                MapSprite ms = MapSprite.FromRoom(RoomDatas.Values[i], 1, this);
                mapSprites.Add(RoomDatas.Keys[i], ms);
            }
            //Load Scripts
            {
                for (int i = 0; i < Scripts.Count; i++)
                {
                    Script script = Scripts.Values[i];
                    string contents = script.Contents;
                    script.Commands = Command.ParseScript(this, contents);
                }
            }
            //Load Player
            {
                JToken pl = loadFrom["Player"];
                if (pl.Type != JTokenType.Null)
                {
                    if (pl.Type == JTokenType.String)
                    {
                        defaultPlayer = (string)loadFrom["Player"];
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
                saveRoom = false;
                LoadRoom(startRoomX, startRoomY);
            }
            //Load Room Groups
            {
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
                            GroupList.Add(newGroup);
                        }
                    }
            }
        }
    }
}
