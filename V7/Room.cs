using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace V7
{
    public class Room : IDisposable
    {
        public void Dispose()
        {
            Objects?.Dispose();
        }

        public SpriteCollection Objects = new SpriteCollection();
        public Script EnterScript;
        public Script ExitScript;
        public string Name = "";
        public AutoTileSettings Ground;
        public AutoTileSettings Background;
        public AutoTileSettings Spikes;
        public string GroupName;
        public string PresetName;
        public JToken BG;

        public SortedList<string, float> Tags = new SortedList<string, float>();

        public TileTexture TileTexture;

        public int X;
        public int Y;

        public Point? RoomRight;
        public Point? RoomLeft;
        public Point? RoomUp;
        public Point? RoomDown;
        public virtual int WidthRooms => 1;
        public virtual int HeightRooms => 1;
        public float GetX => X * ROOM_WIDTH;
        public float GetY => Y * ROOM_HEIGHT;
        public float Right => GetX + Width;
        public float Bottom => GetY + Height;
        private float width = 1;
        private float height = 1;
        public virtual float Width => ROOM_WIDTH * width;
        public virtual float Height => ROOM_HEIGHT * height;
        public virtual bool IsGroup => width != 1 || height != 1;

        public Color Color;

        public const int ROOM_WIDTH = 320;
        public const int ROOM_HEIGHT = 240;

        public Room(SpriteCollection objects, Script enter, Script leave)
        {
            Objects = objects;
            EnterScript = enter ?? Script.Empty;
            ExitScript = leave ?? Script.Empty;
            Ground = AutoTileSettings.Default13(3, 2);
            Background = AutoTileSettings.Default13(0, 17);
            Spikes = AutoTileSettings.Default4(8, 0);
        }

        public void UsePreset(AutoTileSettings.RoomPreset preset, string groupName)
        {
            Color = preset.Color;
            Ground = preset.Ground.Initialize();
            Background = preset.Background.Initialize();
            Spikes = preset.Spikes.Initialize();
            GroupName = groupName;
            PresetName = preset.Name;

        }

        public JObject Save(Game game)
        {
            JObject ret = new JObject();
            ret.Add("EnterScript", EnterScript.Name);
            ret.Add("ExitScript", ExitScript.Name);
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Name", Name);
            ret.Add("Color", System.Drawing.Color.FromArgb(Color.A, Color.R, Color.G, Color.B).ToArgb());
            ret.Add("GroupName", GroupName);
            ret.Add("PresetName", PresetName);
            if (RoomRight.HasValue)
            {
                ret.Add("RightX", RoomRight.Value.X);
                ret.Add("RightY", RoomRight.Value.Y);
            }
            if (RoomLeft.HasValue)
            {
                ret.Add("LeftX", RoomLeft.Value.X);
                ret.Add("LeftY", RoomLeft.Value.Y);
            }
            if (RoomDown.HasValue)
            {
                ret.Add("DownX", RoomDown.Value.X);
                ret.Add("DownY", RoomDown.Value.Y);
            }
            if (RoomUp.HasValue)
            {
                ret.Add("UpX", RoomUp.Value.X);
                ret.Add("UpY", RoomUp.Value.Y);
            }
            if (BG is object)
                ret.Add("BG", BG);
            if (game.RoomPresets.ContainsKey(GroupName ?? ""))
            {
                AutoTileSettings.PresetGroup preset = game.RoomPresets[GroupName];
                if (preset.ContainsKey(Ground.Name))
                    ret.Add("GroundName", Ground.Name);
                else
                    ret.Add("Ground", Ground.Save());
                if (preset.ContainsKey(Background.Name))
                    ret.Add("BackgroundName", Background.Name);
                else
                    ret.Add("Background", Background.Save());
                if (preset.ContainsKey(Spikes.Name))
                    ret.Add("SpikesName", Spikes.Name);
                else
                    ret.Add("Spikes", Spikes.Save());
            }
            List<JObject> objs = new List<JObject>();
            for (int i = 0; i < Objects.Count; i++)
            {
                Sprite obj = Objects[i];
                if (obj.IsOnPlatform)
                    continue;
                if (obj is Tile && obj.X >= X * ROOM_WIDTH && obj.Y >= Y * ROOM_HEIGHT && obj.X < (X + 1) * ROOM_WIDTH && obj.Y < (Y + 1) * ROOM_HEIGHT && obj.X % 8 == 0 && obj.Y % 8 == 0) continue;
                Objects[i].InitializePosition();
                Objects[i].InitialX -= X * ROOM_WIDTH;
                Objects[i].InitialY -= Y * ROOM_HEIGHT;
                JObject loadedSprite = Objects[i].Save(game);
                if (loadedSprite is object)
                    objs.Add(loadedSprite);
                Objects[i].InitialX += X * ROOM_WIDTH;
                Objects[i].InitialY += Y * ROOM_HEIGHT;
            }
            ret.Add("Objects", new JArray(objs));
            List<string> tiles = new List<string>();
            List<Sprite> stuff = new List<Sprite>();
            //Objects.SortForCollisions();
            //for (int x = 0; x < ROOM_WIDTH; x += 8)
            //{
            //    for (int y = 0; y < ROOM_HEIGHT; y += 8)
            //    {
            //        List<Sprite> sprites = Objects.GetPotentialColliders(x, y);
            //        int tx = 0;
            //        int ty = 0;
            foreach (Sprite obj in Objects)
            {
                // make this check if the sprite is in the room
                if (obj is Tile && obj.X >= X * ROOM_WIDTH && obj.Y >= Y * ROOM_HEIGHT && obj.X < (X + 1) * ROOM_WIDTH && obj.Y < (Y + 1) * ROOM_HEIGHT && obj.X % 8 == 0 && obj.Y % 8 == 0)
                {
                    stuff.Add(obj);
                }
            }
            stuff.Sort((s1, s2) => {
                int r = s1.X.CompareTo(s2.X);
                if (r == 0)
                {
                    r =  s1.Y.CompareTo(s2.Y);
                    if (r == 0)
                    {
                        return (s1.Layer == -2).CompareTo(s2.Layer == -2);
                    }
                    else
                        return r;
                }
                else
                    return r;
            });
            int x = 0;
            int y = 0;
            int index = 0;
            int empty = 0;
            while (x < ROOM_WIDTH / 8)
            {
                if (stuff.Count > index)
                {
                    Tile tile = stuff[index] as Tile;
                    if (tile.X % ROOM_WIDTH == x * 8 && tile.Y % ROOM_HEIGHT == y * 8)
                    {
                        if (tile.Layer == -2)
                        {
                            if (empty > 1)
                            {
                                tiles.Add("-3");
                                tiles.Add(empty.ToString());
                            }
                            else if (empty == 1)
                            {
                                tiles.Add("-2");
                            }
                            empty = 0;
                            tiles.Add(tile.TextureX.ToString());
                            tiles.Add(tile.TextureY.ToString());
                            tiles.Add(tile.Tag);
                            index += 1;
                            y += 1;
                            if (y >= ROOM_HEIGHT / 8)
                            {
                                y = 0;
                                x += 1;
                            }
                            if (TileTexture == null)
                                TileTexture = tile.Texture;
                        }
                        else
                        {
                            if (empty > 1)
                            {
                                tiles.Add("-3");
                                tiles.Add(empty.ToString());
                            }
                            else if (empty == 1)
                            {
                                tiles.Add("-2");
                            }
                            empty = 0;
                            tiles.Add("-1");
                            tiles.Add(tile.Layer.ToString());
                            tiles.Add(tile.TextureX.ToString());
                            tiles.Add(tile.TextureY.ToString());
                            tiles.Add(tile.Tag);
                            index += 1;
                        }
                    }
                    else
                    {
                        empty += 1;
                        y += 1;
                        if (y >= ROOM_HEIGHT / 8)
                        {
                            y = 0;
                            x += 1;
                        }
                    }
                }
                else
                {
                    empty += 1;
                    y += 1;
                    if (y >= ROOM_HEIGHT / 8)
                    {
                        y = 0;
                        x += 1;
                    }
                }
            }
            ret.Add("Tiles", new JArray(tiles.ToArray()));
            ret.Add("TileTexture", TileTexture?.Name);
            if (Tags.Count > 0)
            {
                JObject tags = new JObject();
                for (int i = 0; i < Tags.Count; i++)
                {
                    tags.Add(Tags.Keys[i], Tags.Values[i]);
                }
                ret.Add("Tags", tags);
            }
            return ret;
        }
        public static Room LoadRoom(JToken loadFrom, Game game, int xOffset = 0, int yOffset = 0)
        {
            JArray sArr = loadFrom["Objects"] as JArray;
            Room ret = new Room(new SpriteCollection(), null, null);
            ret.X = (int)loadFrom["X"];
            ret.Y = (int)loadFrom["Y"];
            ret.Name = (string)loadFrom["Name"] ?? "";
            System.Drawing.Color c = System.Drawing.Color.FromArgb((int)(loadFrom["Color"] ?? -1));
            ret.Color = Color.FromArgb(c.A, c.R, c.G, c.B);
            ret.GroupName = (string)loadFrom["GroupName"];
            ret.BG = loadFrom["BG"];
            ret.PresetName = (string)loadFrom["PresetName"];
            int? rrx = (int?)loadFrom["RightX"];
            int? rry = (int?)loadFrom["RightY"];
            int? rlx = (int?)loadFrom["LeftX"];
            int? rly = (int?)loadFrom["LeftY"];
            int? rdx = (int?)loadFrom["DownX"];
            int? rdy = (int?)loadFrom["DownY"];
            int? rux = (int?)loadFrom["UpX"];
            int? ruy = (int?)loadFrom["UpY"];
            if (rrx.HasValue) ret.RoomRight = new Point(rrx.Value, rry.Value);
            if (rlx.HasValue) ret.RoomLeft = new Point(rlx.Value, rly.Value);
            if (rdx.HasValue) ret.RoomDown = new Point(rdx.Value, rdy.Value);
            if (rux.HasValue) ret.RoomUp = new Point(rux.Value, ruy.Value);
            if (ret.GroupName is object)
            {
                AutoTileSettings.PresetGroup preset = game.RoomPresets[ret.GroupName];
                if (ret.BG is null)
                {
                    ret.BG = game.GetBackground(preset.Background);
                }
                string gName = (string)loadFrom["GroundName"];
                if (gName is null)
                    ret.Ground = AutoTileSettings.Load(loadFrom["Ground"]) ?? AutoTileSettings.Default13(3, 2);
                else
                    ret.Ground = preset.GetPreset(gName).Ground.Initialize();
                string bName = (string)loadFrom["BackgroundName"];
                if (bName is null)
                    ret.Background = AutoTileSettings.Load(loadFrom["Background"]) ?? AutoTileSettings.Default13(0, 17);
                else
                    ret.Background = preset.GetPreset(bName).Background.Initialize();
                string sName = (string)loadFrom["SpikesName"];
                if (sName is null)
                    ret.Spikes = AutoTileSettings.Load(loadFrom["Spikes"]) ?? AutoTileSettings.Default4(8, 0);
                else
                    ret.Spikes = preset.GetPreset(sName).Spikes.Initialize();
            }
            TileTexture texture = game.TextureFromName((string)loadFrom["TileTexture"]) as TileTexture;
            
            foreach (JToken sprite in sArr)
            {
                Sprite s = Sprite.LoadSprite(sprite, game);
                if (s is null) continue;
                if (s != game.ActivePlayer || game.CurrentState != Game.GameStates.Playing)
                {
                    s.InitialX += ret.X * ROOM_WIDTH + xOffset;
                    s.InitialY += ret.Y * ROOM_HEIGHT + yOffset;
                    s.PreviousX = s.DX;
                    s.PreviousY = s.DY;
                }
                if (s is Checkpoint && game.CurrentState == Game.GameStates.Playing)
                {
                    bool activate = false;
                    foreach (Crewman crewman in game.UserAccessSprites.Values.Where((sp) => sp is Crewman))
                    {
                        if (crewman.CurrentCheckpoint is object && crewman.CurrentCheckpoint.InitialX == s.InitialX && crewman.CurrentCheckpoint.InitialY == s.InitialY)
                        {
                            (s as Checkpoint).RegisterCrewman(crewman);
                            activate = true;
                            crewman.CurrentCheckpoint = s as Checkpoint;
                        }
                    }
                    if (activate)
                    {
                        (s as Checkpoint).Activate(false);
                    }
                }
                ret.Objects.Add(s);
                if (s is IPlatform)
                {
                    IPlatform ip = s as IPlatform;
                    if (ip.OnTop.Count > 0)
                    {
                        foreach (Sprite sprite1 in ip.OnTop)
                        {
                            if (!ret.Objects.Contains(sprite1))
                                ret.Objects.Add(sprite1);
                        }
                    }
                }
            }
            JArray tArr = loadFrom["Tiles"] as JArray;
            if (tArr != null && texture != null)
            {
                ret.TileTexture = texture;
                int x = 0;
                int y = 0;
                for (int i = 0; i < tArr.Count; )
                {
                    int l = (int)tArr[i++];
                    if (l == -1)
                    {
                        int layer = (int)tArr[i++];
                        Tile layeredTile = new Tile((int)ret.GetX + x * 8, (int)ret.GetY + y * 8, texture, (int)tArr[i++], (int)tArr[i++]);
                        layeredTile.Layer = layer;
                        layeredTile.Tag = (string)tArr[i++];
                        ret.Objects.Add(layeredTile);
                    }
                    else if (l == -2)
                    {
                        y += 1;
                        if (y >= ROOM_HEIGHT / 8)
                        {
                            y = 0;
                            x += 1;
                        }
                    }
                    else if (l == -3)
                    {
                        int empty = (int)tArr[i++];
                        for (int j = 0; j < empty; j++)
                        {
                            y += 1;
                            if (y >= ROOM_HEIGHT / 8)
                            {
                                y = 0;
                                x += 1;
                            }
                        }
                    }
                    else
                    {
                        i -= 1;
                        Tile tile = new Tile(x * 8 + (ret.X * ROOM_WIDTH), y * 8 + (ret.Y * ROOM_HEIGHT), texture, (int)tArr[i++], (int)tArr[i++]);
                        tile.Tag = (string)tArr[i++];
                        ret.Objects.Add(tile);
                        y += 1;
                        if (y >= ROOM_HEIGHT / 8)
                        {
                            y = 0;
                            x += 1;
                        }
                    }
                }
            }
            ret.EnterScript = game.ScriptFromName((string)loadFrom["EnterScript"]) ?? Script.Empty;
            ret.ExitScript = game.ScriptFromName((string)loadFrom["ExitScript"]) ?? Script.Empty;
            JObject tags = (JObject)loadFrom["Tags"];
            if (tags is object)
            {
                foreach (JProperty property in tags.Properties())
                {
                    ret.Tags.Add(property.Name, (float)property.Value);
                }
            }
            return ret;
        }

        public static List<Tile> GetTiles(JObject loadFrom, Game game)
        {
            if (loadFrom is null) return new List<Tile>();
            List<Tile> ret = new List<Tile>();
            TileTexture texture = game.TextureFromName((string)loadFrom["TileTexture"]) as TileTexture;
            JArray tArr = loadFrom["Tiles"] as JArray;
            if (tArr != null)
            {
                int x = 0;
                int y = 0;
                for (int i = 0; i < tArr.Count;)
                {
                    int l = (int)tArr[i++];
                    if (l == -1)
                    {
                        int layer = (int)tArr[i++];
                        Tile layeredTile = new Tile(x * 8, y * 8, texture, (int)tArr[i++], (int)tArr[i++]);
                        layeredTile.Layer = layer;
                        layeredTile.Tag = (string)tArr[i++];
                        ret.Add(layeredTile);
                    }
                    else if (l == -2)
                    {
                        y += 1;
                        if (y >= ROOM_HEIGHT / 8)
                        {
                            y = 0;
                            x += 1;
                        }
                    }
                    else if (l == -3)
                    {
                        int empty = (int)tArr[i++];
                        for (int j = 0; j < empty; j++)
                        {
                            y += 1;
                            if (y >= ROOM_HEIGHT / 8)
                            {
                                y = 0;
                                x += 1;
                            }
                        }
                    }
                    else
                    {
                        i -= 1;
                        Tile tile = new Tile(x * 8, y * 8, texture, (int)tArr[i++], (int)tArr[i++]);
                        tile.Tag = (string)tArr[i++];
                        ret.Add(tile);
                        y += 1;
                        if (y >= ROOM_HEIGHT / 8)
                        {
                            y = 0;
                            x += 1;
                        }
                    }
                }
            }
            return ret;
        }

        public void AddRoom(JToken loadFrom, Game game, int x, int y)
        {
            if (x < 0)
            {
                X += x;
                width -= x;
                x = 0;
            }
            else if (x >= width)
                width += x - width + 1;
            if (y < 0)
            {
                Y += y;
                height -= y;
                y = 0;
            }
            else if (y >= height)
                height += y - height + 1;
            JArray sArr = loadFrom["Objects"] as JArray;
            x *= ROOM_WIDTH;
            y *= ROOM_HEIGHT;
            foreach (JToken sprite in sArr)
            {
                Sprite s = Sprite.LoadSprite(sprite, game);
                if (s is null) continue;
                s.InitialX += X * ROOM_WIDTH + x;
                s.InitialY += Y * ROOM_HEIGHT + y;
                s.PreviousX = s.DX;
                s.PreviousY = s.DY;
                if (s != null)
                    Objects.Add(s);
                if (game.CurrentState == Game.GameStates.Playing && s is Checkpoint && game.ActivePlayer.CurrentCheckpoint != null && s.X == game.ActivePlayer.CurrentCheckpoint.X && s.Y == game.ActivePlayer.CurrentCheckpoint.Y)
                {
                    (s as Checkpoint).Activate(false);
                    game.ActivePlayer.CurrentCheckpoint = s as Checkpoint;
                }
            }
            JArray tArr = loadFrom["Tiles"] as JArray;
            if (tArr != null && TileTexture != null)
            {
                int cx = 0;
                int cy = 0;
                for (int i = 0; i < tArr.Count; )
                {
                    int l = (int)tArr[i++];
                    if (l == -1)
                    {
                        int layer = (int)tArr[i++];
                        Tile layeredTile = new Tile((int)GetX + cx * 8 + x, (int)GetY + cy * 8 + y, TileTexture, (int)tArr[i++], (int)tArr[i++]);
                        layeredTile.Layer = layer;
                        layeredTile.Tag = (string)tArr[i++];
                        Objects.Add(layeredTile);
                    }
                    else if (l == -2)
                    {
                        cy += 1;
                        if (cy >= ROOM_HEIGHT / 8)
                        {
                            cy = 0;
                            cx += 1;
                        }
                    }
                    else if (l == -3)
                    {
                        int empty = (int)tArr[i++];
                        for (int j = 0; j < empty; j++)
                        {
                            cy += 1;
                            if (cy >= ROOM_HEIGHT / 8)
                            {
                                cy = 0;
                                cx += 1;
                            }
                        }
                    }
                    else
                    {
                        i -= 1;
                        Tile tile = new Tile(cx * 8 + (X * ROOM_WIDTH) + x, cy * 8 + (Y * ROOM_HEIGHT) + y, TileTexture, (int)tArr[i++], (int)tArr[i++]);
                        tile.Tag = (string)tArr[i++];
                        Objects.Add(tile);
                        cy += 1;
                        if (cy >= ROOM_HEIGHT / 8)
                        {
                            cy = 0;
                            cx += 1;
                        }
                    }
                }
            }
        }
    }
}
