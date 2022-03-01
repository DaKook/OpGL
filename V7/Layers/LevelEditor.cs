using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace V7
{
    public class LevelEditor : SpritesLayer
    {
        public SpriteCollection hudSprites;
        public SpriteCollection Sprites
        {
            get => Owner.CurrentRoom?.Objects;
        }

        public enum Tools { Ground, Background, Spikes, Trinket, Checkpoint, Disappear, Conveyor, Platform, Enemy, GravityLine, Start, Crewman, WarpLine, WarpToken, ScriptBox, Terminal, RoomText, Lever, Tiles, Select, CustomSprite, Point, Attach }
        private EditorTool[] EditorTools = new EditorTool[] {
            //new EditorTool('1', "Ground", "Auto Tiles connect only to the same ground tiles in the same layer.\n     Z: 3x3 brush\n     X: 5x5 brush\n     F: Fill\nHold shift to toggle brush/fill lock. Shift+C lets you set your own brush size.\n(Tip: Hold [ to lock Y-axis, and ] to lock X-axis. These also restrict the fill tool to the opposite axis.)"),
            //new EditorTool('2', "Background", "Auto Tiles connect to the same tiles in the same layer, and also to solid tiles.\n     Z: 3x3 brush\n     X: 5x5 brush\n     F: Fill\nHold shift to toggle brush/fill lock. Shift+C lets you set your own brush size.\n(Tip: Hold [ to lock Y-axis, and ] to lock X-axis. These also restrict the fill tool to the opposite axis, as well as preventing tiles from connecting to solid tiles outside the axis.)"),
            //new EditorTool('3', "Spikes", "Auto Tiles only connect to solid tiles.\nHold F to fill a surface with spikes.\n(Tip: Hold [ to lock Y-axis, and ] to lock X-axis.)"),
            //new EditorTool('4', "Trinket", "Trinkets are collectibles and are not necessarily required. The number of collected trinkets can be accessed in scripting using \"?trinkets\", and the total amout of trinkets in the level can be accessed with \"?totaltrinkets\"."),
            //new EditorTool('5', "Checkpoint", "When touched by a crewman, the crewman's respawn point is set to the checkpoint's position.\n     Z: Flip the checkpoint upside-down\n     X: Flip the checkpoint to face left"),
            //new EditorTool('6', "Disappear", "Platforms that disappear when a crewman stands on them.\n     To specify length, hold shift and click-and-drag."),
            //new EditorTool('7', "Conveyor", "Conveyors push any crewman standing on them in a certain direction. After placing one, press either left or right to specify the direction of the conveyor.\n     To specify length, hold shift and click-and-drag."),
            //new EditorTool('8', "Platform", "Platforms that move in a certain direction. After placing one, press any direction to specify which way the platform should move.\n     To specify length, hold shift and click-and-drag.\n     Middle-click for a shortcut to set a platform's bounds."),
            //new EditorTool('9', "Enemy", "Crewmen die upon touching an enemy. Enemies move in a certain direction. After placing one, press any direction to specify which way the enemy should move.\n     Middle-click for a shortcut to set an enemy's bounds."),
            //new EditorTool('0', "Grav Line", "Gravity lines flip the gravity of any crewman who touches them. Click-and-drag to specify the length and orientation of a Gravity Line."),
            //new EditorTool('P', "Start", "Left-click to set the start position. Right-click to go to the start room.\n     Z: Flip the player upside-down\n     X: Flip the player to face left"),
            //new EditorTool('O', "Crewman", "Place any crewman. A crewman's texture must contain at least the following animations: Standing, Walking, and Dying.\n     Z: Flip the crewman upside-down\n     X: Flip the crewman to face left"),
            //new EditorTool('I', "Warp Line", "When placed on the edge of the room, Warp Lines will warp any moving object, including crewmen, to the opposite side of the room. Click-and-drag to set the length and orientation of a Warp Line."),
            //new EditorTool('U', "Warp Token", "Warp Tokens teleport crewmen to a specified location. After placing a Warp Token, you must set the output. This is done just like placing a Warp Token.\nMiddle-click a Warp Token to go to its output, or an output to go to its input."),
            //new EditorTool('Y', "Script Box", "Script Boxes run a script when touched by the player, then are deactivated until the room is reloaded. Click-and-drag to set the size of a Script Box. After placing one, type the name of the script for it to run. To move/resize a Script Box, click on it while holding Shift. Middle-click a Script Box to edit its script."),
            //new EditorTool('T', "Terminal", "Terminals can be activated by the player by pressing Enter. After placing one, type the name of the script for it to run.\n     Z: Flip the terminal upside-down\n     X: Flip the terminal to face left\nMiddle-click a Terminal to edit its script."),
            //new EditorTool('R', "Roomtext", "Roomtext has no hitbox, and is only used to display text to the player. It can be used as warnings and guides. Click anywhere and start typing. Press Enter to confirm, or press Escape to cancel."),
            //new EditorTool(';', "Lever", "Levers can be interacted with exactly as terminals; press enter to activate them. A script executed by the lever can check if it is on with \"?this:on\".\n     Z: Flip the lever upside-down\n     X: Rotate the lever onto a wall"),
            //new EditorTool('-', "Tiles", "Tiles placed individually. Press Tab to open/close the tileset to select a tile. Middle-click on a tile in the room to instantly select it.\n     Z: 3x3 brush\n     X: 5x5 brush\n     F: Fill\nHold shift to toggle brush/fill lock. Shift+C lets you set your own brush size.\nUse WASD to move the selected tile.\n(Tip: Hold [ to lock Y-axis, and ] to lock X-axis. These also restrict the fill tool to the opposite axis.)"),
            //new EditorTool('=', "Select", "Select multiple objects at once. Middle-click to edit a property of the selected object(s), and right-click for more options.\n     Use the Arrow Keys to move selected objects. Hold Alt to move them one pixel at a time.\n     Press Delete to delete the selected object(s).\n     Hold Control while selecting to select Tiles and Script Boxes.\n     Hold Shift while selecting to select more objects.\n     Press escape to deselect everything.\n     Use Control+A to select everything in the room."),
            //new EditorTool('`', "Custom Sprite", "Places a sprite with no hitbox with any texture or animation.\n     Z: Flip the sprite upside-down\n     X: Flip the sprite to face left")
        };
        private RectangleSprite descBack;
        private StringDrawable descText;
        private int tileToolW = 1;
        private int tileToolH = 1;
        private int tileToolDefW = 1;
        private int tileToolDefH = 1;
        private Tools tool = Tools.Ground;
        private Tools prTool = Tools.Ground;
        public enum FocusOptions { Level, Tileset }
        public FocusOptions CurrentEditingFocus = FocusOptions.Level;
        private BoxSprite selection;
        private Point currentTile = new Point(0, 0);
        private TileTexture currentTexture
        {
            get
            {
                return Owner.CurrentRoom.TileTexture ?? Owner.TextureFromName("tiles") as TileTexture;
            }
        }
        private AutoTileSettings autoTiles
        {
            get
            {
                if (tool == Tools.Background) return Owner.CurrentRoom.Background;
                else if (tool == Tools.Spikes) return Owner.CurrentRoom.Spikes;
                else return Owner.CurrentRoom.Ground;
            }
            set
            {
                if (tool == Tools.Background) Owner.CurrentRoom.Background = value;
                else if (tool == Tools.Spikes) Owner.CurrentRoom.Spikes = value;
                else Owner.CurrentRoom.Ground = value;
            }
        }
        private FullImage tileset;
        private BoxSprite tileSelection;
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
        private int tileLayer = -2;
        private Point tileScroll;
        private bool replaceTiles = false;
        private bool replaceAutoTiles = false;
        private Color roomColor => Owner.CurrentRoom.Color;
        private Action<Keys> GiveDirection = null;
        private WarpToken currentWarp = null;
        public Room WarpRoom { get; private set; } = null;
        private SpriteGroup editorToolbarTop;
        private StringDrawable toolPrompt = null;
        private bool toolPromptImportant = false;
        private Tile previewTile = null;
        private RectangleSprite topEditor = null;
        public string CurrentLevelPath { get; private set; } = "";
        private bool currentlyBinding = false;
        private Action<Rectangle> bindSprite = null;
        public StringDrawable RoomLoc { get; private set; } = null;
        private StringDrawable selecLoc = null;
        private char prefix = 'g';
        private bool isFill => Owner.IsKeyHeld(Keys.F) || fillLock;
        private bool fillLock = false;
        private ScriptBox currentlyResizing = null;
        public bool SaveRoom { get; set; } = true;
        bool showIndicators = true;
        private List<Sprite> indic = new List<Sprite>();
        private bool hideToolbars;
        private JObject editorState;

        private Texture enemyTexture;
        private string enemyAnimation = "Enemy1";
        private Texture customSpriteTexture;
        private string customSpriteAnimation = "TerminalOff";
        private Texture leverTexture;
        private string leverAnimation = "Lever";
        private CrewmanTexture crewmanTexture;
        private Texture platformTexture;
        private string platformAnimation = "platform1";
        private string disappearAnimation = "disappear";
        private string conveyorAnimation = "conveyor1";
        private Texture terminalTexture;
        private string terminalOff = "TerminalOff";
        private string terminalOn = "TerminalOn";
        private Texture warpTokenTexture;
        private string warpTokenAnimation = "WarpToken";
        private Texture trinketTexture;
        private string trinketAnimation = "Trinket";

        private EditorTool currentTool;

        public SortedList<int, RoomGroup> RoomGroups => Owner.RoomGroups;
        public int FocusedRoom => Owner.FocusedRoom;
        public int MouseX => Owner.MouseX;
        public int MouseY => Owner.MouseY;
        public bool LeftMouse => Owner.LeftMouse;
        public bool RightMouse => Owner.RightMouse;
        public bool MiddleMouse => Owner.MiddleMouse;
        public Room CurrentRoom { get => Owner.CurrentRoom; set => Owner.CurrentRoom = value; }
        public SortedList<int, JObject> RoomDatas => Owner.RoomDatas;
        public float CameraX { get => Owner.CameraX; set => Owner.CameraX = value; }
        public float CameraY { get => Owner.CameraY; set => Owner.CameraY = value; }
        public int WidthRooms { get => Owner.WidthRooms; set => Owner.WidthRooms = value; }
        public int HeightRooms { get => Owner.HeightRooms; set => Owner.HeightRooms = value; }
        public Crewman Player { get => Owner.ActivePlayer; set => Owner.ActivePlayer = value; }

        public SpriteCollection BoundsSprites;
        public bool ShowBoundsBoxes;

        public SpriteCollection ExtraHud;
        public StringDrawable TrinketCount;
        public override bool UsesExtraHud => true;

        public LevelEditor(Game game)
        {
            Owner = game;
            hudSprites = new SpriteCollection();
            BoundsSprites = new SpriteCollection();
            ExtraHud = new SpriteCollection();
            Texture toolTexture = Owner.TextureFromName("tools");
            Sprite trinket = new Sprite(-Game.HUD_LEFT + 8, 8, toolTexture, 0, 3, Color.FromArgb(255, 255, 255, 255));
            ExtraHud.Add(trinket);
            TrinketCount = new StringDrawable(0, 40, Owner.FontTexture, "", Color.White);
            TileTexture tiles = Owner.TextureFromName("tiles") as TileTexture;
            Texture sprites32 = Owner.TextureFromName("sprites32");
            selection = new BoxSprite(0, 0, Owner.BoxTexture, 1, 1, Color.Blue);
            tileSelection = new BoxSprite(0, 0, Owner.BoxTexture, 1, 1, Color.Red);
            selection.Visible = false;
            tileset = new FullImage(0, 0, tiles) { Layer = -1 };
            editorTool = new StringDrawable(4, 4, Owner.FontTexture, "1 - Ground", Color.White) { Layer = 2 };
            customSpriteTexture = Owner.TextureFromName("sprites32");
            customSpriteAnimation = "TerminalOff";
            toolPrompt = new StringDrawable(4, 12, Owner.FontTexture, "._.", Color.LightBlue) { Layer = 2 };
            previewTile = new Tile(4, 12, tiles, 0, 0) { Layer = 2 };
            RoomLoc = new StringDrawable(0, 4, Owner.FontTexture, "Room 0, 0", Color.Gray)
            {
                Right = Game.RESOLUTION_WIDTH - 4,
                Layer = 2
            };
            selecLoc = new StringDrawable(0, 4, Owner.NonMonoFont, "[0,0]", Color.Gray)
            {
                Right = RoomLoc.X - 4,
                Layer = 2
            };
            topEditor = new RectangleSprite(0, 0, Game.RESOLUTION_WIDTH, 22)
            {
                Color = Color.FromArgb(100, 0, 0, 0),
                Layer = 1
            };
            editorToolbarTop = new SpriteGroup(topEditor, editorTool, previewTile, toolPrompt, selecLoc, RoomLoc);
            enemyTexture = Owner.TextureFromName("enemies");
            leverTexture = sprites32;
            platformTexture = Owner.TextureFromName("platforms");
            trinketTexture = sprites32;
            terminalTexture = sprites32;
            warpTokenTexture = sprites32;
            Texture lines = Owner.TextureFromName("lines");
            EditorTools = new EditorTool[]
            {
                new GroundTool(this, tiles),
                new BackgroundTool(this, tiles),
                new SpikesTool(this, tiles),
                new TrinketTool(this, sprites32),
                new CheckpointTool(this, sprites32),
                new PlatformTool(this, platformTexture, 0),
                new PlatformTool(this, platformTexture, 1),
                new PlatformTool(this, platformTexture, 2),
                new EnemyTool(this, enemyTexture),
                new GravityLineTool(this, lines),
                new StartTool(this),
                new CrewmanTool(this),
                new WarpLineTool(this),
                new WarpTokenTool(this, sprites32),
                new ScriptBoxTool(this),
                new TerminalTool(this, sprites32),
                new RoomTextTool(this),
                new TilesTool(this, tiles)
            };
            currentTool = EditorTools[0];
            Darken = 0;
        }

        public override void Dispose()
        {
            
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            //throw new NotImplementedException();
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (typing) return;
            if (currentTool.TakeInput)
            {
                currentTool.HandleKey(e);
                if (!e.Pass)
                    return;
            }
            if (CurrentEditingFocus == FocusOptions.Level)
            {
                if (tool == Tools.Point)
                {
                    if (e.Key == Keys.Escape)
                    {
                        tool = prTool;
                        EditorTool t = EditorTools[(int)prTool];
                        editorTool.Text = t.DefaultKey + " - " + t.DefaultName;
                        typing = true;
                    }
                    return;
                }
                if (currentlyBinding)
                {
                    if (e.Key == Keys.Enter)
                    {
                        bindSprite?.Invoke(Rectangle.Empty);
                        currentlyBinding = false;
                        toolPromptImportant = false;
                    }
                    else if (e.Key == Keys.Escape)
                    {
                        currentlyBinding = false;
                        toolPromptImportant = false;
                    }
                    return;
                }
                if (e.Control && e.Key == Keys.S)
                {
                    string p = "levels/" + CurrentLevelPath + "/" + CurrentLevelPath + ".lv7";
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
                        Owner.ShowDialog("Level name:", CurrentLevelPath, files.ToArray(), (r, st) =>
                        {
                            if (r)
                            {
                                CurrentLevelPath = st;
                                p = "levels/" + CurrentLevelPath + "/" + CurrentLevelPath + ".lv7";
                                string sv = Newtonsoft.Json.JsonConvert.SerializeObject(Owner.SaveLevel());
                                if (!System.IO.Directory.Exists("levels/" + CurrentLevelPath))
                                    System.IO.Directory.CreateDirectory("levels/" + CurrentLevelPath);
                                System.IO.File.WriteAllText(p, sv);
                            }
                            else
                                return;
                        });
                    }
                    else
                    {
                        string str = Newtonsoft.Json.JsonConvert.SerializeObject(Owner.SaveLevel());
                        System.IO.File.WriteAllText(p, str);
                    }
                    return;
                }
                else if (e.Control && e.Key == Keys.O)
                {
                    IEnumerable<string> dirs = System.IO.Directory.EnumerateDirectories("levels");
                    List<string> files = new List<string>();
                    foreach (string dir in dirs)
                    {
                        string s = dir.Split('\\').Last();
                        if (System.IO.File.Exists(dir + "\\" + s + ".lv7"))
                            files.Add(s);
                    }
                    IEnumerable<string> lvs = System.IO.Directory.EnumerateFiles("levels");
                    foreach (string dir in lvs)
                    {
                        string s = dir.Split('\\').Last();
                        if (dir.EndsWith(".lv7"))
                            files.Add(s);
                    }
                    files.Sort();
                    Owner.ShowDialog("Load level...", CurrentLevelPath, files.ToArray(), (r, st) =>
                    {
                        if (r)
                        {
                            if (System.IO.File.Exists("levels/" + st + "/" + st + ".lv7"))
                            {
                                CurrentLevelPath = st;
                                Owner.LoadLevel("levels/" + st, st);
                            }
                            else if (System.IO.File.Exists("levels/" + st) && st.EndsWith(".lv7"))
                            {
                                CurrentLevelPath = st;
                                Owner.LoadLevel("", st);
                            }
                            else
                                Owner.NewLevel();
                        }
                    });
                    return;
                }
                else if (e.Control && e.Key == Keys.N)
                {
                    Owner.NewLevel();
                    CurrentLevelPath = "";
                }
                else if (e.Shift && e.Alt && e.Key == Keys.Backspace)
                {
                    VTextBox tb = new VTextBox(0, 0, Owner.FontTexture, "Debug feature disabled :/", Owner.GetColor("terminal") ?? Color.White);
                    tb.CenterX = Game.RESOLUTION_WIDTH / 2;
                    tb.CenterY = Game.RESOLUTION_HEIGHT / 2;
                    tb.frames = 60;
                    tb.Disappeared += (b) => { hudSprites.Remove(b); b.Dispose(); };
                    hudSprites.Add(tb);
                    tb.Appear();
                }
                else if (e.Shift && e.Key == Keys.S)
                {
                    Owner.OpenScripts();
                    return;
                }
                else if (e.Key == Keys.Q)
                {
                    bool shift = e.Shift;
                    string[] scriptNames = new string[Owner.Scripts.Count];
                    for (int i = 0; i < scriptNames.Length; i++)
                    {
                        scriptNames[i] = Owner.Scripts.Values[i].Name;
                    }
                    string message = shift ? "Room Exit Script?" : "Room Enter Script?";
                    Room room = RoomGroups.ContainsKey(FocusedRoom) ? RoomGroups[FocusedRoom] : CurrentRoom;
                    string name = shift ? room.ExitScript?.Name ?? "" : room.EnterScript?.Name ?? "";
                    Owner.ShowDialog(message, name, scriptNames, (r, st) =>
                    {
                        if (r)
                        {
                            Script s = Owner.ScriptFromName(st);
                            if (s is null)
                            {
                                s = new Script(new Command[] { }, st, "");
                                Owner.Scripts.Add(s.Name, s);
                            }
                            if (shift)
                                room.ExitScript = s;
                            else
                                room.EnterScript = s;
                        }
                    });
                }
                else if (e.Key == Keys.F1)
                {
                    List<string> textureNames = new List<string>();
                    foreach (AutoTileSettings.PresetGroup grp in Owner.RoomPresets.Values)
                    {
                        textureNames.Add(grp.Name);
                    }
                    Owner.ShowDialog("Change room tileset?", CurrentRoom.GroupName ?? "", textureNames.ToArray(), (r, st) =>
                    {
                        if (r)
                        {
                            string answer = st;
                            AutoTileSettings.PresetGroup g = Owner.RoomPresets[answer];
                            int ind = 0;
                            if (Owner.RoomPresets.ContainsKey(CurrentRoom.GroupName ?? ""))
                                ind = Owner.RoomPresets[CurrentRoom.GroupName].IndexOfKey(CurrentRoom.PresetName ?? "");
                            ind %= g.Count;
                            if (ind == -1) ind = 0;
                            AutoTileSettings.RoomPreset p = g.Values[ind];
                            TileTexture t = p.Texture;
                            if (t != null && CurrentRoom.TileTexture != t)
                            {
                                foreach (Sprite tile in Sprites)
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
                            replaceAutoTiles = false;
                        }
                    });
                }
                else if (e.Key == Keys.F2)
                {
                    List<string> textureNames = new List<string>();
                    if (!Owner.RoomPresets.ContainsKey(CurrentRoom.GroupName ?? "")) CurrentRoom.GroupName = "Space Station";
                    PreviewScreen ps = new PreviewScreen(new Sprite[] { }, null, Owner);
                    int x = 20;
                    int y = 20;
                    float maxHeight = 0;
                    foreach (AutoTileSettings.RoomPreset grp in Owner.RoomPresets[CurrentRoom.GroupName].Values)
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
                        if (s.Right > Game.RESOLUTION_WIDTH - 20)
                        {
                            y += (int)s.Height + 16;
                            x = 20;
                            s.X = x;
                            s.Y = y;
                        }
                        StringDrawable name = new StringDrawable(x, y + s.Height + 2, Owner.NonMonoFont, init.Name, Color.White);
                        x += (int)s.Width + 8;
                        s.Name = grp.Name;
                        ps.Sprites.Add(s);
                        ps.Sprites.Add(name);
                    }
                    ps.OnClick = (s) =>
                    {
                        ChangeRoomColour(s.Name);
                    };
                    ps.MaxScroll = Math.Max(y + (int)maxHeight + 20 - Game.RESOLUTION_HEIGHT, 0);
                    Owner.AddLayer(ps);

                }
                else if (e.Key == Keys.F3)
                {
                    Owner.LoadAllTextures();
                    VTextBox tb = new VTextBox(0, 0, Owner.FontTexture, "Reloaded Textures", Color.Gray);
                    tb.CenterX = Game.RESOLUTION_WIDTH / 2;
                    tb.CenterY = Game.RESOLUTION_HEIGHT / 2;
                    tb.Layer = 100;
                    tb.frames = 75;
                    tb.Disappeared += (t) => hudSprites.Remove(t);
                    hudSprites.Add(tb);
                    tb.Appear();
                }
                else if (e.Key == Keys.F4)
                {
                    ShowBoundsBoxes = !ShowBoundsBoxes;
                    UpdateBoundsBoxes();
                }
                else if (e.Key == Keys.F5)
                {
                    TextureEditor te = new TextureEditor(Owner, Owner.TextureFromName("tiles3"), "textures/tiles3_data_test.txt");
                    Owner.AddLayer(te);
                }
                else if (e.Key == Keys.F11)
                {
                    hideToolbars = !hideToolbars;
                }
                if (selectedSprites.Count == 0)
                {
                    if (GiveDirection is null)
                    {
                        if (CurrentRoom is RoomGroup)
                        {
                            if (e.Control)
                            {
                                if (e.Key == Keys.Right)
                                {
                                    CameraX = CurrentRoom.Right - Game.RESOLUTION_WIDTH;
                                }
                                else if (e.Key == Keys.Left)
                                {
                                    CameraX = CurrentRoom.GetX;
                                }
                                else if (e.Key == Keys.Down)
                                {
                                    CameraY = CurrentRoom.Bottom - Game.RESOLUTION_HEIGHT;
                                }
                                else if (e.Key == Keys.Up)
                                {
                                    CameraY = CurrentRoom.GetY;
                                }
                            }
                            else
                            {
                                if (e.Key == Keys.Right)
                                {
                                    CameraX += 32;
                                    if (CameraX > CurrentRoom.Right - Game.RESOLUTION_WIDTH)
                                        CameraX = CurrentRoom.Right - Game.RESOLUTION_WIDTH;
                                }
                                else if (e.Key == Keys.Left)
                                {
                                    CameraX -= 32;
                                    if (CameraX < CurrentRoom.GetX)
                                        CameraX = CurrentRoom.GetX;
                                }
                                else if (e.Key == Keys.Down)
                                {
                                    CameraY += 32;
                                    if (CameraY > CurrentRoom.Bottom - Game.RESOLUTION_HEIGHT)
                                        CameraY = CurrentRoom.Bottom - Game.RESOLUTION_HEIGHT;
                                }
                                else if (e.Key == Keys.Up)
                                {
                                    CameraY -= 32;
                                    if (CameraY < CurrentRoom.GetY)
                                        CameraY = CurrentRoom.GetY;
                                }
                            }
                        }
                        else
                        {
                            if (e.Control)
                            {
                                if (e.Key == Keys.Right)
                                {
                                    MapLayer m = Owner.ShowMap(0, 0, Game.RESOLUTION_WIDTH, Game.RESOLUTION_HEIGHT);
                                    m.OnClick = (x, y) => CurrentRoom.RoomRight = new Point(x, y);
                                    m.AllowEscape = true;
                                }
                                else if (e.Key == Keys.Left)
                                {
                                    MapLayer m = Owner.ShowMap(0, 0, Game.RESOLUTION_WIDTH, Game.RESOLUTION_HEIGHT);
                                    m.OnClick = (x, y) => CurrentRoom.RoomLeft = new Point(x, y);
                                    m.AllowEscape = true;
                                }
                                else if (e.Key == Keys.Up)
                                {
                                    MapLayer m = Owner.ShowMap(0, 0, Game.RESOLUTION_WIDTH, Game.RESOLUTION_HEIGHT);
                                    m.OnClick = (x, y) => CurrentRoom.RoomUp = new Point(x, y);
                                    m.AllowEscape = true;
                                }
                                else if (e.Key == Keys.Down)
                                {
                                    MapLayer m = Owner.ShowMap(0, 0, Game.RESOLUTION_WIDTH, Game.RESOLUTION_HEIGHT);
                                    m.OnClick = (x, y) => CurrentRoom.RoomDown = new Point(x, y);
                                    m.AllowEscape = true;
                                }
                            }
                            else
                            {
                                if (e.Key == Keys.Right)
                                {
                                    RoomDatas[FocusedRoom] = CurrentRoom.Save(Owner);
                                    if (e.Shift && CurrentRoom.X == WidthRooms - 1 && WidthRooms < 100)
                                    {
                                        WidthRooms += 1;
                                    }
                                    Owner.LoadRoom((CurrentRoom.X + 1) % WidthRooms, CurrentRoom.Y);
                                }
                                else if (e.Key == Keys.Left)
                                {
                                    RoomDatas[FocusedRoom] = CurrentRoom.Save(Owner);
                                    int x = CurrentRoom.X - 1;
                                    if (x < 0) x = WidthRooms - 1;
                                    Owner.LoadRoom(x, CurrentRoom.Y);
                                }
                                else if (e.Key == Keys.Down)
                                {
                                    RoomDatas[FocusedRoom] = CurrentRoom.Save(Owner);
                                    if (e.Shift && CurrentRoom.Y == HeightRooms - 1 && HeightRooms < 100)
                                    {
                                        HeightRooms += 1;
                                    }
                                    Owner.LoadRoom(CurrentRoom.X, (CurrentRoom.Y + 1) % HeightRooms);
                                }
                                else if (e.Key == Keys.Up)
                                {
                                    RoomDatas[FocusedRoom] = CurrentRoom.Save(Owner);
                                    int y = CurrentRoom.Y - 1;
                                    if (y < 0) y = HeightRooms - 1;
                                    Owner.LoadRoom(CurrentRoom.X, y);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (e.Key == Keys.Up || e.Key == Keys.Down || e.Key == Keys.Left || e.Key == Keys.Right || e.Key == Keys.Enter || e.Key == Keys.Escape)
                        {
                            GiveDirection(e.Key);
                            GiveDirection = null;
                            return;
                        }
                    }
                }
                else
                {
                    if (e.Key == Keys.Right)
                    {
                        int m = e.Alt ? 1 : 8;
                        for (int i = 0; i < selectedSprites.Count; i++)
                        {
                            selectedSprites[i].X += m;
                            selectBoxes[i].X += m;
                        }
                    }
                    else if (e.Key == Keys.Left)
                    {
                        int m = e.Alt ? 1 : 8;
                        for (int i = 0; i < selectedSprites.Count; i++)
                        {
                            selectedSprites[i].X -= m;
                            selectBoxes[i].X -= m;
                        }
                    }
                    else if (e.Key == Keys.Down)
                    {
                        int m = e.Alt ? 1 : 8;
                        for (int i = 0; i < selectedSprites.Count; i++)
                        {
                            selectedSprites[i].Y += m;
                            selectBoxes[i].Y += m;
                        }
                    }
                    else if (e.Key == Keys.Up)
                    {
                        int m = e.Alt ? 1 : 8;
                        for (int i = 0; i < selectedSprites.Count; i++)
                        {
                            selectedSprites[i].Y -= m;
                            selectBoxes[i].Y -= m;
                        }
                    }
                    else if (e.Key == Keys.Escape)
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
                    else if (e.Key == Keys.Delete)
                    {
                        for (int i = 0; i < selectedSprites.Count; i++)
                        {
                            Owner.DeleteSprite(selectedSprites[i]);
                            Sprites.Remove(selectBoxes[i]);
                        }
                        selectedSprites.Clear();
                        selectBoxes.Clear();
                    }
                }
                if (!e.Control)
                {
                    if (e.Key == Keys.Escape)
                    {
                        LevelEditorMenu();
                    }
                    // TOOLS
                    for (int i = 0; i < EditorTools.Length; i++)
                    {
                        if (EditorTools[i].Keybind == e.Key)
                        {
                            currentTool = EditorTools[i];
                        }
                    }

                    if (e.Key == Keys.Period)
                    {
                        int t = (int)tool;
                        t = (t + 1) % EditorTools.Length;
                        tool = (Tools)t;
                        ClearSelection();
                        EditorTool et = EditorTools[t];
                        editorTool.Text = et.DefaultKey + " - " + et.DefaultName;
                        if (tool == Tools.Ground) prefix = 'g';
                        else if (tool == Tools.Background) prefix = 'b';
                        else if (tool == Tools.Spikes) prefix = 's';
                    } // Next Tool (Period)
                    else if (e.Key == Keys.Comma)
                    {
                        int t = (int)tool;
                        t = (t - 1 + EditorTools.Length) % EditorTools.Length;
                        tool = (Tools)t;
                        ClearSelection();
                        EditorTool et = EditorTools[t];
                        editorTool.Text = et.DefaultKey + " - " + et.DefaultName;
                        if (tool == Tools.Ground) prefix = 'g';
                        else if (tool == Tools.Background) prefix = 'b';
                        else if (tool == Tools.Spikes) prefix = 's';
                    } // Previous Tool (Comma)
                    else if (e.Key == Keys.Space)
                    {
                        PreviewScreen ps = new PreviewScreen(new Sprite[] { }, null, Owner);
                        RectangleSprite rs = null;
                        Texture t = Owner.TextureFromName("tools");
                        for (int i = 0; i < EditorTools.Length; i++)
                        {
                            rs = new RectangleSprite(16, 16 + (i * 32), Game.RESOLUTION_WIDTH - 32, 30);
                            rs.Color = Color.Gray;
                            rs.Layer = -1;
                            rs.Name = i.ToString();
                            ps.Sprites.Add(rs);
                            StringDrawable sd = new StringDrawable(52, 0, Owner.FontTexture, EditorTools[i].DefaultKey + " - " + EditorTools[i].DefaultName);
                            sd.Color = Color.Black;
                            sd.CenterY = rs.CenterY;
                            ps.Sprites.Add(sd);
                            Sprite sp = new Sprite(rs.X + 2, rs.Y - 1, t, 0, i);
                            ps.Sprites.Add(sp);
                        }
                        ps.MaxScroll = (int)Math.Max(rs.Bottom + 16 - Game.RESOLUTION_HEIGHT, 0);
                        ps.OnClick = (s) =>
                        {
                            if (int.TryParse(s.Name, out int tn))
                            {
                                ClearSelection();
                                EditorTool et = EditorTools[tn];
                                tool = (Tools)tn;
                                editorTool.Text = et.DefaultKey + " - " + et.DefaultName;
                                if (tool == Tools.Ground) prefix = 'g';
                                else if (tool == Tools.Background) prefix = 'b';
                                else if (tool == Tools.Spikes) prefix = 's';
                            }
                        };
                        Owner.AddLayer(ps);
                    } // Open Tool Select (Space)
                    // Open Tileset (Tab)
                    else if (e.Key == Keys.Tab && (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Spikes || tool == Tools.Tiles))
                    {
                        
                    }
                    // Begin Playtest (Enter)
                    else if (e.Key == Keys.Enter)
                    {
                        ClearSelection();
                        Owner.CurrentSong = Owner.LevelMusic;
                        Owner.CurrentSong.Rewind();
                        Owner.CurrentSong.Play();
                        if (CurrentRoom is RoomGroup)
                        {
                            int x = (int)CameraX / Game.RESOLUTION_WIDTH;
                            int y = (int)CameraY / Game.RESOLUTION_HEIGHT;
                            Owner.LoadRoom(x, y);
                        }
                        editorState = Owner.CreateSave();
                        Player.Visible = true;
                        bool pl = CurrentRoom.Objects.Contains(Player);
                        if (SaveRoom) Owner.SaveCurrentRoom();
                        Owner.CurrentState = Game.GameStates.Playing;
                        Owner.LoadRoom(CurrentRoom.X, CurrentRoom.Y);
                        if (!pl)
                        {
                            List<Sprite> col = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                            bool found = false;
                            for (int i = 0; i < col.Count; i++)
                            {
                                if (col[i] is Checkpoint)
                                {
                                    Player.CenterX = col[i].CenterX;
                                    if (col[i].FlipY)
                                    {
                                        Player.Y = col[i].Y;
                                        Player.Gravity = -Math.Abs(Player.Gravity);
                                    }
                                    else
                                    {
                                        Player.Bottom = col[i].Bottom;
                                        Player.Gravity = Math.Abs(Player.Gravity);
                                    }
                                    Player.FlipX = col[i].FlipX;
                                    Player.FlipY = col[i].FlipY;
                                    found = true;
                                    col[i].HandleCrewmanCollision(Player);
                                    break;
                                }
                            }
                            if (!found)
                            {
                                for (int i = 0; i < Sprites.Count; i++)
                                {
                                    if (Sprites[i] is Checkpoint)
                                    {
                                        Player.CenterX = Sprites[i].CenterX;
                                        if (Sprites[i].FlipY)
                                        {
                                            Player.Y = Sprites[i].Y;
                                            Player.Gravity = -Math.Abs(Player.Gravity);
                                        }
                                        else
                                        {
                                            Player.Bottom = Sprites[i].Bottom;
                                            Player.Gravity = Math.Abs(Player.Gravity);
                                        }
                                        Player.FlipX = Sprites[i].FlipX;
                                        Player.FlipY = Sprites[i].FlipY;
                                        found = true;
                                        Sprites[i].HandleCrewmanCollision(Player);
                                        break;
                                    }
                                }
                            }
                            if (!found)
                            {
                                if (!Owner.IsOutsideRoom(selection.X + CameraX, selection.Y + CameraY))
                                {
                                    Tile t = Owner.GetTile((int)(selection.X + CameraX), (int)(selection.Y - 8 + CameraY));
                                    if (t is object && t.Solid == Sprite.SolidState.Ground)
                                    {
                                        Player.Y = t.Bottom;
                                        Player.CenterX = t.CenterX;
                                        Player.FlipX = false;
                                        Player.FlipY = true;
                                        Player.Gravity = -Math.Abs(Player.Gravity);
                                    }
                                    else
                                    {
                                        Player.Bottom = selection.Bottom + CameraY;
                                        Player.CenterX = selection.CenterX + CameraX;
                                        Player.FlipX = false;
                                        Player.FlipY = false;
                                        Player.Gravity = Math.Abs(Player.Gravity);
                                    }
                                }
                                else
                                {

                                }
                            }
                        }
                        Owner.IgnoreAction();
                        if (!Sprites.Contains(Player))
                            Sprites.Add(Player);
                        Owner.RemoveLayer(this, false);
                    }
                    // Set Room Name (E)
                    else if (e.Key == Keys.E)
                    {
                        Owner.ShowRoomName();
                        Owner.RoomName.Text = CurrentRoom.Name;
                        string rn = CurrentRoom.Name;
                        Owner.StartTyping(Owner.RoomName, (r, st) =>
                        {
                            if (!r)
                                Owner.RoomName.Text = rn;
                            CurrentRoom.Name = Owner.RoomName.Text;
                            if (Owner.RoomName.Text == "")
                            {
                                Owner.HideRoomName();
                            }
                        }, true);
                    }
                    // Show Map (M)
                    else if (e.Key == Keys.M)
                    {
                        Owner.LoadRoom(CurrentRoom.X, CurrentRoom.Y);
                        MapLayer m = Owner.ShowMap(15, 15, Game.RESOLUTION_WIDTH - 30, Game.RESOLUTION_HEIGHT - 30, showAll: true);
                        m.OnClick = (x, y) =>
                        {
                            Owner.LoadRoom(x, y);
                            m.Close(Owner.MapAnimation);
                        };
                        m.AllowEscape = true;
                        m.EnableSelect = true;
                        m.SwapMap = (r1, r2) =>
                        {
                            JObject room1 = RoomDatas[r1];
                            JObject room2 = RoomDatas[r2];
                            int? r1x = (int?)room1["X"];
                            int? r2x = (int?)room2["X"];
                            int? r1y = (int?)room1["Y"];
                            int? r2y = (int?)room2["Y"];
                            room1["X"] = r2x;
                            room1["Y"] = r2y;
                            room2["X"] = r1x;
                            room2["Y"] = r1y;
                            RoomDatas.Remove(r1);
                            RoomDatas.Remove(r2);
                            RoomDatas.Add(r1, room2);
                            RoomDatas.Add(r2, room1);
                        };
                        SaveRoom = false;
                    }
                    // Set Background (B)
                    else if (e.Key == Keys.B)
                    {
                        string[] choices = new string[Owner.Backgrounds.Count];
                        for (int i = 0; i < Owner.Backgrounds.Count; i++)
                        {
                            choices[i] = Owner.Backgrounds.Keys[i];
                        }
                        Owner.ShowDialog("Set background...", Owner.BGSprites.Name, choices, (r, st) =>
                        {
                            if (r)
                            {
                                JToken bg = Owner.GetBackground(st);
                                if (bg is object)
                                {
                                    CurrentRoom.BG = bg;
                                    Owner.BGSprites.Load(bg, Owner);
                                }
                            }
                        });
                    }
                    // Tool Info (Slash)
                    else if (e.Key == Keys.Slash)
                    {
                        if (descBack is null || !hudSprites.Contains(descBack))
                        {
                            if (descBack is null)
                                descBack = new RectangleSprite(20, 20, Game.RESOLUTION_WIDTH - 40, Game.RESOLUTION_HEIGHT - 40);
                            descBack.Color = Color.Black;
                            descBack.Layer = 60;
                            if (descText is null)
                            {
                                descText = new StringDrawable(25, 25, Owner.NonMonoFont, "", Color.White);
                                descText.MaxWidth = Game.RESOLUTION_WIDTH - 50;
                                descText.Layer = 61;
                            }
                            EditorTool et = currentTool;
                            descText.Text = "Press Space for a list of tools and their keybinds.\nPress < and > to select the previous/next tool.\n\n" + et.DefaultKey + " - " + et.DefaultName + "\n\n" + et.DefaultDescription;
                            descBack.SetHeight(descText.Height + 10);
                            descBack.CenterY = Game.RESOLUTION_HEIGHT / 2;
                            descText.CenterY = Game.RESOLUTION_HEIGHT / 2;
                            hudSprites.Add(descBack);
                            hudSprites.Add(descText);
                        }
                        else
                        {
                            hudSprites.Remove(descBack);
                            hudSprites.Remove(descText);
                        }
                    }
                    // Edit Room Group (Shift G)
                    else if (e.Shift && e.Key == Keys.G)
                    {
                        if (CurrentRoom is RoomGroup)
                        {
                            RoomGroup group = CurrentRoom as RoomGroup;
                            int x = (int)CameraX / Game.RESOLUTION_WIDTH;
                            int y = (int)CameraY / Game.RESOLUTION_HEIGHT;
                            Owner.LoadRoom(x, y);
                            group.Unload();
                        }
                        else
                        {
                            if (RoomGroups.ContainsKey(FocusedRoom))
                            {
                                if (SaveRoom)
                                    Owner.SaveCurrentRoom();
                                CurrentRoom?.Dispose();
                                CurrentRoom = RoomGroups[FocusedRoom].Load(Owner);
                                RoomLoc.Text = "Group " + CurrentRoom.X.ToString() + ", " + CurrentRoom.Y.ToString();
                                RoomLoc.Right = Game.RESOLUTION_WIDTH - 4;
                                if (showIndicators)
                                {
                                    showIndicators = false;
                                    ShowTileIndicators();
                                    showIndicators = true;
                                }
                            }
                            else
                            {
                                Owner.Shake(10);
                            }
                        }
                    }
                }
                else
                {
                    // HOLDING CONTROL
                    // Ctrl+T: Show/Hide Tile Indicators
                    if (e.Key == Keys.T)
                    {
                        showIndicators = !showIndicators;
                        ShowTileIndicators();
                    }
                    // Ctrl+E: Room Tags
                    else if (e.Key == Keys.E)
                    {
                        Owner.ShowDialog("Choose a tag to edit, or type a name for a new tag.", "", CurrentRoom.Tags.Keys.ToArray(), (r, st) =>
                        {
                            if (r)
                            {
                                string tag = st;
                                CurrentRoom.Tags.TryGetValue(tag, out float v);
                                Owner.ShowDialog("Type a number value for the tag \"" + tag + "\", or leave blank to delete the tag.", v.ToString(), new string[] { }, (r2, st2) =>
                                {
                                    if (r2)
                                    {
                                        if (st2 == "")
                                        {
                                            CurrentRoom.Tags.Remove(tag);
                                            VTextBox tb = new VTextBox(0, 0, Owner.FontTexture, "Deleted tag \"" + tag + "\".", Color.Gray);
                                            tb.frames = 60;
                                            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
                                            tb.CenterY = Game.RESOLUTION_HEIGHT / 2;
                                            hudSprites.Add(tb);
                                            tb.Appear();
                                        }
                                        else
                                        {
                                            if (!CurrentRoom.Tags.ContainsKey(tag))
                                                CurrentRoom.Tags.Add(tag, 0);
                                            if (float.TryParse(st2, out v))
                                            {
                                                CurrentRoom.Tags[tag] = v;
                                            }
                                        }
                                    }
                                });
                            }
                        });
                    }
                    // Ctrl+G: Group Rooms
                    else if (e.Key == Keys.G)
                    {
                        if (RoomGroups.ContainsKey(FocusedRoom))
                        {
                            int escapeItem;
                            MenuLayer.Builder menuItems = new MenuLayer.Builder(Owner);
                            menuItems.AddItem("Ungroup rooms", () =>
                            {
                                RoomGroup grp = RoomGroups[FocusedRoom];
                                for (int y = 0; y < grp.HeightRooms; y++)
                                {
                                    for (int x = 0; x < grp.WidthRooms; x++)
                                    {
                                        int id = x + CurrentRoom.X + (y + CurrentRoom.Y) * 100;
                                        if (RoomGroups.ContainsKey(id) && RoomGroups[id] == grp)
                                            RoomGroups.Remove(id);
                                    }
                                }
                                Owner.GroupList.Remove(grp);
                                Owner.ClearMenu();
                            });
                            escapeItem = menuItems.ItemCount;
                            menuItems.AddItem("Cancel", () =>
                            {
                                Owner.ClearMenu();
                            });
                            menuItems.Build();
                        }
                        else
                        {
                            Owner.ShowDialog("Create Room Group: Specify Size...", "1x1", new string[] { }, (r, st) =>
                            {
                                if (r)
                                {
                                    string[] size = st.Split(',', 'x');
                                    int w = -1;
                                    int h = -1;
                                    bool success = false;
                                    if (size.Length == 2)
                                    {
                                        success = int.TryParse(size[0].Trim(), out w) && int.TryParse(size[1].Trim(), out h);
                                    }
                                    else if (size.Length == 1)
                                    {
                                        success = int.TryParse(size[0].Trim(), out w);
                                        h = w;
                                    }
                                    if (success)
                                    {
                                        RoomGroup rg = new RoomGroup(CurrentRoom.EnterScript, CurrentRoom.ExitScript);
                                        for (int x = 0; x < w; x++)
                                        {
                                            for (int y = 0; y < h; y++)
                                            {
                                                int id = x + CurrentRoom.X + (y + CurrentRoom.Y) * 100;
                                                if (!RoomDatas.ContainsKey(id))
                                                {
                                                    Room newRoom = new Room(new SpriteCollection(), Script.Empty, Script.Empty);
                                                    newRoom.X = x;
                                                    newRoom.Y = y;
                                                    newRoom.TileTexture = CurrentRoom.TileTexture;
                                                    int ssn = x + y;
                                                    if (ssn < 0) ssn = 0;
                                                    if (Owner.RoomPresets.ContainsKey(CurrentRoom.GroupName))
                                                    {
                                                        AutoTileSettings.PresetGroup g = Owner.RoomPresets[CurrentRoom.GroupName];
                                                        AutoTileSettings.RoomPreset p = g.GetValueOrDefault(CurrentRoom.PresetName);
                                                        newRoom.UsePreset(p, g.Name);
                                                    }
                                                    RoomDatas.Add(id, newRoom.Save(Owner));
                                                }
                                                rg.RoomDatas.Add(id, RoomDatas[id]);
                                                if (!RoomGroups.ContainsKey(id))
                                                    RoomGroups.Add(id, rg);
                                            }
                                        }
                                        Owner.GroupList.Add(rg);
                                    }
                                }
                            });
                        }
                    }
                    // Ctrl+R: Jump to Room
                    else if (e.Key == Keys.R)
                    {
                        Owner.ShowDialog("Jump to room...", CurrentRoom.X.ToString() + ", " + CurrentRoom.Y, new string[] { }, (r, st) =>
                        {
                            if (r)
                            {
                                string[] xy = st.Split(',');
                                if (xy.Length == 2 && int.TryParse(xy[0].Trim(), out int x) && int.TryParse(xy[1].Trim(), out int y))
                                {
                                    Owner.LoadRoom(x, y);
                                }
                            }
                        });
                    }
                }
                if (tool == Tools.Enemy)
                {
                    if (e.Key == Keys.A)
                    {
                        PreviewScreen ps = AnimationPreviews(enemyTexture);
                        ps.OnClick = (s) =>
                        {
                            enemyAnimation = s.Animation.Name;
                        };
                        Owner.AddLayer(ps);
                    }
                    else if (e.Key == Keys.S)
                    {
                        List<string> texList = new List<string>();
                        foreach (Texture tex in Owner.Textures.Values)
                        {
                            texList.Add(tex.Name);
                        }
                        Owner.ShowDialog("Enemy texture?", enemyTexture.Name, texList.ToArray(), (r, st) =>
                        {
                            Texture t = Owner.TextureFromName(st);
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
                    if (e.Control && e.Key == Keys.V)
                    {
                        string[] tiles = new string[] { };
                        if (tiles.Length > 1)
                        {
                            float w = currentTexture.Width / currentTexture.TileSizeX;
                            float h = currentTexture.Height / currentTexture.TileSizeY;
                            float max = w * h;
                            int curX = (int)CurrentRoom.Right - 8;
                            int curY = (int)CurrentRoom.Bottom - 8;
                            Sprites.RemoveAll((s) => s is Tile);
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
                                            Sprites.AddForCollisions(newTile);
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
                }
                else if (tool == Tools.CustomSprite)
                {
                    if (e.Key == Keys.A)
                    {
                        List<string> choices = new List<string>();
                        foreach (Animation animation in customSpriteTexture.Animations.Values)
                        {
                            choices.Add(animation.Name);
                        }
                        Owner.ShowDialog("Sprite animation?", customSpriteAnimation, choices.ToArray(), (r, st) =>
                        {
                            if (r)
                            {
                                if (customSpriteTexture.AnimationFromName(st) is object)
                                {
                                    customSpriteAnimation = st;
                                }
                            }
                        });
                    }
                    else if (e.Key == Keys.S)
                    {
                        List<string> texList = new List<string>();
                        foreach (Texture tex in Owner.Textures.Values)
                        {
                            texList.Add(tex.Name);
                        }
                        Owner.ShowDialog("Sprite texture?", customSpriteTexture.Name, texList.ToArray(), (r, st) =>
                        {
                            Texture t = Owner.TextureFromName(st);
                            if (t is object)
                            {
                                customSpriteTexture = t;
                            }
                        });
                    }
                }
                else if (tool == Tools.Select)
                {
                    if (e.Key == Keys.A && e.Control)
                    {
                        ClearSelection();
                        List<Sprite> spr = new List<Sprite>(Sprites);
                        foreach (Sprite s in spr)
                        {
                            if (s is BoxSprite && !(s is ScriptBox)) continue;
                            if (!selectedSprites.Contains(s))
                            {
                                selectedSprites.Add(s);
                                BoxSprite b = new BoxSprite(s.X, s.Y, Owner.BoxTexture, 1, 1, Color.Cyan);
                                b.Layer = int.MaxValue;
                                b.SetSize((int)Math.Ceiling(s.Width / 8), (int)Math.Ceiling(s.Height / 8));
                                b.CenterX = s.CenterX;
                                b.CenterY = s.CenterY;
                                selectBoxes.Add(b);
                                Sprites.Add(b);

                            }
                            else
                            {
                                int i = selectedSprites.IndexOf(s);
                                BoxSprite b = selectBoxes[i];
                                Sprites.Remove(b);
                                selectBoxes.RemoveAt(i);
                                selectedSprites.RemoveAt(i);
                            }
                        }
                    }
                }
                else if (tool == Tools.Lever)
                {
                    if (e.Key == Keys.A)
                    {
                        PreviewScreen ps = AnimationPreviews(leverTexture);
                        ps.OnClick = (s) =>
                        {
                            string anim = s.Animation.Name;
                            if (anim.ToLower().EndsWith("on"))
                                anim = anim.Substring(0, anim.Length - 2);
                            else if (anim.ToLower().EndsWith("off"))
                                anim = anim.Substring(0, anim.Length - 3);
                            leverAnimation = anim;
                        };
                        Owner.AddLayer(ps);
                    }
                    else if (e.Key == Keys.S)
                    {
                        List<string> texList = new List<string>();
                        foreach (Texture tex in Owner.Textures.Values)
                        {
                            texList.Add(tex.Name);
                        }
                        Owner.ShowDialog("Lever texture?", leverTexture.Name, texList.ToArray(), (r, st) =>
                        {
                            Texture t = Owner.TextureFromName(st);
                            if (t is object && t.Animations is object && t.Animations.Count > 0)
                            {
                                leverTexture = t;
                                leverAnimation = "Lever";
                            }
                        });
                    }
                }
                else if (tool == Tools.Crewman)
                {
                    if (e.Key == Keys.S)
                    {
                        List<string> crewmen = new List<string>();
                        foreach (Texture texture in Owner.Textures.Values)
                        {
                            if (texture is CrewmanTexture && texture.AnimationFromName("Standing") != null && texture.AnimationFromName("Walking") != null && texture.AnimationFromName("Dying") != null)
                            {
                                crewmen.Add(texture.Name);
                            }
                        }
                        Owner.ShowDialog("Which crewmate do you wish to place?", "", crewmen.ToArray(), (r, st) =>
                        {
                            if (r)
                            {
                                CrewmanTexture t = Owner.TextureFromName(st) as CrewmanTexture;
                                if (t is object && t.Animations.ContainsKey("Standing"))
                                {
                                    crewmanTexture = t;
                                }
                            }
                        });
                    }
                }

                currentTool.HandleKey(e);
            }
            else if (CurrentEditingFocus == FocusOptions.Tileset)
            {
                TilesTool tt = currentTool as TilesTool;
                if (tt is null)
                    HideTileset();
                else
                {
                    if (e.Key == Keys.Tab || e.Key == Keys.Escape)
                    {
                        HideTileset();
                    }
                    else if (e.Key == Keys.D1 && tt.IsAuto)
                    {
                        selection.SetSize(3, 5);
                    }
                    else if (e.Key == Keys.D2 && tt.IsAuto)
                    {
                        selection.SetSize(3, 1);
                    }
                    else if (e.Key == Keys.D3 && tt.IsAuto)
                    {
                        selection.SetSize(8, 6);
                    }
                    else if (e.Key == Keys.D4 && tt.IsAuto)
                    {
                        selection.SetSize(4, 1);
                    }
                    else if (e.Key == Keys.Right)
                    {
                        float tx = tileset.X;
                        tileset.X -= 32;
                        if (tileset.Right < Game.RESOLUTION_WIDTH)
                            tileset.Right = Game.RESOLUTION_WIDTH;
                        tx -= tileset.X;
                        tileScroll.X += (int)tx;
                        tileSelection.X -= tx;
                    }
                    else if (e.Key == Keys.Left)
                    {
                        float tx = tileset.X;
                        tileset.X += 32;
                        if (tileset.X > 0)
                            tileset.X = 0;
                        tx -= tileset.X;
                        tileScroll.X += (int)tx;
                        tileSelection.X -= tx;
                    }
                    else if (e.Key == Keys.Down)
                    {
                        float ty = tileset.Y;
                        tileset.Y -= 32;
                        if (tileset.Bottom < Game.RESOLUTION_HEIGHT)
                            tileset.Bottom = Game.RESOLUTION_HEIGHT;
                        ty -= tileset.Y;
                        tileScroll.Y += (int)ty;
                        tileSelection.Y -= ty;
                    }
                    else if (e.Key == Keys.Up)
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
            }
        }

        private void UpdateBoundsBoxes()
        {
            BoundsSprites.Clear();
            for (int i = 0; i < Sprites.Count; i++)
            {
                if (Sprites[i] is IBoundSprite)
                {
                    Color c = Sprites[i].KillCrewmen ? Color.Red : Color.Lime;
                    if (Sprites[i].Within(MouseX, MouseY, 1, 1))
                        c = Color.Cyan;
                    Rectangle s = (Sprites[i] as IBoundSprite).Bounds;
                    if (s.Width == 0 || s.Height == 0) continue;
                    s.X += (int)Sprites[i].InitialX;
                    s.Y += (int)Sprites[i].InitialY;
                    RectangleSprite rs = new RectangleSprite(s.X, s.Y, s.Width, 1);
                    rs.Color = c;
                    BoundsSprites.Add(rs);
                    rs = new RectangleSprite(s.X, s.Y, 1, s.Height);
                    rs.Color = c;
                    BoundsSprites.Add(rs);
                    rs = new RectangleSprite(s.X, s.Y + s.Height - 1, s.Width, 1);
                    rs.Color = c;
                    BoundsSprites.Add(rs);
                    rs = new RectangleSprite(s.X + s.Width - 1, s.Y, 1, s.Height);
                    rs.Color = c;
                    BoundsSprites.Add(rs);
                }
            }
        }
        public void Notify(string message, float x, float y, Color color, int time)
        {
            VTextBox tb = new VTextBox(x, y - 26, Owner.FontTexture, message, color);
            if (tb.Y < 0)
                tb.Y = 0;
            else if (tb.Bottom > Game.RESOLUTION_HEIGHT)
                tb.Bottom = Game.RESOLUTION_HEIGHT;
            if (tb.X < 0)
                tb.X = 0;
            else if (tb.Right > Game.RESOLUTION_WIDTH)
                tb.Right = Game.RESOLUTION_WIDTH;
            Owner.hudSprites.Add(tb);
            tb.Disappeared += (b) =>
            {
                Owner.hudSprites.Remove(b);
            };
            tb.frames = time;
            tb.Appear();
        }

        public void ShowTileset(TilesTool tool)
        {
            CurrentEditingFocus = FocusOptions.Tileset;
            if (tileset.Texture != tool.Texture)
            {
                tileset.ChangeTexture(tool.Texture);
                tileset.X = 0;
                tileset.Y = 0;
                tileScroll = new Point(0, 0);
                tileset.Size = 1f / (tool.Texture.TileSizeX / 8);
            }
            tileset.Layer = -1;
            if (tool.IsAuto)
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
            else
            {
                tileSelection.SetSize(1, 1);
                tileSelection.X = tool.Tile.X * tool.Texture.TileSizeX - tileScroll.X;
                tileSelection.Y = tool.Tile.Y * tool.Texture.TileSizeY - tileScroll.Y;
            }
            hudSprites.Add(tileSelection);
            hudSprites.Add(tileset);
        }

        public void HideTileset()
        {
            CurrentEditingFocus = FocusOptions.Level;
            hudSprites.Remove(tileset);
            hudSprites.Remove(tileSelection);
        }

        private void backupSelectTool(KeyboardKeyEventArgs e)
        {

            if (e.Key == Keys.D1)
            {
                tool = Tools.Ground;
                editorTool.Text = "1 - Ground";
                prefix = 'g';
                ClearSelection();
            } // Ground
            else if (e.Key == Keys.D2)
            {
                tool = Tools.Background;
                editorTool.Text = "2 - Background";
                prefix = 'b';
                ClearSelection();
            } // Background
            else if (e.Key == Keys.D3)
            {
                tool = Tools.Spikes;
                editorTool.Text = "3 - Spikes";
                prefix = 's';
                ClearSelection();
            } // Spikes
            else if (e.Key == Keys.D4)
            {
                tool = Tools.Trinket;
                editorTool.Text = "4 - Trinket";
                ClearSelection();
            } // Trinkets
            else if (e.Key == Keys.D5)
            {
                tool = Tools.Checkpoint;
                editorTool.Text = "5 - Checkpoint";
                ClearSelection();
            } // Checkpoints
            else if (e.Key == Keys.D6)
            {
                tool = Tools.Disappear;
                editorTool.Text = "6 - Disappear";
                ClearSelection();
            } // Disappear
            else if (e.Key == Keys.D7)
            {
                tool = Tools.Conveyor;
                editorTool.Text = "7 - Conveyor";
                ClearSelection();
            } // Moving Platforms
            else if (e.Key == Keys.D8)
            {
                tool = Tools.Platform;
                editorTool.Text = "8 - Platform";
                ClearSelection();
            } // Conveyors
            else if (e.Key == Keys.D9)
            {
                tool = Tools.Enemy;
                editorTool.Text = "9 - Enemy";
                ClearSelection();
            } // Enemies
            else if (e.Key == Keys.D0)
            {
                tool = Tools.GravityLine;
                editorTool.Text = "0 - Grav Line";
                ClearSelection();
            } // Grav Lines
            else if (e.Key == Keys.P)
            {
                tool = Tools.Start;
                editorTool.Text = "P - Start";
                ClearSelection();
            } // Start Point
            else if (e.Key == Keys.O)
            {
                tool = Tools.Crewman;
                editorTool.Text = "O - Crewmate";
                ClearSelection();
            } // Crewmates
            else if (e.Key == Keys.I)
            {
                tool = Tools.WarpLine;
                editorTool.Text = "I - Warp Line";
                ClearSelection();
            } // Warp Lines
            else if (e.Key == Keys.U)
            {
                tool = Tools.WarpToken;
                editorTool.Text = "U - Warp Token";
                ClearSelection();
            } // Warp Tokens
            else if (e.Key == Keys.Y)
            {
                tool = Tools.ScriptBox;
                editorTool.Text = "Y - Script Box";
                ClearSelection();
            } // Script Boxes
            else if (e.Key == Keys.T)
            {
                tool = Tools.Terminal;
                editorTool.Text = "T - Terminal";
                ClearSelection();
            } // Terminals
            else if (e.Key == Keys.R)
            {
                tool = Tools.RoomText;
                editorTool.Text = "R - Roomtext";
                ClearSelection();
            } // Room Text
            else if (e.Key == Keys.Semicolon)
            {
                tool = Tools.Lever;
                editorTool.Text = "; - Lever";
                ClearSelection();
            } // Lever
            else if (e.Key == Keys.Minus)
            {
                tool = Tools.Tiles;
                tileSelection.X = currentTile.X * 8 - tileScroll.X;
                tileSelection.Y = currentTile.Y * 8 - tileScroll.Y;
                tileSelection.SetSize(1, 1);
                editorTool.Text = "- - Tiles";
                ClearSelection();
            } // Tiles
            else if (e.Key == Keys.Equal)
            {
                tool = Tools.Select;
                editorTool.Text = "= - Select";
                ClearSelection();
            } // Select
            else if (e.Key == Keys.GraveAccent)
            {
                tool = Tools.CustomSprite;
                editorTool.Text = "` - Custom Sprite";
                ClearSelection();
            } // Custom Sprite
        }

        public PreviewScreen AnimationPreviews(Texture texture)
        {
            PreviewScreen ps = new PreviewScreen(new Sprite[] { }, null, Owner);
            int x = 20;
            int y = 20;
            float maxHeight = 0;
            foreach (Animation animation in texture.Animations.Values)
            {
                Sprite s = new Sprite(x, y, texture, animation);
                s.ColorModifier = AnimatedColor.Default;
                if (s.Height > maxHeight) maxHeight = s.Height;
                if (s.Right > Game.RESOLUTION_WIDTH - 20)
                {
                    y += (int)maxHeight + 18;
                    x = 20;
                    s.X = x;
                    s.Y = y;
                }
                s.Name = animation.Name;
                ps.Sprites.Add(s);
                StringDrawable animName = new StringDrawable(s.X, s.Bottom + 2, Owner.NonMonoFont, animation.Name);
                if (animName.Right > Game.RESOLUTION_WIDTH - 5)
                {
                    y += (int)maxHeight + 18;
                    x = 20;
                    s.X = x;
                    s.Y = y;
                    animName.X = x;
                    animName.Y = s.Bottom + 2;
                }
                x += (int)Math.Max(s.Width, animName.Width) + 8;
                ps.Sprites.Add(animName);
            }
            ps.MaxScroll = (int)Math.Max(0, y + maxHeight + 24 - Game.RESOLUTION_HEIGHT);
            return ps;
        }

        public override void HandleWheel(int e)
        {
            
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            hudSprites.Render(Owner.FrameCount);
            if (currentTool.Sprites is object)
                currentTool.Sprites.Render(Owner.FrameCount);
            if (ShowBoundsBoxes)
                BoundsSprites.Render(Owner.FrameCount);
        }
        public override void DrawExtraHud(Matrix4 baseCamera, int viewMatrixLocation)
        {
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            ExtraHud.Render(Owner.FrameCount);
        }

        public override void Process()
        {
            if (replaceTiles)
                ReplaceTiles();
            Sprites.SortForCollisions();
            if (ShowBoundsBoxes)
                UpdateBoundsBoxes();
            selection.Visible = true;
            { 
            //selection.Color = Color.Blue;
            //if (tool == Tools.Background || tool == Tools.Ground || tool == Tools.Tiles || tool == Tools.Spikes)
            //{
            //    if (isFill)
            //        selection.Color = Color.Magenta;
            //    if (tool != Tools.Spikes)
            //    {
            //        if (Owner.IsKeyHeld(Keys.Z))
            //        {
            //            tileToolW = 3;
            //            tileToolH = 3;
            //        }
            //        else if (Owner.IsKeyHeld(Keys.X))
            //        {
            //            tileToolW = 5;
            //            tileToolH = 5;
            //        }
            //        else
            //        {
            //            tileToolW = tileToolDefW;
            //            tileToolH = tileToolDefH;
            //        }
            //    }
            //    else
            //    {
            //        tileToolW = tileToolH = 1;
            //    }
            //}
            //else if (tool == Tools.Checkpoint || tool == Tools.Start || tool == Tools.Terminal || tool == Tools.CustomSprite || tool == Tools.Crewman || tool == Tools.Lever)
            {
                if (Owner.IsKeyHeld(Keys.Z))
                {
                    flipToolY = true;
                }
                else
                {
                    flipToolY = false;
                }
                if (Owner.IsKeyHeld(Keys.X))
                {
                    flipToolX = true;
                }
                else
                {
                    flipToolX = false;
                }
            }
            } // Backup
            { 
            //if (CurrentEditingFocus == FocusOptions.Level && !currentlyBinding)
            //    switch (tool)
            //    {
            //        case Tools.Ground:
            //        case Tools.Background:
            //        case Tools.Spikes:
            //        case Tools.Tiles:
            //            {
            //                selection.SetSize(tileToolW, tileToolH);
            //                int x = tool == Tools.Tiles ? currentTile.X : autoTiles.Origin.X;
            //                int y = tool == Tools.Tiles ? currentTile.Y : autoTiles.Origin.Y;
            //                if (previewTile.Texture != currentTexture)
            //                {
            //                    previewTile.ChangeTexture(currentTexture);
            //                }
            //                if (previewTile.TextureX != x || previewTile.TextureY != y)
            //                {
            //                    previewTile.Animation = Animation.Static(x, y, currentTexture);
            //                    previewTile.ResetAnimation();
            //                }
            //                string s = "  " + (tool == Tools.Tiles ? "Tile" : "Auto Tiles") + " {" + x.ToString() + ", " + y.ToString() + "}";
            //                if (tileLayer != -2)
            //                {
            //                    s += " (Layer = " + tileLayer.ToString() + ")";
            //                }
            //                toolPrompt.Text = s;
            //                previewTile.Visible = true;
            //            }
            //            break;
            //        case Tools.Checkpoint:
            //            selection.SetSize(2, 2);
            //            toolPrompt.Text = "";
            //            break;
            //        case Tools.Trinket:
            //            selection.SetSize(2, 2);
            //            toolPrompt.Text = "Total Trinkets: " + Owner.LevelTrinkets.Count.ToString();
            //            break;
            //        case Tools.Enemy:
            //            {
            //                Rectangle r = new Rectangle(0, 0, 8, 8);
            //                Animation a = enemyTexture.AnimationFromName(enemyAnimation);
            //                if (a is object) r = a.Hitbox;
            //                selection.SetSize((int)Math.Ceiling(r.Width / 8f), (int)Math.Ceiling(r.Height / 8f));
            //                if (GiveDirection == null)
            //                    toolPrompt.Text = enemyAnimation;
            //                else
            //                    toolPrompt.Text = "Press arrow key for enemy direction";
            //                if (!hudSprites.Contains(toolPrompt))
            //                    hudSprites.Add(toolPrompt);
            //            }
            //            break;
            //        case Tools.Disappear:
            //            {
            //                if (shift)
            //                    selection.SetSize(1, 1);
            //                else
            //                {
            //                    Animation a = platformTexture.AnimationFromName(disappearAnimation);
            //                    if (a is object)
            //                        selection.SetSize((int)Math.Ceiling(a.Hitbox.Width / 2d), (int)Math.Ceiling(a.Hitbox.Height / 8d));
            //                }
            //                toolPrompt.Text = disappearAnimation;
            //            }
            //            break;
            //        case Tools.Platform:
            //            {
            //                if (shift)
            //                    selection.SetSize(1, 1);
            //                else
            //                {
            //                    Animation a = platformTexture.AnimationFromName(platformAnimation);
            //                    if (a is object)
            //                        selection.SetSize((int)Math.Ceiling(a.Hitbox.Width / 2d), (int)Math.Ceiling(a.Hitbox.Height / 8d));
            //                }
            //                if (!toolPromptImportant)
            //                    toolPrompt.Text = platformAnimation;
            //            }
            //            break;
            //        case Tools.Lever:
            //            {
            //                Rectangle r = new Rectangle(0, 0, 8, 8);
            //                Animation a = leverTexture.AnimationFromName(leverAnimation + "Off");
            //                if (a is object) r = a.Hitbox;
            //                selection.SetSize((int)Math.Ceiling(r.Width / 8f), (int)Math.Ceiling(r.Height / 8f));
            //                if (!toolPromptImportant)
            //                    toolPrompt.Text = leverAnimation;
            //                if (!hudSprites.Contains(toolPrompt))
            //                    hudSprites.Add(toolPrompt);
            //            }
            //            break;
            //        case Tools.Conveyor:
            //            {
            //                if (shift)
            //                    selection.SetSize(1, 1);
            //                else
            //                {
            //                    Animation a = platformTexture.AnimationFromName(conveyorAnimation);
            //                    if (a is object)
            //                        selection.SetSize((int)Math.Ceiling(a.Hitbox.Width / 2d), (int)Math.Ceiling(a.Hitbox.Height / 8d));
            //                }
            //                if (!toolPromptImportant)
            //                    toolPrompt.Text = conveyorAnimation;
            //            }
            //            break;
            //        case Tools.Terminal:
            //            {
            //                Animation terminal = terminalTexture.AnimationFromName(terminalOff);
            //                if (terminal is object)
            //                    selection.SetSize((int)Math.Ceiling(terminal.Hitbox.Width / 8d), (int)Math.Ceiling(terminal.Hitbox.Height / 8d));
            //                if (!toolPromptImportant)
            //                    toolPrompt.Text = terminalOff + "/" + terminalOn;
            //            }
            //            break;
            //        case Tools.WarpToken:
            //            {
            //                Animation warp = warpTokenTexture.AnimationFromName(warpTokenAnimation);
            //                if (warp is object)
            //                    selection.SetSize((int)Math.Ceiling(warp.Hitbox.Width / 8d), (int)Math.Ceiling(warp.Hitbox.Height / 8d));
            //                if (!toolPromptImportant)
            //                    toolPrompt.Text = "";
            //            }
            //            break;
            //        case Tools.ScriptBox:
            //            selection.SetSize(1, 1);
            //            if (!toolPromptImportant)
            //                toolPrompt.Text = "";
            //            break;
            //        case Tools.GravityLine:
            //        case Tools.WarpLine:
            //            selection.SetSize(1, 1);
            //            if (!toolPromptImportant)
            //                toolPrompt.Text = "";
            //            break;
            //        case Tools.Start:
            //            selection.SetSize((int)Math.Ceiling(Player.Width / 8), (int)Math.Ceiling(Player.Height / 8));
            //            if (!toolPromptImportant)
            //                toolPrompt.Text = "";
            //            break;
            //        case Tools.Crewman:
            //            selection.SetSize(2, 3);
            //            if (!toolPromptImportant)
            //                toolPrompt.Text = crewmanTexture?.Name ?? "Press S to choose crewmate";
            //            break;
            //        case Tools.RoomText:
            //            selection.SetSize(1, 1);
            //            if (!toolPromptImportant)
            //                toolPrompt.Text = "";
            //            break;
            //        case Tools.CustomSprite:
            //            {
            //                Animation anim = customSpriteTexture.AnimationFromName(customSpriteAnimation);
            //                if (anim is null)
            //                    selection.SetSize(1, 1);
            //                else
            //                    selection.SetSize((int)Math.Ceiling((float)anim.Hitbox.Width / 8), (int)Math.Ceiling((float)anim.Hitbox.Height / 8));
            //                toolPrompt.Text = customSpriteAnimation + " (" + customSpriteTexture.Name + ")";
            //            }
            //            break;
            //        case Tools.Select:
            //            selection.SetSize(1, 1);
            //            if (!toolPromptImportant)
            //                toolPrompt.Text = "";
            //            break;
            //        default:
            //            selection.SetSize(1, 1);
            //            break;
            //    }
            } // Backup
            if (Owner.MouseIn)
            {
                selection.Visible = true;
                if (!Owner.IsKeyHeld(Keys.RightBracket))
                    selection.X = (int)Math.Floor((MouseX - (8 * (selection.WidthTiles / 2f))) / 8 + 0.5) * 8;
                if (!Owner.IsKeyHeld(Keys.LeftBracket))
                    selection.Y = (int)Math.Floor((MouseY - (8 * (selection.HeightTiles / 2f))) / 8 + 0.5) * 8;
            }
            else
            {
                selection.Visible = false;
                selection.Right = 0;
                selection.Bottom = 0;
            }
            if (CurrentEditingFocus == FocusOptions.Level)
            {
                Sprites.SortForCollisions();
                currentTool.Process();
                Rectangle r = new Rectangle(currentTool.Position, currentTool.Size);
                selection.X = r.X;
                selection.Y = r.Y;
                selection.SetSize(r.Width, r.Height);
                selection.Color = currentTool.Color;
            }
            else if (CurrentEditingFocus == FocusOptions.Tileset)
            {
                TilesTool tt = currentTool as TilesTool;
                if (tt is null)
                    HideTileset();
                else
                {
                    if (!tt.IsAuto)
                    {
                        if (LeftMouse)
                        {
                            tt.Tile = new Point((int)(selection.X + tileScroll.X) / 8, (int)(selection.Y + tileScroll.Y) / 8);
                            tileSelection.X = selection.X;
                            tileSelection.Y = selection.Y;
                            tileSelection.SetSize(1, 1);
                        }
                    }
                    else
                    {
                        if (LeftMouse)
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
            }
            HandleHUD();
        }

        void backupProcessTool()
        {
            {
                if (currentlyBinding)
                {
                    selection.SetSize(1, 1);
                    if (selecting)
                    {
                        int w = (int)Math.Floor((MouseX - selectOrigin.X) / 8);
                        int h = (int)Math.Floor((MouseY - selectOrigin.Y) / 8);
                        if (w >= 0) w += 1;
                        else w -= 1;
                        if (h >= 0) h += 1;
                        else h -= 1;
                        selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                        selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                        selection.SetSize(Math.Abs(w), Math.Abs(h));
                        if (!LeftMouse)
                        {
                            bindSprite?.Invoke(new Rectangle((int)selection.X + (int)CameraX, (int)selection.Y + (int)CameraY, (int)selection.Width, (int)selection.Height));
                            selecting = false;
                            toolPromptImportant = false;
                        }
                    }
                    else if (LeftMouse && !selecting)
                    {
                        selecting = true;
                        selectOrigin = new PointF(selection.X, selection.Y);
                    }
                }
                else
                {
                    if ((Owner.IsKeyHeld(Keys.LeftControl) && !Owner.IsKeyHeld(Keys.LeftShift) && LeftMouse && tool != Tools.Attach && tool != Tools.Point) || tool == Tools.Select)
                    {
                        if (selecting)
                        {
                            int w = (int)Math.Floor((MouseX - selectOrigin.X) / 8);
                            int h = (int)Math.Floor((MouseY - selectOrigin.Y) / 8);
                            if (w >= 0) w += 1;
                            else w -= 1;
                            if (h >= 0) h += 1;
                            else h -= 1;
                            selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                            selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                            selection.SetSize(Math.Abs(w), Math.Abs(h));
                            if (!LeftMouse)
                            {
                                selecting = false;
                                List<Sprite> col = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY, selection.Width, selection.Height);
                                foreach (Sprite s in col)
                                {
                                    if (s is BoxSprite && !(s is ScriptBox)) continue;
                                    if (!Owner.IsKeyHeld(Keys.LeftControl) && (s is Tile || s is ScriptBox)) continue;
                                    if (!selectedSprites.Contains(s))
                                    {
                                        selectedSprites.Add(s);
                                        BoxSprite b = new BoxSprite(s.X, s.Y, Owner.BoxTexture, 1, 1, Color.Cyan);
                                        b.Layer = int.MaxValue;
                                        b.SetSize((int)Math.Ceiling(s.Width / 8), (int)Math.Ceiling(s.Height / 8));
                                        b.CenterX = s.CenterX;
                                        b.CenterY = s.CenterY;
                                        selectBoxes.Add(b);
                                        Sprites.Add(b);

                                    }
                                    else if (selectedSprites.Contains(s))
                                    {
                                        int i = selectedSprites.IndexOf(s);
                                        BoxSprite b = selectBoxes[i];
                                        Sprites.Remove(b);
                                        selectBoxes.RemoveAt(i);
                                        selectedSprites.RemoveAt(i);
                                    }
                                }
                            }
                        }
                        else if (dragging)
                        {
                            float x = (int)(MouseX - selectOrigin.X);
                            float y = (int)(MouseY - selectOrigin.Y);
                            if (!Owner.IsKeyHeld(Keys.Menu))
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
                            if (!LeftMouse)
                            {
                                dragging = false;
                            }
                        }
                        else
                        {
                            if (LeftMouse)
                            {
                                bool drag = false;
                                if (!Owner.IsKeyHeld(Keys.LeftShift))
                                {
                                    List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 1, 1);
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
                                    selectOrigin = new PointF(MouseX, MouseY);
                                }
                                else
                                {
                                    tool = Tools.Select;
                                    editorTool.Text = "= - Select";
                                    if (!Owner.IsKeyHeld(Keys.LeftShift))
                                        ClearSelection();
                                    selecting = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                            }
                            else if (MiddleMouse)
                            {
                                bool isSelected = false;
                                List<Sprite> colliders = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
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
                                    BoxSprite b = new BoxSprite(s.X, s.Y, Owner.BoxTexture, 1, 1, Color.Cyan);
                                    b.Layer = int.MaxValue;
                                    b.SetSize((int)Math.Ceiling(s.Width / 8), (int)Math.Ceiling(s.Height / 8));
                                    b.CenterX = s.CenterX;
                                    b.CenterY = s.CenterY;
                                    selectBoxes.Add(b);
                                    Sprites.Add(b);
                                }
                                SetProperty();
                            }
                            else if (RightMouse && !stillHolding)
                            {
                                stillHolding = true;
                                bool isSelected = false;
                                List<Sprite> colliders = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
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
                                        BoxSprite b = new BoxSprite(s.X, s.Y, Owner.BoxTexture, 1, 1, Color.Cyan);
                                        b.Layer = int.MaxValue;
                                        b.SetSize((int)Math.Ceiling(s.Width / 8), (int)Math.Ceiling(s.Height / 8));
                                        b.CenterX = s.CenterX;
                                        b.CenterY = s.CenterY;
                                        selectBoxes.Add(b);
                                        Sprites.Add(b);
                                    }
                                }
                                if (selectedSprites.Count > 0)
                                {
                                    List<VMenuItem> contextMenuItems = new List<VMenuItem>();
                                    List<Type> typesMulti = new List<Type> { typeof(IBoundSprite), typeof(IPlatform), typeof(IScriptExecutor), typeof(Platform) };
                                    List<Type> typesSingle = new List<Type> { typeof(StringDrawable), typeof(WarpToken), typeof(Crewman) };
                                    for (int i = 0; i < typesMulti.Count; i++)
                                    {
                                        if (selectedSprites.Any((sp) => !typesMulti[i].IsAssignableFrom(sp.GetType())))
                                        {
                                            typesMulti.RemoveAt(i--);
                                        }
                                    }
                                    if (selectedSprites.Count == 1)
                                    {
                                        for (int i = 0; i < typesSingle.Count; i++)
                                        {
                                            if (!typesSingle[i].IsAssignableFrom(selectedSprites.Single().GetType()))
                                                typesSingle.RemoveAt(i--);
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
                                            float speedX = (selectedSprites[0] as IBoundSprite).XVelocity;
                                            float speedY = (selectedSprites[0] as IBoundSprite).YVelocity;
                                            float speedV = (float)Math.Sqrt((selectedSprites[0] as IBoundSprite).XVelocity * (selectedSprites[0] as IBoundSprite).XVelocity + (selectedSprites[0] as IBoundSprite).YVelocity * (selectedSprites[0] as IBoundSprite).YVelocity);
                                            bool canXY = true;
                                            bool canVel = true;
                                            for (int i = 1; i < selectedSprites.Count; i++)
                                            {
                                                IBoundSprite sprite = selectedSprites[i] as IBoundSprite;
                                                if (sprite.XVelocity != speedX || sprite.YVelocity != speedY) canXY = false;
                                                if (Math.Sqrt(sprite.XVelocity * sprite.XVelocity + sprite.YVelocity * sprite.YVelocity) != speedV)
                                                {
                                                    canXY = false;
                                                    canVel = false;
                                                    break;
                                                }
                                            }
                                            string da = canXY ? speedX.ToString() + ", " + speedY.ToString() : (canVel ? speedV.ToString() : "");
                                            Owner.ShowDialog("Set speed (format: x, y) - Negative values for up/left.\nYou can also type only one value to keep the direction the same.", da, null, (r, st) =>
                                            {
                                                string[] a = st.Split(new char[] { ',', 'x', 'X' });
                                                if (a.Length == 2)
                                                {
                                                    if (float.TryParse(a[0], out float xs) && float.TryParse(a[1], out float ys))
                                                    {
                                                        foreach (IBoundSprite sprite in selectedSprites)
                                                        {
                                                            sprite.XVelocity = xs;
                                                            sprite.YVelocity = ys;
                                                        }
                                                    }
                                                }
                                                else if (a.Length == 1)
                                                {
                                                    if (float.TryParse(a[0], out float vs))
                                                    {
                                                        foreach (IBoundSprite sprite in selectedSprites)
                                                        {
                                                            double direction = Math.Atan2(sprite.YVelocity, sprite.XVelocity);
                                                            sprite.XVelocity = (float)Math.Round((vs * Math.Cos(direction)), 5);
                                                            sprite.YVelocity = (float)Math.Round((vs * Math.Sin(direction)), 5);
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
                                    if (typesSingle.Contains(typeof(Crewman)) && selectedSprites.Count == 1)
                                    {
                                        bool hasName = selectedSprites[0].Name is object;
                                        contextMenuItems.Add(new VMenuItem(hasName ? "Change Name" : "Assign Name", () =>
                                        {
                                            Crewman c = selectedSprites[0] as Crewman;
                                            Owner.ShowDialog("Name of crewman (texture = " + c.Texture.Name + ")", c.Name ?? c.Texture.Name, new string[] { }, (r, st) =>
                                            {
                                                if (r)
                                                {
                                                    if (!Owner.UserAccessSprites.ContainsKey(st))
                                                    {
                                                        if (Owner.UserAccessSprites.ContainsKey(c.Name))
                                                        {
                                                            Owner.UserAccessSprites.Remove(c.Name);
                                                        }
                                                        c.Name = st;
                                                        Owner.UserAccessSprites.Add(c.Name, c);
                                                    }
                                                }
                                            });
                                        }));
                                    }
                                    if (typesMulti.Contains(typeof(Platform)))
                                    {
                                        contextMenuItems.Add(new VMenuItem("Set One-Way...", () =>
                                        {
                                            string response = "None";
                                            switch ((selectedSprites[0] as Platform).State)
                                            {
                                                case Tile.TileStates.OneWayU:
                                                    response = "Up";
                                                    break;
                                                case Tile.TileStates.OneWayD:
                                                    response = "Down";
                                                    break;
                                                case Tile.TileStates.OneWayL:
                                                    response = "Left";
                                                    break;
                                                case Tile.TileStates.OneWayR:
                                                    response = "Right";
                                                    break;
                                            }
                                            Owner.ShowDialog("Platform one-way state?", response, new string[] { "None", "Up", "Down", "Left", "Right" }, (r, st) =>
                                            {
                                                if (r)
                                                {
                                                    response = st;
                                                    Tile.TileStates state = Tile.TileStates.Normal;
                                                    switch (response)
                                                    {
                                                        case "Up":
                                                            state = Tile.TileStates.OneWayU;
                                                            break;
                                                        case "Down":
                                                            state = Tile.TileStates.OneWayD;
                                                            break;
                                                        case "Left":
                                                            state = Tile.TileStates.OneWayL;
                                                            break;
                                                        case "Right":
                                                            state = Tile.TileStates.OneWayR;
                                                            break;
                                                    }
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        if (sprite is Platform)
                                                            (sprite as Platform).State = state;
                                                    }
                                                }
                                            });
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
                                        Owner.ShowDialog("Change Layer", ans, new string[] { }, (r, st) =>
                                        {
                                            if (r)
                                            {
                                                if (int.TryParse(st, out int l))
                                                {
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        Sprites.RemoveFromCollisions(sprite);
                                                        sprite.Layer = l;
                                                        Sprites.AddForCollisions(sprite);
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
                                        Owner.ShowDialog("Change Size", ans, new string[] { }, (r, st) =>
                                        {
                                            if (r)
                                            {
                                                if (float.TryParse(st, out float l))
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
                                    Owner.OpenContextMenu(MouseX, MouseY, contextMenuItems);
                                }
                            }
                            else if (!RightMouse && stillHolding)
                            {
                                stillHolding = false;
                            }
                        }
                    }
                    else
                    {
                        if (tool == Tools.Tiles)
                        {
                            if (LeftMouse || RightMouse)
                            {
                                bool lm = LeftMouse;
                                if (!isFill)
                                {
                                    for (int tileX = 0; tileX < tileToolW; tileX++)
                                    {
                                        for (int tileY = 0; tileY < tileToolH; tileY++)
                                        {
                                            Owner.TileTool(selection.X + CameraX + tileX * 8, selection.Y + CameraY + tileY * 8, lm, currentTexture, currentTile, tileLayer);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!selecting)
                                    {
                                        selecting = true;
                                        Owner.TileFillTool(selection.X + CameraX, selection.Y + CameraY, lm, Tools.Tiles, autoTiles, currentTile, tileLayer, currentTexture, prefix, lr: !Owner.IsKeyHeld(Keys.RightBracket), ud: !Owner.IsKeyHeld(Keys.LeftBracket));
                                    }
                                }
                            }
                            else
                            {
                                selecting = false;
                                if (MiddleMouse)
                                {
                                    Tile t = Owner.GetTile((int)(selection.X + CameraX), (int)(selection.Y + CameraY), tileLayer);
                                    if (t != null)
                                    {
                                        currentTile = new Point(t.TextureX, t.TextureY);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Ground || tool == Tools.Background || tool == Tools.Spikes && autoTiles != null)
                        {
                            if (LeftMouse || RightMouse || dragging)
                            {
                                bool lm = LeftMouse;
                                if (!isFill)
                                {
                                    if (Owner.IsKeyHeld(Keys.LeftShift) && !dragging)
                                    {
                                        dragging = true;
                                        selectOrigin = new PointF(selection.X, selection.Y);
                                    }
                                    else if (dragging)
                                    {
                                        int w = (int)Math.Floor((MouseX - selectOrigin.X) / 8);
                                        int h = (int)Math.Floor((MouseY - selectOrigin.Y) / 8);
                                        if (w >= 0) w += 1;
                                        else w -= 1;
                                        if (h >= 0) h += 1;
                                        else h -= 1;
                                        selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                        selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                                        selection.SetSize(Math.Abs(w), Math.Abs(h));
                                        if (!LeftMouse)
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
                                            Owner.AutoTilesToolMulti(points, true, tool, autoTiles, tileLayer, currentTexture, prefix, Owner.Control);
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
                                        Owner.AutoTilesToolMulti(toFill, LeftMouse, tool, autoTiles, tileLayer, currentTexture, prefix);
                                    }
                                }
                                else
                                {
                                    if (!selecting)
                                    {
                                        if (tool == Tools.Spikes && lm)
                                        {
                                            Owner.SpikesFillTool(selection.X + CameraX, selection.Y + CameraY, lm, tool, autoTiles, tileLayer, currentTexture, !Owner.IsKeyHeld(Keys.RightBracket), !Owner.IsKeyHeld(Keys.LeftBracket));
                                        }
                                        else
                                        {
                                            selecting = true;
                                            Owner.TileFillTool(selection.X + CameraX, selection.Y + CameraY, lm, tool, autoTiles, currentTile, tileLayer, currentTexture, prefix, lr: !Owner.IsKeyHeld(Keys.RightBracket), ud: !Owner.IsKeyHeld(Keys.LeftBracket));
                                        }
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
                            if (LeftMouse)
                            {
                                if (!selecting)
                                {
                                    int id = 0;
                                    while (Owner.LevelTrinkets.ContainsKey(id))
                                    {
                                        id++;
                                    }
                                    Trinket t = new Trinket(0, 0, trinketTexture, trinketTexture.AnimationFromName(trinketAnimation), Owner.ScriptFromName("trinket"), Owner);
                                    if (t is Trinket)
                                    {
                                        t.CenterX = selection.CenterX + CameraX;
                                        t.CenterY = selection.CenterY + CameraY;
                                        t.InitializePosition();
                                        t.SetID(id);
                                        Sprites.Add(t);
                                        selecting = true;
                                    }
                                }
                            }
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Trinket)
                                    {
                                        Owner.DeleteSprite(sprite);
                                    }
                                }
                            }
                            else if (selecting && !LeftMouse)
                            {
                                selecting = false;
                            }
                        }
                        else if (tool == Tools.Checkpoint)
                        {
                            if (LeftMouse & !selecting)
                            {
                                selecting = true;
                                Texture sp32 = Owner.TextureFromName("sprites32");
                                Checkpoint cp = new Checkpoint(selection.X + CameraX, selection.Y + CameraY, Owner, sp32, sp32.AnimationFromName("CheckOff"), sp32.AnimationFromName("CheckOn"), flipToolX, flipToolY);
                                cp.CenterX = selection.CenterX + CameraX;
                                if (!flipToolY)
                                    cp.Bottom = selection.Bottom + CameraY;
                                cp.InitializePosition();
                                Sprites.Add(cp);
                            }
                            else if (!LeftMouse & selecting)
                                selecting = false;
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Checkpoint)
                                    {
                                        Sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Disappear)
                        {
                            if (true && !dragging)
                            {
                                selection.SetSize(1, 1);
                                if (LeftMouse)
                                {
                                    dragging = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                            }
                            else if (dragging)
                            {
                                int w = (int)Math.Floor((MouseX - selectOrigin.X) / 8);
                                if (w >= 0) w += 1;
                                else w -= 1;
                                selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                selection.Y = selectOrigin.Y;
                                selection.SetSize(Math.Abs(w), 1);
                                if (!LeftMouse)
                                {
                                    DisappearTool(selection.CenterX + CameraX, selection.CenterY + CameraY, Math.Abs(w));
                                    dragging = false;
                                }
                            }
                            else if (LeftMouse && !selecting)
                            {
                                selecting = true;
                                DisappearTool(selection.CenterX + CameraX, selection.CenterY + CameraY);
                            }
                            else if (!LeftMouse && selecting)
                                selecting = false;
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Platform)
                                    {
                                        Sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Conveyor)
                        {
                            if (true && !dragging)
                            {
                                selection.SetSize(1, 1);
                                if (LeftMouse)
                                {
                                    dragging = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                            }
                            else if (dragging)
                            {
                                int w = (int)Math.Floor((MouseX - selectOrigin.X) / 8);
                                if (w >= 0) w += 1;
                                else w -= 1;
                                selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                selection.Y = selectOrigin.Y;
                                selection.SetSize(Math.Abs(w), 1);
                                if (!LeftMouse)
                                {
                                    ConveyorTool(selection.CenterX + CameraX, selection.CenterY + CameraY, Math.Abs(w));
                                    dragging = false;
                                }
                            }
                            else if (LeftMouse && !selecting)
                            {
                                selecting = true;
                                ConveyorTool(selection.CenterX + CameraX, selection.CenterY + CameraY);
                            }
                            else if (!LeftMouse && selecting)
                                selecting = false;
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Platform)
                                    {
                                        Sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Platform)
                        {
                            if (true && !dragging)
                            {
                                selection.SetSize(1, 1);
                                if (LeftMouse)
                                {
                                    dragging = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                            }
                            else if (dragging)
                            {
                                int w = (int)Math.Floor((MouseX - selectOrigin.X) / 8);
                                if (w >= 0) w += 1;
                                else w -= 1;
                                selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                selection.Y = selectOrigin.Y;
                                selection.SetSize(Math.Abs(w), 1);
                                if (!LeftMouse)
                                {
                                    PlatformTool(selection.CenterX + CameraX, selection.CenterY + CameraY, Math.Abs(w));
                                    dragging = false;
                                }
                            }
                            else if (LeftMouse && !selecting)
                            {
                                selecting = true;
                                PlatformTool(selection.CenterX + CameraX, selection.CenterY + CameraY);
                            }
                            else if (!LeftMouse && selecting)
                                selecting = false;
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Platform)
                                    {
                                        Sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                            else if (MiddleMouse && !stillHolding)
                            {
                                stillHolding = true;
                                List<Sprite> spr = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
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
                            else if (!MiddleMouse && stillHolding)
                                stillHolding = false;
                        }
                        else if (tool == Tools.Enemy)
                        {
                            if (LeftMouse && !selecting)
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
                                    Sprites.AddForCollisions(enemy);
                                    if (!hudSprites.Contains(toolPrompt))
                                        hudSprites.Add(toolPrompt);
                                    toolPromptImportant = true;
                                    GiveDirection = (d) =>
                                    {
                                        if (d == Keys.Up)
                                            enemy.YVelocity = -2;
                                        else if (d == Keys.Down)
                                            enemy.YVelocity = 2;
                                        else if (d == Keys.Left)
                                            enemy.XVelocity = -2;
                                        else if (d == Keys.Right)
                                            enemy.XVelocity = 2;
                                        else if (d == Keys.Escape)
                                            Sprites.Remove(enemy);
                                        toolPromptImportant = false;
                                    };
                                }
                            }
                            else if (!LeftMouse && selecting)
                                selecting = false;
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Enemy)
                                    {
                                        Sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                            else if (MiddleMouse && !stillHolding)
                            {
                                stillHolding = true;
                                List<Sprite> spr = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
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
                            else if (!MiddleMouse && stillHolding)
                                stillHolding = false;
                        }
                        else if (tool == Tools.GravityLine || tool == Tools.WarpLine)
                        {
                            if (selecting)
                            {
                                int w = (int)Math.Floor((MouseX - selectOrigin.X) / 8);
                                int h = (int)Math.Floor((MouseY - selectOrigin.Y) / 8);
                                if (Math.Abs(w) > Math.Abs(h)) h = 0;
                                else w = 0;
                                if (w >= 0) w += 1;
                                else w -= 1;
                                if (h >= 0) h += 1;
                                else h -= 1;
                                selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                                selection.SetSize(Math.Abs(w), Math.Abs(h));
                                if (!LeftMouse)
                                {
                                    selecting = false;
                                    Texture tex = Owner.TextureFromName("lines");
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
                                        Sprites.AddForCollisions(gl);
                                    }
                                    else if (tool == Tools.WarpLine)
                                    {
                                        if (hor && selection.Bottom == Game.RESOLUTION_HEIGHT)
                                        {
                                            y = Game.RESOLUTION_HEIGHT - 1;
                                        }
                                        else if (!hor && selection.Right == Game.RESOLUTION_WIDTH)
                                        {
                                            x = Game.RESOLUTION_WIDTH - 1;
                                        }
                                        WarpLine wl = new WarpLine(x + CameraX, y + CameraY, tex, hor ? tex.AnimationFromName("HWarpLine") : tex.AnimationFromName("VWarpLine"), (int)Math.Max(selection.Width / 8, selection.Height / 8), hor, 0, 0, 0);
                                        if (hor)
                                        {
                                            if (y == 0)
                                            {
                                                wl.Offset = new PointF(0, Game.RESOLUTION_HEIGHT);
                                                wl.Direction = -1;
                                            }
                                            else if (y == Game.RESOLUTION_HEIGHT - 1)
                                            {
                                                wl.Offset = new PointF(0, -Game.RESOLUTION_HEIGHT);
                                                wl.Direction = 1;
                                            }
                                        }
                                        else
                                        {
                                            if (x == 0)
                                            {
                                                wl.Offset = new PointF(Game.RESOLUTION_WIDTH, 0);
                                                wl.Direction = -1;
                                            }
                                            else if (x == Game.RESOLUTION_WIDTH - 1)
                                            {
                                                wl.Offset = new PointF(-Game.RESOLUTION_WIDTH, 0);
                                                wl.Direction = 1;
                                            }
                                        }
                                        wl.InitializePosition();
                                        Sprites.Add(wl);
                                    }
                                }
                            }
                            else
                            {
                                if (LeftMouse)
                                {
                                    selecting = true;
                                    selectOrigin = new PointF(selection.X, selection.Y);
                                }
                                else if (RightMouse)
                                {
                                    List<Sprite> spr = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                    foreach (Sprite sprite in spr)
                                    {
                                        if ((tool == Tools.GravityLine && sprite is GravityLine) || (tool == Tools.WarpLine && sprite is WarpLine))
                                        {
                                            Sprites.RemoveFromCollisions(sprite);
                                        }
                                    }
                                }
                                else if (tool == Tools.GravityLine && MiddleMouse && !stillHolding)
                                {
                                    stillHolding = true;
                                    List<Sprite> spr = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
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
                                else if (!MiddleMouse && stillHolding)
                                    stillHolding = false;
                            }
                        }
                        else if (tool == Tools.Start)
                        {
                            if (LeftMouse)
                            {
                                if (!Sprites.Contains(Player))
                                {
                                    Sprites.Add(Player);
                                }
                                Player.Visible = true;
                                Player.CenterX = selection.CenterX + CameraX;
                                Player.Bottom = selection.Bottom + CameraY;
                                Player.FlipX = flipToolX;
                                Player.FlipY = flipToolY;
                                if (flipToolY) Player.Gravity = -Math.Abs(Player.Gravity);
                                else Player.Gravity = Math.Abs(Player.Gravity);
                                Owner.StartX = (int)Player.X;
                                Owner.StartY = (int)Player.Y;
                                Owner.StartRoomX = Owner.CurrentRoom.X;
                                Owner.StartRoomY = Owner.CurrentRoom.Y;
                                defaultPlayer = Player.Name;
                            }
                            else if (RightMouse)
                            {
                                if (!(CurrentRoom.X == Owner.StartRoomX && CurrentRoom.Y == Owner.StartRoomY))
                                {
                                    Owner.LoadRoom(Owner.StartRoomX, Owner.StartRoomY);
                                }
                                if (!Sprites.Contains(Player))
                                {
                                    Sprites.Add(Player);
                                    Player.Visible = true;
                                }
                                Player.X = Owner.StartX;
                                Player.Y = Owner.StartY;
                            }
                            else
                            {

                            }
                        }
                        else if (tool == Tools.Crewman)
                        {
                            if (LeftMouse && !selecting)
                            {
                                selecting = true;
                                float x = selection.CenterX + CameraX;
                                float y = selection.Y + CameraY;
                                float bottom = selection.Bottom + CameraY;
                                bool flipX = flipToolX;
                                bool flipY = flipToolY;
                                CrewmanTexture tex = crewmanTexture;
                                if (tex != null)
                                {
                                    string name = tex.Name.First().ToString().ToUpper() + tex.Name.Substring(1);
                                    if (!Owner.UserAccessSprites.ContainsKey(name) || Owner.IsKeyHeld(Keys.LeftShift))
                                    {
                                        Animation stand = tex.AnimationFromName("Standing"), walk = tex.AnimationFromName("Walking"),
                                        fall = tex.AnimationFromName("Falling"), jump = tex.AnimationFromName("Jumping"), die = tex.AnimationFromName("Dying");
                                        Crewman c = new Crewman(0, 0, tex, Owner, name, stand, walk, fall, jump, die);
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
                                        if (!Owner.IsKeyHeld(Keys.LeftShift))
                                            Owner.UserAccessSprites.Add(name, c);
                                        else
                                            c.Name = null;
                                        c.InitializePosition();
                                        Sprites.Add(c);
                                    }
                                    else
                                    {
                                        Sprite c = Owner.UserAccessSprites[name];
                                        c.ResetAnimation();
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
                                        if (!Sprites.Contains(c))
                                            Sprites.Add(c);
                                    }
                                }
                            }
                            else if (!LeftMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sp in spr)
                                {
                                    if (sp is Crewman && sp != Player)
                                    {
                                        Owner.DeleteSprite(sp);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.WarpToken)
                        {
                            if (LeftMouse && !selecting)
                            {
                                selecting = true;
                                if (currentWarp is null)
                                {
                                    WarpToken wt = new WarpToken(0, 0, warpTokenTexture, warpTokenTexture.AnimationFromName(warpTokenAnimation), 0, 0, 0, 0, Owner);
                                    if (wt.Animation is object)
                                    {
                                        wt.CenterX = selection.CenterX + CameraX;
                                        wt.CenterY = selection.CenterY + CameraY;
                                        Sprites.Add(wt);
                                        currentWarp = wt;
                                        WarpRoom = Owner.CurrentRoom;
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
                                    currentWarp.OutRoomX = Owner.CurrentRoom.X;
                                    currentWarp.OutRoomY = Owner.CurrentRoom.Y;
                                    RoomDatas[WarpRoom.X + WarpRoom.Y * 100] = WarpRoom.Save(Owner);
                                    Texture sp32 = Owner.TextureFromName("sprites32");
                                    WarpToken.WarpData data = new WarpToken.WarpData(currentWarp, WarpRoom.X, WarpRoom.Y);
                                    WarpTokenOutput wto = new WarpTokenOutput(currentWarp.OutX, currentWarp.OutY, sp32, sp32.AnimationFromName("WarpToken"), data, currentWarp.ID);
                                    Owner.Warps.Add(Owner.GetNextWarpID(), data);
                                    currentWarp = null;
                                    WarpRoom = null;
                                    Sprites.Add(wto);
                                }
                            }
                            else if (MiddleMouse & !selecting)
                            {
                                selecting = true;
                                List<Sprite> col = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sprite in col)
                                {
                                    if (sprite is WarpToken)
                                    {
                                        WarpToken w = sprite as WarpToken;
                                        int x = (int)w.OutX / Room.ROOM_WIDTH;
                                        int y = (int)w.OutY / Room.ROOM_HEIGHT;
                                        if (x != Owner.CurrentRoom.X || y != Owner.CurrentRoom.Y)
                                        {
                                            Owner.LoadRoom(x, y);
                                        }
                                    }
                                    else if (sprite is WarpTokenOutput)
                                    {
                                        WarpTokenOutput w = sprite as WarpTokenOutput;
                                        int x = w.Parent.InRoom.X;
                                        int y = w.Parent.InRoom.Y;
                                        if (x != Owner.CurrentRoom.X || y != Owner.CurrentRoom.Y)
                                        {
                                            Owner.LoadRoom(x, y);
                                        }
                                    }
                                }
                            }
                            else if (!LeftMouse && !MiddleMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is WarpToken)
                                    {
                                        Owner.DeleteSprite(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.ScriptBox)
                        {
                            if (selecting)
                            {
                                int w = (int)Math.Floor((MouseX - selectOrigin.X) / 8);
                                int h = (int)Math.Floor((MouseY - selectOrigin.Y) / 8);
                                if (w >= 0) w += 1;
                                else w -= 1;
                                if (h >= 0) h += 1;
                                else h -= 1;
                                selection.X = Math.Min(selectOrigin.X, selectOrigin.X + ((w + 1) * 8));
                                selection.Y = Math.Min(selectOrigin.Y, selectOrigin.Y + ((h + 1) * 8));
                                selection.SetSize(Math.Abs(w), Math.Abs(h));
                                if (!LeftMouse)
                                {
                                    selecting = false;
                                    if (currentlyResizing is null)
                                    {
                                        ScriptBox sb = new ScriptBox(selection.X + CameraX, selection.Y + CameraY, Owner.BoxTexture, selection.WidthTiles, selection.HeightTiles, null, Owner);
                                        Sprites.Add(sb);
                                        toolPrompt.Text = "[Script Name]";
                                        toolPromptImportant = true;
                                        Owner.StartTyping(toolPrompt, (r, st) =>
                                        {
                                            if (r)
                                            {
                                                Script s = Owner.ScriptFromName(toolPrompt.Text);
                                                if (s is null)
                                                {
                                                    s = new Script(new Command[] { }, toolPrompt.Text, "");
                                                    Owner.Scripts.Add(s.Name, s);
                                                }
                                                sb.Script = s;
                                            }
                                            else
                                            {
                                                Sprites.Remove(sb);
                                            }
                                            toolPromptImportant = false;
                                        }, true);
                                        toolPrompt.SelectionStart = 0;
                                        toolPrompt.SelectionLength = toolPrompt.Text.Length;
                                        toolPrompt.Text = toolPrompt.Text;
                                        if (!hudSprites.Contains(toolPrompt))
                                            hudSprites.Add(toolPrompt);
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
                            else if (LeftMouse && !stillHolding)
                            {
                                if (Owner.IsKeyHeld(Keys.LeftShift))
                                {
                                    stillHolding = true;
                                    List<Sprite> col = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY, 8, 8);
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
                            else if (MiddleMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is ScriptBox && (sprite as ScriptBox).Script is object)
                                    {
                                        Owner.OpenScript((sprite as ScriptBox).Script);
                                        break;
                                    }
                                }
                            }
                            else if (stillHolding)
                            {
                                stillHolding = false;
                            }
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is ScriptBox)
                                    {
                                        Owner.DeleteSprite(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Terminal)
                        {
                            if (LeftMouse && !selecting)
                            {
                                selecting = true;
                                Terminal t = new Terminal(0, 0, terminalTexture, terminalTexture.AnimationFromName(terminalOff), terminalTexture.AnimationFromName(terminalOn), null, false, Owner);
                                t.CenterX = selection.CenterX + CameraX;
                                t.FlipX = flipToolX;
                                if (flipToolY)
                                {
                                    t.FlipY = true;
                                    t.Y = selection.Y + CameraY;
                                }
                                else
                                    t.Bottom = selection.Bottom + CameraY;
                                Sprites.Add(t);
                                if (!hudSprites.Contains(toolPrompt))
                                    hudSprites.Add(toolPrompt);
                                toolPrompt.Text = "[Script Name]";
                                toolPromptImportant = true;
                                Owner.StartTyping(toolPrompt, (r, st) =>
                                {
                                    if (r)
                                    {
                                        Script s = Owner.ScriptFromName(toolPrompt.Text);
                                        if (s is null)
                                        {
                                            s = new Script(new Command[] { }, toolPrompt.Text, "");
                                            Owner.Scripts.Add(s.Name, s);
                                        }
                                        t.Script = s;
                                    }
                                    else
                                    {
                                        Sprites.Remove(t);
                                    }
                                    toolPromptImportant = false;
                                }, true);
                                toolPrompt.SelectionStart = 0;
                                toolPrompt.SelectionLength = toolPrompt.Text.Length;
                                toolPrompt.Text = toolPrompt.Text;
                            }
                            else if (MiddleMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Terminal && (sprite as Terminal).Script is object)
                                    {
                                        Owner.OpenScript((sprite as Terminal).Script);
                                        break;
                                    }
                                }
                            }
                            else if (!LeftMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Terminal)
                                    {
                                        Sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.RoomText)
                        {
                            if (LeftMouse && !selecting)
                            {
                                selecting = true;
                                StringDrawable s = new StringDrawable(selection.X + CameraX, selection.Y + CameraY, Owner.FontTexture, "", Color.White);
                                Owner.StartTyping(s, (b, st) =>
                                {
                                    if (!b)
                                    {
                                        Sprites.Remove(s);
                                    }
                                }, true);
                                Sprites.Add(s);
                            }
                            else if (!LeftMouse && !MiddleMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (MiddleMouse && !selecting)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sp in spr)
                                {
                                    if (sp is StringDrawable)
                                    {
                                        StringDrawable s = sp as StringDrawable;
                                        string prText = s.Text;
                                        Owner.StartTyping(s, (b, st) =>
                                        {
                                            if (!b)
                                            {
                                                s.Text = prText;
                                            }
                                        }, true);
                                        selecting = true;
                                        break;
                                    }
                                }
                            }
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sp in spr)
                                {
                                    if (sp is StringDrawable)
                                    {
                                        if (sp == Owner.TypingTo)
                                            Owner.StartTyping(null);
                                        Sprites.RemoveFromCollisions(sp);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Lever)
                        {
                            if (LeftMouse && !selecting)
                            {
                                selecting = true;
                                Animation offAnim = leverTexture.AnimationFromName(leverAnimation + "Off");
                                Animation onAnim = leverTexture.AnimationFromName(leverAnimation + "On");
                                Lever lever = new Lever(0, 0, leverTexture, offAnim, onAnim, Script.Empty, false, false, Owner, false);
                                if (lever is object)
                                {
                                    if (flipToolX)
                                    {
                                        lever.CenterY = selection.CenterY;
                                        if (flipToolY)
                                        {
                                            lever.Right = selection.Right;
                                            lever.FlipX = true;
                                        }
                                        else
                                            lever.X = selection.X;
                                    }
                                    else
                                    {
                                        lever.CenterX = selection.CenterX;
                                        if (flipToolY)
                                        {
                                            lever.Y = selection.Y;
                                            lever.FlipY = true;
                                        }
                                        else
                                            lever.Bottom = selection.Bottom;
                                    }
                                }
                                Sprites.Add(lever);
                                if (!hudSprites.Contains(toolPrompt))
                                    hudSprites.Add(toolPrompt);
                                toolPrompt.Text = "[Script Name]";
                                toolPromptImportant = true;
                                Owner.StartTyping(toolPrompt, (r, st) =>
                                {
                                    if (r)
                                    {
                                        Script s = Owner.ScriptFromName(toolPrompt.Text);
                                        if (s is null)
                                        {
                                            s = new Script(new Command[] { }, toolPrompt.Text, "");
                                            Owner.Scripts.Add(s.Name, s);
                                        }
                                        lever.Script = s;
                                    }
                                    else
                                    {
                                        Sprites.Remove(lever);
                                    }
                                    toolPromptImportant = false;
                                }, true);
                                toolPrompt.SelectionStart = 0;
                                toolPrompt.SelectionLength = toolPrompt.Text.Length;
                                toolPrompt.Text = toolPrompt.Text;
                            }
                            else if (!LeftMouse && selecting)
                                selecting = false;
                            else if (MiddleMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Lever && (sprite as Lever).Script is object)
                                    {
                                        Owner.OpenScript((sprite as Lever).Script);
                                        break;
                                    }
                                }
                            }
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(MouseX + CameraX, MouseY + CameraY, 2, 2);
                                foreach (Sprite sprite in spr)
                                {
                                    if (sprite is Lever)
                                    {
                                        Sprites.RemoveFromCollisions(sprite);
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.CustomSprite)
                        {
                            if (LeftMouse & !selecting)
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
                                    Sprites.AddForCollisions(cs);
                                }
                            }
                            else if (!LeftMouse && selecting)
                            {
                                selecting = false;
                            }
                            else if (RightMouse)
                            {
                                List<Sprite> spr = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY);
                                foreach (Sprite sp in spr)
                                {
                                    if (sp.GetType() == typeof(Sprite))
                                    {
                                        Sprites.RemoveFromCollisions(sp);
                                        //if (sp == typingTo)
                                        //{
                                        //    EscapeTyping();
                                        //}
                                    }
                                }
                            }
                        }
                        else if (tool == Tools.Point)
                        {
                            if (LeftMouse)
                            {
                                //string x = selection.X.ToString();
                                //string y = selection.Y.ToString();
                                //typing = true;
                                //tool = prTool;
                                //EditorTool t = EditorTools[(int)prTool];
                                //editorTool.Text = t.Hotkey.ToString() + " - " + t.Name;
                                //TypeText(x + "," + y);
                                //textChanged?.Invoke(typingTo.Text);
                            }
                        }
                        else if (tool == Tools.Attach)
                        {
                            if (LeftMouse)
                            {
                                tool = Tools.Select;
                                toolPrompt.Text = "";
                                toolPromptImportant = false;
                                IPlatform att = null;
                                List<Sprite> col = Sprites.GetPotentialColliders(selection.X + CameraX, selection.Y + CameraY, 8, 8);
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
            } // Backup
        }

        internal void ExitPlaytest()
        {
            SaveRoom = false;
            selection.Visible = true;
            editorTool.Visible = true;
            Owner.LoadSave(editorState);
            Owner.AddLayer(this);
        }

        private void ClearSelection()
        {
            previewTile.Visible = false;
            foreach (BoxSprite sprite in selectBoxes)
            {
                Sprites.Remove(sprite);
                sprite.Dispose();
            }
            selectBoxes.Clear();
            selectedSprites.Clear();
        }

        public void ShowTileIndicators()
        {
            int x = CurrentRoom.X;
            int y = CurrentRoom.Y;
            while (indic.Count > 0)
            {
                Sprite i = indic[0];
                hudSprites.Remove(i);
                indic.RemoveAt(0);
            }
            if (showIndicators)
            {
                Texture tileIndicators = Owner.TextureFromName("tileindicator");
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
                        List<Tile> tiles = Room.GetTiles(room, Owner);
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
                                    x4 = Game.RESOLUTION_WIDTH - 8;
                                    y4 = i * 8;
                                    tx = 0;
                                    ty = 1;
                                    break;
                                case 2:
                                    x4 = i * 8;
                                    y4 = Game.RESOLUTION_HEIGHT - 8;
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
        }

        private void ReplaceTiles()
        {
            List<PointF> grounds = new List<PointF>();
            List<PointF> backgrounds = new List<PointF>();
            List<PointF> spikes = new List<PointF>();
            int l = tileLayer;
            tileLayer = -2;
            if (replaceAutoTiles)
            {
                foreach (Tile tile in Sprites.Where((s) => s is Tile && s.Layer == -2))
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
                    Owner.AutoTilesToolMulti(grounds, true, Tools.Ground, CurrentRoom.Ground, tileLayer, currentTexture, 'g');
                }
                if (backgrounds.Count > 0)
                {
                    Owner.AutoTilesToolMulti(backgrounds, true, Tools.Background, CurrentRoom.Background, tileLayer, currentTexture, 'b');
                }
                if (spikes.Count > 0)
                {
                    Owner.AutoTilesToolMulti(spikes, true, Tools.Spikes, CurrentRoom.Spikes, tileLayer, currentTexture, 's');
                }
                tileLayer = l;
                tool = curTool;
                prefix = prf;
            }
            else
            {
                foreach (Tile tile in Sprites.Where((s) => s is Tile))
                {
                    tile.ChangeTexture(currentTexture);
                }
            }
            replaceTiles = false;
            replaceAutoTiles = false;
        }

        private void ConveyorTool(float x, float y, int length = -1)
        {
            Platform platform = new Platform(0, 0, platformTexture, platformTexture.AnimationFromName(conveyorAnimation));
            if (platform.Animation is object)
            {
                if (length > 0)
                    platform.Length = length;
                platform.SingleDirection = true;
                platform.CenterX = x;
                platform.CenterY = y;
                Color c = roomColor;
                int r = c.R + (255 - c.R) / 2;
                int g = c.G + (255 - c.G) / 2;
                int b = c.B + (255 - c.B) / 2;
                platform.Color = Color.FromArgb(255, r, g, b);
                platform.ResetAnimation();
                platform.InitializePosition();
                Sprites.AddForCollisions(platform);
                toolPrompt.Text = "Press arrow key for platform direction";
                toolPromptImportant = true;
                GiveDirection = (d) =>
                {
                    if (d == Keys.Left)
                    {
                        platform.Conveyor = -2;
                        platform.FlipX = true;
                    }
                    else if (d == Keys.Right)
                    {
                        platform.Conveyor = 2;
                    }
                    else if (d == Keys.Escape)
                        Sprites.Remove(platform);
                    toolPromptImportant = false;
                };
            }
        }
        private void PlatformTool(float x, float y, int length = -1)
        {
            Platform platform = new Platform(0, 0, platformTexture, platformTexture.AnimationFromName(platformAnimation));
            if (length > 0)
                platform.Length = length;
            if (platform.Animation is object)
            {
                platform.CenterX = x;
                platform.CenterY = y;
                Color c = roomColor;
                int r = c.R + (255 - c.R) / 2;
                int g = c.G + (255 - c.G) / 2;
                int b = c.B + (255 - c.B) / 2;
                platform.Color = Color.FromArgb(255, r, g, b);
                platform.ResetAnimation();
                platform.InitializePosition();
                Sprites.AddForCollisions(platform);
                toolPrompt.Text = "Press arrow key for platform direction";
                if (!hudSprites.Contains(toolPrompt))
                    hudSprites.Add(toolPrompt);
                toolPromptImportant = true;
                GiveDirection = (d) =>
                {
                    if (d == Keys.Up)
                        platform.YVelocity = -2;
                    else if (d == Keys.Down)
                        platform.YVelocity = 2;
                    else if (d == Keys.Left)
                        platform.XVelocity = -2;
                    else if (d == Keys.Right)
                        platform.XVelocity = 2;
                    else if (d == Keys.Escape)
                        Sprites.Remove(platform);
                    toolPromptImportant = false;
                };
            }
        }
        private void DisappearTool(float x, float y, int length = -1)
        {
            Platform platform = new Platform(0, 0, platformTexture, platformTexture.AnimationFromName(platformAnimation), 0, 0, 0, true, platformTexture.AnimationFromName(disappearAnimation), 4);
            if (length > 0)
                platform.Length = length;
            if (platform.Animation is object && platform.DisappearAnimation is object)
            {
                platform.CenterX = x;
                platform.CenterY = y;
                Color c = roomColor;
                int r = c.R + (255 - c.R) / 2;
                int g = c.G + (255 - c.G) / 2;
                int b = c.B + (255 - c.B) / 2;
                platform.Color = Color.FromArgb(255, r, g, b);
                platform.InitializePosition();
                Sprites.AddForCollisions(platform);
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
                Owner.ShowDialog("Change which property?", "", properties.ToArray(), (r, st) =>
                {
                    if (r)
                    {
                        string answer = st;
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
                                        val = (selectedSprites[i].GetProperty(answer) ?? "").ToString();
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
                                case SpriteProperty.Types.Color:
                                    break;
                                case SpriteProperty.Types.Texture:
                                    choices = new List<string>();
                                    foreach (Texture texture in Owner.Textures.Values)
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
                                    foreach (SoundEffect sound in Owner.Sounds.Values)
                                    {
                                        choices.Add(sound.Name);
                                    }
                                    break;
                                case SpriteProperty.Types.Script:
                                    choices = new List<string>();
                                    foreach (Script script in Owner.Scripts.Values)
                                    {
                                        choices.Add(script.Name);
                                    }
                                    break;
                            }
                            if (choices is object)
                            {
                                Owner.ShowDialog(answer + " - " + selectedSprites.First().Properties[answer].Description, val, choices.ToArray(), (r2, st2) =>
                                {
                                    if (r2)
                                    {
                                        string ans = st2;
                                        switch (selectedSprites.First().Properties[answer].Type)
                                        {
                                            case SpriteProperty.Types.Int:
                                                if (int.TryParse(ans, out int intV))
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, intV, Owner);
                                                    }
                                                break;
                                            case SpriteProperty.Types.Float:
                                                if (float.TryParse(ans, out float floatV))
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, floatV, Owner);
                                                    }
                                                break;
                                            case SpriteProperty.Types.String:
                                                foreach (Sprite sprite in selectedSprites)
                                                {
                                                    sprite.SetProperty(answer, ans, Owner);
                                                }
                                                break;
                                            case SpriteProperty.Types.Bool:
                                                if (bool.TryParse(ans, out bool boolV))
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, boolV, Owner);
                                                    }
                                                break;
                                            case SpriteProperty.Types.Texture:
                                                Texture textureV = Owner.TextureFromName(ans);
                                                if (textureV is object)
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, ans, Owner);
                                                    }
                                                break;
                                            case SpriteProperty.Types.Animation:
                                                foreach (Sprite sprite in selectedSprites)
                                                {
                                                    if (sprite.Texture.AnimationFromName(ans) is object)
                                                        sprite.SetProperty(answer, ans, Owner);
                                                }
                                                break;
                                            case SpriteProperty.Types.Sound:
                                                SoundEffect soundV = Owner.GetSound(ans);
                                                if (soundV is object)
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, ans, Owner);
                                                    }
                                                break;
                                            case SpriteProperty.Types.Script:
                                                Script scriptV = Owner.ScriptFromName(ans);
                                                if (scriptV is object)
                                                    foreach (Sprite sprite in selectedSprites)
                                                    {
                                                        sprite.SetProperty(answer, ans, Owner);
                                                    }
                                                break;
                                        }
                                    }
                                });
                            }
                            else if (selProp.Type == SpriteProperty.Types.Color)
                            {
                                string v = "White";
                                Color c = Color.FromArgb((int)selProp.GetValue());
                                v = c.Name;
                                if (v == "0")
                                {
                                    v = ((int)selProp.GetValue()).ToString("X8");
                                }
                                Owner.ShowColorDialog(selProp.Name + " - " + selProp.Description, v, (r2, s2) =>
                                {
                                    if (r2)
                                    {
                                        Color? cl = Owner.GetColor(s2);
                                        foreach (Sprite sprite in selectedSprites)
                                        {
                                            if (cl.HasValue)
                                            {
                                                Color clr = Color.FromArgb(cl.Value.A, cl.Value.R, cl.Value.G, cl.Value.B);
                                                sprite.SetProperty(selProp.Name, clr.ToArgb(), Owner);
                                            }
                                        }
                                    }
                                });
                            }
                        }
                    }
                });
            }
        }

        private void ChangeRoomColour(string colour)
        {
            AutoTileSettings.PresetGroup g = Owner.RoomPresets[CurrentRoom.GroupName];
            AutoTileSettings.RoomPreset p;
            if (!g.ContainsKey(colour)) return;
            p = g[colour];
            TileTexture t = p.Texture;
            if (t != null && CurrentRoom.TileTexture != t)
            {
                foreach (Sprite tile in Sprites)
                {
                    if (tile is Tile)
                    {
                        (tile as Tile).ChangeTexture(t);
                    }
                }
                CurrentRoom.TileTexture = t;
            }
            CurrentRoom.UsePreset(p, g.Name);
            foreach (Sprite sprite in Sprites)
            {
                if (sprite is Platform)
                {
                    Color c = roomColor;
                    int tr = c.R + (255 - c.R) / 2;
                    int tg = c.G + (255 - c.G) / 2;
                    int tb = c.B + (255 - c.B) / 2;
                    sprite.Color = Color.FromArgb(255, tr, tg, tb);
                }
                else if (sprite is Enemy)
                {
                    sprite.Color = roomColor;
                }
            }
            replaceTiles = true;
            replaceAutoTiles = true;
        }

        private void HandleHUD()
        {
            if (!hudSprites.Contains(selection))
                hudSprites.Add(selection);
            if (Owner.EnableExtraHud)
            {
                if (!ExtraHud.Contains(editorToolbarTop))
                    ExtraHud.Add(editorToolbarTop);
                if (!ExtraHud.Contains(TrinketCount))
                    ExtraHud.Add(TrinketCount);
                editorToolbarTop.Y = Game.RESOLUTION_HEIGHT;
                TrinketCount.CenterX = -Game.HUD_LEFT / 2;
                TrinketCount.Text = Owner.LevelTrinkets.Count.ToString();
            }
            else
            {
                topEditor.Visible = RoomLoc.Visible = selecLoc.Visible = editorTool.Visible = toolPrompt.Visible = Owner.RoomName.Visible = Owner.RoomNameBar.Visible =
                    true;
                if (!hudSprites.Contains(editorToolbarTop))
                    hudSprites.Add(editorToolbarTop);
                if (CurrentEditingFocus == FocusOptions.Tileset)
                {
                    topEditor.Visible = editorTool.Visible = RoomLoc.Visible = selecLoc.Visible = previewTile.Visible = toolPrompt.Visible = Owner.RoomName.Visible = Owner.RoomNameBar.Visible =
                    false;
                    selection.Visible = tileSelection.Visible =
                        CurrentEditingFocus == FocusOptions.Tileset;
                }
                if ((selection.Y < 56 && Owner.MouseIn) || hideToolbars)
                {
                    if (topEditor.Bottom > 0)
                    {
                        if (topEditor.Bottom < Game.RESOLUTION_HEIGHT || topEditor.Bottom > Game.RESOLUTION_HEIGHT)
                            MoveTop(-2);
                    }
                    else if (!hideToolbars)
                    {
                        MoveTop(Game.RESOLUTION_HEIGHT + topEditor.Height);
                    }
                }
                else if (topEditor.Y < 0 || topEditor.Bottom >= Game.RESOLUTION_HEIGHT)
                {
                    MoveTop(2);
                    if (topEditor.Y >= Game.RESOLUTION_HEIGHT)
                        MoveTop(-Game.RESOLUTION_HEIGHT - topEditor.Height);
                }
            }
            if (currentTool.PromptImportant)
                toolPrompt.Color = Color.Red;
            else
                toolPrompt.Color = Color.LightBlue;
            selection.Visible = true;
            if (Owner.MouseIn)
            {
                selecLoc.Text = "[" + selection.X.ToString() + "," + selection.Y.ToString() + "]";
            }
            else
            {
                selecLoc.Text = "[-,-]";
            }
            selecLoc.Right = RoomLoc.X - 4;
            editorTool.Text = currentTool.Key + " - " + currentTool.Name;
            toolPrompt.Text = currentTool.Prompt;
            if (currentTool.PreviewTexture is object)
            {
                previewTile.Visible = true;
                if (previewTile.Texture != currentTool.PreviewTexture)
                {
                    hudSprites.Remove(previewTile);
                    previewTile.ChangeTexture(currentTool.PreviewTexture);
                    hudSprites.Add(previewTile);
                }
                previewTile.Animation = Animation.Static(currentTool.PreviewPoint.X, currentTool.PreviewPoint.Y, currentTool.PreviewTexture);
            }
            else
            {
                previewTile.Visible = false;
            }
            Owner.RoomNameBar.Color = Color.FromArgb(100, 0, 0, 0);
            if (selection.Y > 200 || hideToolbars)
            {
                if (Owner.RoomNameBar.Y < Game.RESOLUTION_HEIGHT)
                {
                    Owner.RoomName.Y += 1;
                    Owner.RoomNameBar.Y += 1;
                }
            }
            else if (Owner.RoomNameBar.Bottom > Game.RESOLUTION_HEIGHT)
            {
                Owner.RoomName.Y -= 1;
                Owner.RoomNameBar.Y -= 1;
            }
            Owner.RoomName.CenterX = Game.RESOLUTION_WIDTH / 2;
            Owner.RoomName.Y = Owner.RoomNameBar.Y + 1;
        }

        private void MoveTop(float y)
        {
            if (editorToolbarTop.Y > Owner.RoomNameBar.Y && Math.Abs(y) < Game.RESOLUTION_HEIGHT)
            {
                Owner.RoomNameBar.Y += y;
                Owner.RoomName.Y += y;
            }
            editorToolbarTop.Y += y;
        }

        private MenuLayer LevelEditorMenu()
        {
            MenuLayer.Builder mlb = new MenuLayer.Builder(Owner);
            int escapeItem;
            mlb.AddItem("Set Level Size", () =>
            {
                Owner.ShowDialog("Level Size (Format: x, y)", WidthRooms.ToString() + ", " + HeightRooms.ToString(), null, (r, st) =>
                {
                    if (r)
                    {
                        int w = 0;
                        int h = 0;
                        string[] s = st.Split(new char[] { ',', 'x', 'X' });
                        if (int.TryParse(s.First(), out w))
                        {
                            WidthRooms = Math.Max(Math.Min(w, 100), 1);
                            if (int.TryParse(s.Last(), out h))
                            {
                                HeightRooms = Math.Max(Math.Min(h, 100), 1);
                            }
                        }
                    }
                });
            }, "Set the size in rooms of the current level.");
            mlb.AddItem("Set Music", () =>
            {
                List<string> ch = new List<string>();
                foreach (Music m in Owner.Songs.Values)
                {
                    ch.Add(m.Name);
                }
                Owner.ShowDialog("Change music", Owner.LevelMusic.Name, ch.ToArray(), (r, st) =>
                {
                    if (r)
                    {
                        Music m = Owner.GetMusic(st);
                        if (m is object)
                            Owner.LevelMusic = m;
                    }
                });
            }, "Set the music that plays at the beginning of the level.");
            mlb.AddItem("Misc. Options", () =>
            {
                MiscOptions();
            }, "Set some less frequent level options.");
            escapeItem = mlb.ItemCount;
            mlb.AddItem("Back", () =>
            {
                Owner.ClearMenu();
            }, "Return to the level editor.");
            mlb.AddItem("Return to Menu", () =>
            {
                Owner.GetSound("crew1")?.Play();
                StringDrawable areYouSure = new StringDrawable(0, 0, Owner.FontTexture, "Would you like to save the current\nlevel before returning to the menu?");
                areYouSure.Layer = int.MaxValue;
                areYouSure.AlignToCenter();
                areYouSure.CenterX = Game.RESOLUTION_WIDTH / 2;
                areYouSure.Y = 12;
                int item = mlb.Result.SelectedItem;
                MenuLayer.Builder ays = new MenuLayer.Builder(Owner);
                ays.AddItem("Cancel", () =>
                {
                    Owner.GetSound("crew1")?.Play();
                    LevelEditorMenu().SelectedItem = item;
                }, "Go back to the level editor menu.");
                ays.AddItem("Don't Save", () =>
                {
                    Owner.GetSound("crew1")?.Play();
                    Owner.ReturnToMenu();
                }, "Return to the main menu without saving the level.");
                ays.Build();
            }, "Return to the main menu.");
            mlb.EscapeItem = escapeItem;
            return mlb.Build();
        }

        private void MiscOptions()
        {
            MenuLayer.Builder mlb = new MenuLayer.Builder(Owner);
            mlb.AddItem("Lose Trinkets: " + (Owner.LoseTrinkets ? "On" : "Off"), () =>
            {
                Owner.ShowDialog("Lose trinkets? When this option is on, the player will lose any trinkets collected since the last checkpoint on death. This means the player will have to get safely to a checkpoint after collecting a trinket in order to keep it.", Owner.LoseTrinkets ? "On" : "Off", new string[] { "On", "Off" }, (r, st) =>
                {
                    if (r)
                    {
                        if (!bool.TryParse(st, out bool lose))
                        {
                            lose = st.ToLower() == "on";
                        }
                        Owner.LoseTrinkets = lose;
                    }
                });
            }, "Set whether or not the player loses trinkets collected\nsince the last checkpoint on death.");
            mlb.EscapeItem = mlb.ItemCount;
            mlb.AddItem("Back", () => 
            {
                Owner.ClearMenu();
                LevelEditorMenu();
            }, "Return to the level editor menu.");
            mlb.Build();
        }
    }
}
