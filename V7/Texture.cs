using OpenTK.Graphics.OpenGL;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;

namespace V7
{
    public class Texture
    {
        public SortedList<string,Animation> Animations { get; set; }
        public string Name { get; private set; }
        public int ID { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public int TileSizeX { get; set; }
        public int TileSizeY { get; set; }
        public TextureProgram Program { get; private set; }
        public int baseVAO { get; private set; }
        public int baseVBO { get; private set; }
        public Matrix4 BaseTexMatrix { get; private set; }
        public int Updated = 0;

        public bool IsOriginal = true;
        public bool HasDataFile = true;

        public Animation AnimationFromName(string name)
        {
            if (Animations is null) return null;
            if (Animations.TryGetValue(name, out Animation anim))
                return anim;
            else if (int.TryParse(name ?? "", out int index) && index > -1 && index < Animations.Count)
                return Animations.Values[index];
            else
                return null;
        }

        public void Update(float width, float height, int tileSize, int tileSize2, int vao, int vbo, int id)
        {
            Width = width;
            Height = height;
            TileSizeX = tileSize;
            TileSizeY = tileSize2;
            baseVAO = vao;
            baseVBO = vbo;
            //ID = id;
            BaseTexMatrix = Matrix4.CreateScale(tileSize / width, tileSize2 / height, 1f);
            Updated += 1;
        }

        public Texture(int id, float width, float height, int tileSize, int tileSize2, string name, TextureProgram program, int vao, int vbo)
        {
            ID = id;
            Width = width;
            Height = height;
            TileSizeX = tileSize;
            TileSizeY = tileSize2;
            Name = name;
            Program = program;
            baseVAO = vao;
            baseVBO = vbo;
            BaseTexMatrix = Matrix4.CreateScale(tileSize / width, tileSize2 / height, 1f);
        }

        public virtual JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("GridSize", TileSizeX);
            if (TileSizeY != TileSizeX)
                ret.Add("GridSize2", TileSizeY);
            if (Animations is object && Animations.Count > 0)
            {
                JArray animations = new JArray();
                for (int i = 0; i < Animations.Count; i++)
                {
                    animations.Add(Animations.Values[i].Save());
                }
                ret.Add(animations);
            }
            return ret;
        }

        public override string ToString()
        {
            return Name;
        }

        public virtual Texture Clone()
        {
            Texture ret = new Texture(ID, Width, Height, TileSizeX, TileSizeY, Name, Program, baseVAO, baseVBO);
            ret.Animations = new SortedList<string, Animation>();
            for (int i = 0; i < Animations.Count; i++)
            {
                Animation a = Animations.Values[i];
                ret.Animations.Add(a.Name, a.Clone(ret));
            }
            ret.IsOriginal = false;
            ret.HasDataFile = HasDataFile;
            return ret;
        }

        public static void LoadTextures(IEnumerable<string> files, Game game)
        {
            foreach (string file in files)
            {
                if (file.EndsWith(".png"))
                {
                    bool original = file.StartsWith("textures");
                    string fName = file.Split(new char[] { '/', '\\' }).Last();
                    fName = fName.Substring(0, fName.Length - 4);

                    string dataPath = file.Substring(0, file.Length - 4) + "_data.txt";
                    if (System.IO.File.Exists(dataPath))
                    {
                        JObject jObject = JObject.Parse(System.IO.File.ReadAllText(dataPath));
                        int gridSize = (int)(jObject["GridSize"] ?? 32);
                        int gridSize2;
                        if (jObject.ContainsKey("GridSize2"))
                            gridSize2 = (int)jObject["GridSize2"];
                        else
                            gridSize2 = gridSize;
                        string type = (string)jObject["Type"] ?? "";
                        Texture tex = CreateTexture(fName, file, gridSize, gridSize2, type, game);
                        tex.IsOriginal = original;
                        if (!game.Textures.ContainsKey(fName))
                            game.Textures.Add(tex.Name, tex);

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
                                JArray eHitboxes = (JArray)anim["Hitboxes"];
                                if (eHitboxes is object)
                                {
                                    animation.ExtraHitboxes = new Rectangle[eHitboxes.Count / 4];
                                    for (int j = 0; j < eHitboxes.Count - 3; j += 4)
                                    {
                                        r = new Rectangle((int)eHitboxes[j], (int)eHitboxes[j + 1], (int)eHitboxes[j + 2], (int)eHitboxes[j + 3]);
                                        animation.ExtraHitboxes[j / 4] = r;
                                    }
                                }
                                JArray hbFrames = (JArray)anim["FrameHitboxes"];
                                if (hbFrames is object)
                                {
                                    int hbi = 0;
                                    List<int[]> hbf = new List<int[]>();
                                    while (hbi < hbFrames.Count)
                                    {
                                        int[] add = new int[(int)hbFrames[hbi++]];
                                        for (int hb = 0; hb < add.Length; hb++)
                                        {
                                            add[hb] = (int)hbFrames[hbi++];
                                        }
                                        hbf.Add(add);
                                    }
                                    animation.FrameHitboxes = hbf.ToArray();
                                }
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

                        if (tex is CrewmanTexture)
                        {
                            //TextBox
                            System.Drawing.Color tbc = System.Drawing.Color.FromArgb((int)(jObject["TextBox"] ?? -1));
                            (tex as CrewmanTexture).TextBoxColor = Color.FromArgb(tbc.A, tbc.R, tbc.G, tbc.B);

                            //Squeak
                            (tex as CrewmanTexture).Squeak = (string)jObject["Squeak"] ?? "";
                        }

                        if (tex is TileTexture)
                        {
                            // Tiles
                            JArray tls = (JArray)jObject["Tiles"];
                            if (tls != null)
                            {
                                int[,] states = new int[(int)(tex.Width / tex.TileSizeX), (int)(tex.Height / tex.TileSizeY)];
                                int i = 0;
                                int x = 0;
                                int y = 0;
                                while (i < tls.Count && y < states.GetLength(1))
                                {
                                    int count = (int)tls[i];
                                    for (int j = 0; j < count; j++)
                                    {
                                        if (y >= states.GetLength(1))
                                            break;
                                        states[x, y] = (int)tls[i + 1];
                                        x += 1;
                                        if (x >= states.GetLength(0))
                                        {
                                            x = 0;
                                            y += 1;
                                        }
                                    }
                                    i += 2;
                                }
                                (tex as TileTexture).TileSolidStates = states;
                            }
                            else
                                (tex as TileTexture).TileSolidStates = new int[0, 0];

                            //Auto Tiles
                            JArray aut = (JArray)jObject["RoomPresets"];
                            if (aut != null)
                            {
                                foreach (JToken group in aut)
                                {
                                    string groupName = (string)group["GroupName"];
                                    string backgroundName = (string)group["Background"];
                                    if (!game.RoomPresets.ContainsKey(groupName))
                                        game.RoomPresets.Add(groupName, new AutoTileSettings.PresetGroup(groupName, backgroundName));
                                    (tex as TileTexture).AddGroup(groupName, backgroundName);
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
                                                Color.FromArgb(255, r, g, b), tex as TileTexture);
                                            preset.Name = name;
                                            game.RoomPresets[groupName].Add(preset);
                                            (tex as TileTexture).GetPresetList((tex as TileTexture).GroupCount - 1).Add(preset);
                                        }
                                    }
                                }
                            }
                        }

                        if (tex is FontTexture)
                        {
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
                                (tex as FontTexture).CharacterWidths = characters;
                            }
                            else
                                (tex as FontTexture).CharacterWidths = new SortedList<int, int>();
                        }
                    }
                    else // no _data file, create with default grid size
                    {
                        Texture newTex = CreateTexture(fName, file, 32, 32, "", game);
                        if (!game.Textures.ContainsKey(fName))
                            game.Textures.Add(newTex.Name, newTex);
                        newTex.IsOriginal = original;
                        newTex.HasDataFile = false;
                        newTex.Animations = new SortedList<string, Animation>();
                    }
                }
            }
        }

        private static Texture CreateTexture(string texture, string fullPath, int gridSize, int gridSize2, string type = "", Game game = null)
        {
            SkiaSharp.SKBitmap im = SkiaSharp.SKBitmap.Decode(fullPath);
            SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.Decode(fullPath, new SkiaSharp.SKImageInfo(im.Width, im.Height, im.ColorType, SkiaSharp.SKAlphaType.Unpremul));
            im.Dispose();


            //System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(fullPath);
            //var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Texture currentTex = game?.TextureFromName(texture);
            int texa;
            int texb;
            if (currentTex is null || currentTex.TileSizeX != gridSize || currentTex.TileSizeY != gridSize2 || bmp.Width != currentTex.Width || bmp.Height != currentTex.Height)
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
            if (currentTex is object)
                GL.DeleteTexture(currentTex.ID);
            GL.CreateTextures(TextureTarget.Texture2D, 1, out tex);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmp.GetPixels());

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

            //bmp.UnlockBits(data);

            if (currentTex is null)
            {
                switch (type)
                {
                    case "Tiles":
                        return new TileTexture(tex, bmp.Width, bmp.Height, gridSize, gridSize2, texture, game.ProgramID, texa, texb);
                    case "Font":
                        return new FontTexture(tex, bmp.Width, bmp.Height, gridSize, gridSize2, texture, game.ProgramID, texa, texb);
                    case "Crewman":
                        return new CrewmanTexture(tex, bmp.Width, bmp.Height, gridSize, gridSize2, texture, game.ProgramID, texa, texb);
                    default:
                        return new Texture(tex, bmp.Width, bmp.Height, gridSize, gridSize2, texture, game.ProgramID, texa, texb);
                }
            }
            else
            {
                currentTex.Update(bmp.Width, bmp.Height, gridSize, gridSize2, texa, texb, tex);
                return currentTex;
            }
        }
    }

    public class TileTexture : Texture
    {
        public int[,] TileSolidStates { get; set; }
        private List<List<AutoTileSettings.RoomPreset>> presets = new List<List<AutoTileSettings.RoomPreset>>();
        private List<string> names = new List<string>();
        private List<string> bgs = new List<string>();

        public void AddGroup(string name, string bg)
        {
            names.Add(name);
            bgs.Add(bg);
            presets.Add(new List<AutoTileSettings.RoomPreset>());
        }
        public void RemoveGroup(int index)
        {
            names.RemoveAt(index);
            bgs.RemoveAt(index);
            presets.RemoveAt(index);
        }
        public AutoTileSettings.PresetGroup GetPresetGroup(int index)
        {
            AutoTileSettings.PresetGroup group = new AutoTileSettings.PresetGroup(names[index], bgs[index]);
            group.AddRange(presets[index]);
            return group;
        }
        public string GetName(int index) => names[index];
        public string GetBackground(int index) => bgs[index];
        public List<AutoTileSettings.RoomPreset> GetPresetList(int index) => presets[index];
        public int GroupCount => presets.Count;

        public TileTexture(int id, float width, float height, int tileSize, int tileSize2, string name, TextureProgram program, int vao, int vbo) : base(id, width, height, tileSize, tileSize2, name, program, vao, vbo)
        {

        }
        public override JObject Save()
        {
            JObject ret = base.Save();
            if (TileSolidStates is object)
            {
                JArray tiles = new JArray();
                int count = 0;
                int current = -1;
                for (int y = 0; y < TileSolidStates.GetLength(1); y++)
                {
                    for (int x = 0; x < TileSolidStates.GetLength(0); x++)
                    {
                        if (count == 0)
                        {
                            count++;
                            current = TileSolidStates[x, y];
                        }
                        else
                        {
                            if (TileSolidStates[x, y] != current)
                            {
                                tiles.Add(count);
                                tiles.Add(current);
                                count = 1;
                                current = TileSolidStates[x, y];
                            }
                            else
                                count++;
                        }
                    }
                }
                ret.Add("Tiles", tiles);
            }
            if (presets is object && presets.Count > 0)
            {
                JArray presets = new JArray();
                for (int i = 0; i < this.presets.Count; i++)
                {
                    List<AutoTileSettings.RoomPreset> group = this.presets[i];
                    if (group.Count == 0) continue;
                    JObject preset = new JObject();
                    preset.Add("GroupName", names[i]);
                    preset.Add("Background", bgs[i]);
                    AutoTileSettings.RoomPreset p = group[0];
                    preset.Add("GroundSize", p.Ground.Size);
                    if (p.Ground.Size2 != new Point(1, 1))
                        preset.Add("GroundSize2", new JArray() { p.Ground.Size2.X, p.Ground.Size2.Y });
                    preset.Add("BackgroundSize", p.Background.Size);
                    if (p.Background.Size2 != new Point(1, 1))
                        preset.Add("BackgroundSize2", new JArray() { p.Background.Size2.X, p.Background.Size2.Y });
                    preset.Add("SpikesSize", p.Spikes.Size);
                    JArray contents = new JArray();
                    for (int j = 0; j < group.Count; j++)
                    {
                        p = group[j];
                        contents.Add(p.Name);
                        contents.Add(p.Ground.Origin.X);
                        contents.Add(p.Ground.Origin.Y);
                        contents.Add(p.Background.Origin.X);
                        contents.Add(p.Background.Origin.Y);
                        contents.Add(p.Spikes.Origin.X);
                        contents.Add(p.Spikes.Origin.Y);
                        contents.Add(p.Color.R);
                        contents.Add(p.Color.G);
                        contents.Add(p.Color.B);
                    }
                    preset.Add("Contents", contents);
                    presets.Add(preset);
                }
                ret.Add("RoomPresets", presets);
            }
            ret.Add("Type", "Tiles");
            return ret;
        }

        public override Texture Clone()
        {
            TileTexture ret = new TileTexture(ID, Width, Height, TileSizeX, TileSizeY, Name, Program, baseVAO, baseVBO);
            ret.Animations = new SortedList<string, Animation>();
            for (int i = 0; i < Animations.Count; i++)
            {
                Animation a = Animations.Values[i];
                ret.Animations.Add(a.Name, a.Clone(ret));
            }
            ret.IsOriginal = false;
            ret.HasDataFile = HasDataFile;
            int[,] states = TileSolidStates.Clone() as int[,];
            ret.TileSolidStates = states;
            ret.presets = new List<List<AutoTileSettings.RoomPreset>>();
            for (int i = 0; i < presets.Count; i++)
            {
                List<AutoTileSettings.RoomPreset> group = presets[i];
                List<AutoTileSettings.RoomPreset> newGroup = new List<AutoTileSettings.RoomPreset>();
                ret.presets.Add(newGroup);
                newGroup.AddRange(group);
            }
            return ret;
        }
    }

    public class FontTexture : Texture
    {
        public SortedList<int, int> CharacterWidths { get; set; }
        public SortedList<uint, uint> CharacterList { get; set; }
        public FontTexture(int id, float width, float height, int tileSize, int tileSize2, string name, TextureProgram program, int vao, int vbo) : base(id, width, height, tileSize, tileSize2, name, program, vao, vbo)
        {

        }

        public int GetCharacterWidth(int character)
        {
            if (CharacterWidths != null && CharacterWidths.TryGetValue(character, out int ret))
                return ret;
            else
                return TileSizeX;
        }

        public override JObject Save()
        {
            JObject ret = base.Save();
            if (CharacterWidths is object && CharacterWidths.Count > 0)
            {
                JArray widths = new JArray();
                int count = 0;
                int index = -2;
                int find = -1;
                for (int i = 0; i < CharacterWidths.Count; i++)
                {
                    if (CharacterWidths.Keys[i] != index)
                    {
                        index = CharacterWidths.Keys[i];
                        if (find > -1)
                        {
                            widths[find] = -count;
                        }
                        widths.Add(index);
                        widths.Add(0);
                        find = widths.Count - 1;
                        count = 0;
                    }
                    widths.Add(CharacterWidths.Values[i]);
                    count++;
                    index++;
                }
                ret.Add("Characters", widths);
            }
            if (CharacterList is object && CharacterList.Count > 0)
            {

            }
            ret.Add("Type", "Font");
            return ret;
        }

        public override Texture Clone()
        {
            FontTexture ret = new FontTexture(ID, Width, Height, TileSizeX, TileSizeY, Name, Program, baseVAO, baseVBO);
            ret.Animations = new SortedList<string, Animation>();
            for (int i = 0; i < Animations.Count; i++)
            {
                Animation a = Animations.Values[i];
                ret.Animations.Add(a.Name, a.Clone(ret));
            }
            ret.IsOriginal = false;
            ret.HasDataFile = HasDataFile;
            ret.CharacterWidths = new SortedList<int, int>();
            for (int i = 0; i < CharacterWidths.Count; i++)
            {
                ret.CharacterWidths.Add(CharacterWidths.Keys[i], CharacterWidths.Values[i]);
            }
            ret.CharacterList = new SortedList<uint, uint>();
            for (int i = 0; i < CharacterList.Count; i++)
            {
                ret.CharacterList.Add(CharacterList.Keys[i], CharacterList.Values[i]);
            }
            return ret;
        }
    }

    public class CrewmanTexture : Texture
    {
        public Color TextBoxColor { get; set; }
        public string Squeak = "";
        public CrewmanTexture(int id, float width, float height, int tileSize, int tileSize2, string name, TextureProgram program, int vao, int vbo) : base(id, width, height, tileSize, tileSize2, name, program, vao, vbo)
        {

        }

        public override JObject Save()
        {
            JObject ret = base.Save();
            ret.Add("TextBox", System.Drawing.Color.FromArgb(TextBoxColor.A, TextBoxColor.R, TextBoxColor.G, TextBoxColor.B).ToArgb());
            ret.Add("Squeak", Squeak);
            return ret;
        }

        public override Texture Clone()
        {
            CrewmanTexture ret = new CrewmanTexture(ID, Width, Height, TileSizeX, TileSizeY, Name, Program, baseVAO, baseVBO);
            ret.Animations = new SortedList<string, Animation>();
            for (int i = 0; i < Animations.Count; i++)
            {
                Animation a = Animations.Values[i];
                ret.Animations.Add(a.Name, a.Clone(ret));
            }
            ret.IsOriginal = false;
            ret.HasDataFile = HasDataFile;
            ret.TextBoxColor = TextBoxColor;
            ret.Squeak = Squeak;
            return ret;
        }
    }
}
