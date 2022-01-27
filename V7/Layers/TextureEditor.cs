using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using SkiaSharp;
using System.Text;
using System.Drawing;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace V7
{
    public class TextureEditor : SpritesLayer
    {
        public SpriteCollection Sprites { get; set; }
        public SpriteCollection HudSprites { get; set; }
        public Texture EditingTexture { get; set; }
        public string TexturePath;
        public float CameraX, CameraY, CameraZoom = 1;
        private VTextBox zoomIndicate;
        public FullImage TextureImage;
        public RectangleSprite Selection;
        public bool UpdateSelection = true;
        public SpriteGroup Toolbar;
        //Toolbar
        private RectangleSprite tbBase;
        private VButton fileButton;
        private VButton propertiesButton;
        private VButton animationsButton;

        private Texture lastSave;


        private List<VButton> Buttons = new List<VButton>();
        private VButton SelectedButton;

        private int mx;
        private int my;
        private bool scroll;
        private bool click;

        private Point origin;
        private bool dragging;

        private bool editTiles = false;
        private Sprite[,] tileStates;
        private Sprite tileState;
        private StringDrawable stateName;

        public SizeF SelectionSize;

        private const int WIDTH = Game.RESOLUTION_WIDTH;
        private const int HEIGHT = Game.RESOLUTION_HEIGHT - TOOLBAR_HEIGHT;
        public const int TOOLBAR_HEIGHT = 20;

        public TextureEditor(Game game, Texture texture, string path)
        {
            Owner = game;
            EditingTexture = texture.Clone();
            SelectionSize = new SizeF(EditingTexture.TileSizeX, EditingTexture.TileSizeY);
            lastSave = texture.Clone();
            TexturePath = path;
            Sprites = new SpriteCollection();
            HudSprites = new SpriteCollection();
            TextureImage = new FullImage(0, 0, EditingTexture) { Layer = 0 };
            Selection = new RectangleSprite(0, 0, texture.TileSizeX, texture.TileSizeY) { Color = Color.FromArgb(80, 255, 255, 255), Visible = false, Layer = 2 };
            FreezeBelow = true;
            Darken = 1;
            Sprites.Add(TextureImage);
            Sprites.Add(Selection);
            tbBase = new RectangleSprite(0, 0, WIDTH, TOOLBAR_HEIGHT) { Color = Color.FromArgb(255, 25, 25, 30) };
            fileButton = new VButton(4, 4, Owner.NonMonoFont, 60, TOOLBAR_HEIGHT - 8, "File", Color.LightBlue, 2);
            propertiesButton = new VButton(fileButton.Right + 4, 4, Owner.NonMonoFont, 70, TOOLBAR_HEIGHT - 8, "Properties", Color.LightBlue, 2);
            animationsButton = new VButton(propertiesButton.Right + 4, 4, Owner.NonMonoFont, 70, TOOLBAR_HEIGHT - 8, "Animations", Color.LightBlue, 2);
            Toolbar = new SpriteGroup(tbBase);
            HudSprites.Add(Toolbar);
            Buttons.AddRange(new VButton[] { fileButton, propertiesButton, animationsButton });
            HudSprites.Add(fileButton);
            HudSprites.Add(propertiesButton);
            HudSprites.Add(animationsButton);
            fileButton.OnClick = () => FileButton();
            propertiesButton.OnClick = () => PropertiesButton();
        }

        private void FileButton()
        {
            List<VMenuItem> items = new List<VMenuItem>();
            items.Add(new VMenuItem("Save Data File (Ctrl+S)", () =>
            {
                SaveTexture();
            }));
            items.Add(new VMenuItem("Revert to Last Save (Ctrl+R)", () =>
            {
                RevertTexture();
            }));
            ContextMenu cm = new ContextMenu(fileButton.X, fileButton.Bottom, items, Owner);
            Owner.AddLayer(cm);
        }

        private void PropertiesButton()
        {
            List<VMenuItem> items = new List<VMenuItem>();
            items.Add(new VMenuItem("Set Grid Size (G)", () =>
            {
                SetGridSize();
            }));
            if (EditingTexture is TileTexture)
            {
                if (editTiles)
                {
                    items.Add(new VMenuItem("Exit Tile States Mode", () =>
                    {
                        ExitEditMode();
                    }));
                }
                else
                {
                    items.Add(new VMenuItem("Edit Tile States", () =>
                    {
                        EnterEditMode();
                    }));
                }
                items.Add(new VMenuItem("Edit Auto Tiles (A)", () =>
                {
                    AutoTilesEditor ate = new AutoTilesEditor(Owner, this);
                    Owner.AddLayer(ate);
                }));
            }
            else if (EditingTexture is CrewmanTexture)
            {
                items.Add(new VMenuItem("Set Textbox Color (C)", () =>
                {
                    SetTextboxColor();
                }));
                items.Add(new VMenuItem("Set Squeak (S)", () =>
                {
                    SetSqueak();
                }));
            }
            items.Add(new VMenuItem("Change Texture Type... (T)", () =>
            {
                ChangeType();
            }));
            ContextMenu cm = new ContextMenu(propertiesButton.X, propertiesButton.Bottom, items, Owner);
            Owner.AddLayer(cm);
        }

        public override void Process()
        {
            for (int i = Sprites.Count - 1; i >= 0; i--)
            {
                Sprites[i].Process();
            }
            for (int i = HudSprites.Count - 1; i >= 0; i--)
            {
                HudSprites[i].Process();
            }
            int mouseX = Owner.MouseX;
            int mouseY = Owner.MouseY - TOOLBAR_HEIGHT;
            if (Owner.LeftMouse || scroll || (mouseX >= 0 && mouseX < WIDTH && mouseY >= 0 && mouseY < HEIGHT))
            {
                if (SelectedButton is object && !Owner.LeftMouse)
                {
                    SelectedButton.Unselect();
                    SelectedButton = null;
                }
                Selection.Visible = true;
                Selection.SetSize(SelectionSize.Width, SelectionSize.Height);
                int cx = (int)(mouseX / CameraZoom + CameraX - (SelectionSize.Width - EditingTexture.TileSizeX) / 2) / EditingTexture.TileSizeX;
                if (!dragging || cx < origin.X)
                {
                    Selection.X = cx * EditingTexture.TileSizeX;
                    if (dragging)
                        Selection.SetWidth((origin.X - cx + 1) * EditingTexture.TileSizeX);
                }
                else
                {
                    Selection.X = origin.X * EditingTexture.TileSizeX;
                    Selection.SetWidth((cx - origin.X + 1) * EditingTexture.TileSizeX);
                }
                int cy = (int)(mouseY / CameraZoom + CameraY - (SelectionSize.Height - EditingTexture.TileSizeY) / 2) / EditingTexture.TileSizeY;
                if (!dragging || cy < origin.Y)
                {
                    Selection.Y = cy * EditingTexture.TileSizeY;
                    if (dragging)
                        Selection.SetHeight((origin.Y - cy + 1) * EditingTexture.TileSizeY);
                }
                else
                {
                    Selection.Y = origin.Y * EditingTexture.TileSizeY;
                    Selection.SetHeight((cy - origin.Y + 1) * EditingTexture.TileSizeY);
                }
                if (Owner.MiddleMouse && !scroll)
                {
                    mx = Owner.MouseX;
                    my = Owner.MouseY;
                    scroll = true;
                }
                else if (Owner.MiddleMouse && scroll)
                {
                    int sx = Owner.MouseX - mx;
                    int sy = Owner.MouseY - my;
                    CameraX -= sx / CameraZoom;
                    CameraY -= sy / CameraZoom;
                    if (CameraX < 0 || TextureImage.Width < WIDTH / CameraZoom)
                        CameraX = 0;
                    else if (CameraX > TextureImage.Width - WIDTH / CameraZoom)
                        CameraX = TextureImage.Width - WIDTH / CameraZoom;
                    if (CameraY < 0 || TextureImage.Height < HEIGHT / CameraZoom)
                        CameraY = 0;
                    else if (CameraY > TextureImage.Height - HEIGHT / CameraZoom)
                        CameraY = TextureImage.Height - HEIGHT / CameraZoom;
                    mx = Owner.MouseX;
                    my = Owner.MouseY;
                }
                else
                {
                    scroll = false;
                }
                if (!Owner.LeftMouse && dragging)
                {
                    dragging = false;
                    if (editTiles)
                    {
                        int sx = (int)Selection.X / EditingTexture.TileSizeX;
                        int sy = (int)Selection.Y / EditingTexture.TileSizeY;
                        for (int y = sy; y < sy + (int)Selection.Height / EditingTexture.TileSizeY; y++)
                        {
                            for (int x = sx; x < sx + (int)Selection.Width / EditingTexture.TileSizeX; x++)
                            {
                                ChangeState(x, y, tileState.TextureX);
                            }
                        }
                    }
                }
            }
            else
            {
                Selection.Visible = false;
                bool found = false;
                foreach (VButton btn in Buttons)
                {
                    if (btn.IsTouching(new Point(Owner.MouseX, Owner.MouseY)))
                    {
                        if (SelectedButton is object && !Owner.LeftMouse)
                        {
                            SelectedButton.Unselect();
                        }
                        found = true;
                        SelectedButton = btn;
                        btn.Select();
                        break;
                    }
                }
                if (!found && SelectedButton is object && !Owner.LeftMouse)
                {
                    SelectedButton.Unselect();
                    SelectedButton = null;
                }
            }
            if (click)
            {
                click = false;
                if (SelectedButton is object)
                {
                    SelectedButton.OnClick?.Invoke();
                }
            }
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            Matrix4 cam = baseCamera;
            baseCamera = Matrix4.CreateTranslation(0, TOOLBAR_HEIGHT, 0) * baseCamera;
            baseCamera = Matrix4.CreateScale(CameraZoom, CameraZoom, 1) * baseCamera;
            baseCamera = Matrix4.CreateTranslation(-CameraX, -CameraY, 0) * baseCamera;
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            Sprites.Render(Owner.FrameCount);
            GL.UniformMatrix4(viewMatrixLocation, false, ref cam);
            HudSprites.Render(Owner.FrameCount);
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                click = true;
                if (Selection.Visible && editTiles)
                {
                    origin = new Point((int)Selection.X / EditingTexture.TileSizeX, (int)Selection.Y / EditingTexture.TileSizeY);
                    dragging = true;
                }
            }
        }

        private readonly Keys[] keys = new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0, Keys.Minus };
        private readonly string[] stNames = new string[] { "1 - Solid", "2 - Non-Solid", "3 - Spike (Up)", "4 - Spike (Down)", "5 - Spike (Right)", "6 - Spike (Left)", "7 - One-Way (Up)", "8 - One-Way (Down)", "9 - One-Way (Right)", "0 - One-Way (Left)", "- - Spike (Full-Tile)" };
        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (editTiles)
            {
                int i = Array.IndexOf(keys, e.Key);
                if (i > -1)
                {
                    stateName.Text = stNames[i];
                    tileState.Animation = Animation.Static(i, 1, tileState.Texture);
                    if (!e.Shift && !dragging)
                    {
                        ChangeState((int)Selection.X / EditingTexture.TileSizeX, (int)Selection.Y / EditingTexture.TileSizeY, i);
                    }
                }
            }
        }

        private void ChangeState(int x, int y, int state)
        {
            if (!editTiles || !(EditingTexture is TileTexture) || x < 0 || y < 0 || x >= tileStates.GetLength(0) || y >= tileStates.GetLength(1)) return;
            tileStates[x, y].Animation = Animation.Static(state, 1, tileState.Texture);
            (EditingTexture as TileTexture).TileSolidStates[x, y] = state;
        }

        public override void HandleWheel(int e)
        {
            if (Owner.Control)
            {
                if (e > 0 && CameraZoom < 2f)
                {
                    CameraZoom += 0.125f;
                }
                else if (e < 0 && CameraZoom > 0.125f)
                {
                    CameraZoom -= 0.125f;
                }
                CameraZoom = (float)Math.Round(CameraZoom, 3);
                if (zoomIndicate is null)
                {
                    zoomIndicate = new VTextBox(8, TOOLBAR_HEIGHT + 8, Owner.FontTexture, "", Color.Gray);
                    HudSprites.Add(zoomIndicate);
                }
                string text = (CameraZoom * 100).ToString() + "%";
                while (text.Length < 6)
                    text += " ";
                zoomIndicate.Text = text;
                zoomIndicate.Appear();
                zoomIndicate.frames = 60;
            }
            else if (Owner.Shift)
            {
                if (e < 0)
                {
                    CameraX += 8;
                }
                else if (e > 0)
                {
                    CameraX -= 8;
                }
                if (CameraX < 0 || TextureImage.Width < WIDTH / CameraZoom)
                    CameraX = 0;
                else if (CameraX > TextureImage.Width - WIDTH / CameraZoom)
                    CameraX = TextureImage.Width - WIDTH / CameraZoom;
            }
            else
            {
                if (e < 0)
                {
                    CameraY += 8;
                }
                else if (e > 0)
                {
                    CameraY -= 8;
                }
                if (CameraY < 0 || TextureImage.Height < HEIGHT / CameraZoom)
                    CameraY = 0;
                else if (CameraY > TextureImage.Height - HEIGHT / CameraZoom)
                    CameraY = TextureImage.Height - HEIGHT / CameraZoom;
            }
        }

        public override void Dispose()
        {
            Sprites.Dispose();
            HudSprites.Dispose();
        }

        private void SaveTexture()
        {
            System.IO.File.WriteAllText(TexturePath, Newtonsoft.Json.JsonConvert.SerializeObject(EditingTexture.Save(), Newtonsoft.Json.Formatting.None));
        }
        private void RevertTexture()
        {
            EditingTexture = lastSave.Clone();
            TextureImage.ChangeTexture(EditingTexture);
        }
        private void SetGridSize()
        {
            Dialog d = new Dialog(Owner, "Grid Size (Format: x,y)", EditingTexture.TileSizeX.ToString() + "," + EditingTexture.TileSizeY.ToString(), null, (r, s) =>
            {
                string[] size = s.Split(',', 'x', 'X');
                if (size.Length == 1)
                {
                    if (int.TryParse(size[0], out int x))
                    {
                        EditingTexture.TileSizeX = EditingTexture.TileSizeY = x;
                    }
                }
                else if (size.Length == 2)
                {
                    if (int.TryParse(size[0], out int x) && int.TryParse(size[1], out int y))
                    {
                        EditingTexture.TileSizeX = x;
                        EditingTexture.TileSizeY = y;
                    }
                }
                Selection.SetSize(EditingTexture.TileSizeX, EditingTexture.TileSizeY);
                GL.BindVertexArray(EditingTexture.baseVAO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, EditingTexture.baseVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, 16 * sizeof(float), new float[]
                {
                        0,                        0,                        0, 0,
                        EditingTexture.TileSizeX, 0,                        1, 0,
                        EditingTexture.TileSizeX, EditingTexture.TileSizeY, 1, 1,
                        0,                        EditingTexture.TileSizeY, 0, 1
                }, BufferUsageHint.StaticDraw);
                EditingTexture.Update(EditingTexture.Width, EditingTexture.Height, EditingTexture.TileSizeX, EditingTexture.TileSizeY, EditingTexture.baseVAO, EditingTexture.baseVBO, EditingTexture.ID);
            });
            Owner.AddLayer(d);
        }
        private void ChangeType()
        {
            string type = "None";
            if (EditingTexture is FontTexture) type = "Font";
            else if (EditingTexture is TileTexture) type = "Tiles";
            else if (EditingTexture is CrewmanTexture) type = "Crewman";
            Dialog d = new Dialog(Owner, "Texture type? WARNING! Changing the texture type will delete any properties unique to the current type.", type, new string[] { "None", "Font", "Tiles", "Crewman" }, (r, s) =>
            {
                bool changed;
                switch (s.ToLower())
                {
                    case "none":
                        if (changed = EditingTexture.GetType().Name != "Texture")
                            EditingTexture = new Texture(EditingTexture.ID, EditingTexture.Width, EditingTexture.Height, EditingTexture.TileSizeX, EditingTexture.TileSizeY, EditingTexture.Name, EditingTexture.Program, EditingTexture.baseVAO, EditingTexture.baseVBO);
                        break;
                    case "font":
                        if (changed = !(EditingTexture is FontTexture))
                        {
                            EditingTexture = new FontTexture(EditingTexture.ID, EditingTexture.Width, EditingTexture.Height, EditingTexture.TileSizeX, EditingTexture.TileSizeY, EditingTexture.Name, EditingTexture.Program, EditingTexture.baseVAO, EditingTexture.baseVBO);
                            (EditingTexture as FontTexture).CharacterWidths = new SortedList<int, int>();
                            (EditingTexture as FontTexture).CharacterList = new SortedList<uint, uint>();
                        }
                        break;
                    case "tiles":
                        if (changed = !(EditingTexture is TileTexture))
                        {
                            EditingTexture = new TileTexture(EditingTexture.ID, EditingTexture.Width, EditingTexture.Height, EditingTexture.TileSizeX, EditingTexture.TileSizeY, EditingTexture.Name, EditingTexture.Program, EditingTexture.baseVAO, EditingTexture.baseVBO);
                            (EditingTexture as TileTexture).TileSolidStates = new int[(int)EditingTexture.Width / EditingTexture.TileSizeX, (int)EditingTexture.Height / EditingTexture.TileSizeY];
                        }
                        break;
                    case "crewman":
                        if (changed = !(EditingTexture is CrewmanTexture))
                            EditingTexture = new CrewmanTexture(EditingTexture.ID, EditingTexture.Width, EditingTexture.Height, EditingTexture.TileSizeX, EditingTexture.TileSizeY, EditingTexture.Name, EditingTexture.Program, EditingTexture.baseVAO, EditingTexture.baseVBO);
                        break;
                    default:
                        changed = false;
                        break;
                }
                if (changed)
                {
                    TextureImage.ChangeTexture(EditingTexture);
                }
            });
            Owner.AddLayer(d);
        }
        private void ExitEditMode()
        {
            editTiles = false;
            foreach (Sprite sprite in tileStates)
            {
                Sprites.Remove(sprite);
            }
            tileStates = null;
            HudSprites.Remove(tileState);
            HudSprites.Remove(stateName);
        }
        private void EnterEditMode()
        {
            TileTexture tt = EditingTexture as TileTexture;
            if (tt is null) return;
            editTiles = true;
            tileStates = new Sprite[tt.TileSolidStates.GetLength(0), tt.TileSolidStates.GetLength(1)];
            for (int y = 0; y < tt.TileSolidStates.GetLength(1); y++)
            {
                for (int x = 0; x < tt.TileSolidStates.GetLength(0); x++)
                {
                    Sprite s = new Sprite(x * tt.TileSizeX, y * tt.TileSizeY, Owner.TinyFont, tt.TileSolidStates[x, y], 1);
                    s.Layer = 1;
                    tileStates[x, y] = s;
                    Sprites.Add(s);
                }
            }
            if (tileState is null)
                tileState = new Sprite(animationsButton.Right + 4, animationsButton.Y + 4, Owner.TinyFont, 0, 1) { Layer = 2 };
            if (stateName is null)
                stateName = new StringDrawable(tileState.Right + 2, tileState.Y - 1, Owner.NonMonoFont, "1 - Solid", Color.LightBlue) { Layer = 2 };
            HudSprites.Add(tileState);
            HudSprites.Add(stateName);
        }
        private void SetTextboxColor()
        {
            CrewmanTexture ct = EditingTexture as CrewmanTexture;
            if (ct is null) return;
            string v = "White";
            System.Drawing.Color c = System.Drawing.Color.FromArgb(ct.TextBoxColor.A, ct.TextBoxColor.R, ct.TextBoxColor.G, ct.TextBoxColor.B);
            v = c.Name;
            if (v == "0")
            {
                v = (c.ToArgb()).ToString("X8");
            }
            Dialog d = new Dialog(Owner, "Textbox Color", v, null, (r, s) =>
            {
                if (r)
                {
                    Color? cl = Owner.GetColor(s);
                    string msg = "Invalid color";
                    if (cl.HasValue)
                    {
                        ct.TextBoxColor = cl.Value;
                        msg = "Color updated!";
                    }
                    Owner.GetSound(ct.Squeak ?? "")?.Play();
                    ShowTextBox(msg, ct.TextBoxColor);
                }
            }, 36, 26, true);
            Owner.AddLayer(d);
        }
        private void SetSqueak()
        {
            CrewmanTexture ct = EditingTexture as CrewmanTexture;
            if (ct is null) return;
            List<string> choices = new List<string>(Owner.Sounds.Keys);
            Dialog d = new Dialog(Owner, "Squeak", ct.Squeak, choices.ToArray(), (r, s) =>
            {
                SoundEffect se = Owner.GetSound(s);
                string msg = "Invalid Sound";
                if (se is object)
                {
                    ct.Squeak = s;
                    msg = "Squeak updated!";
                }
                Owner.GetSound(ct.Squeak ?? "")?.Play();
                ShowTextBox(msg, ct.TextBoxColor);
            });
            Owner.AddLayer(d);
        }
        private void ShowTextBox(string msg, Color clr)
        {
            VTextBox tb = new VTextBox(0, 0, Owner.FontTexture, msg, clr);
            tb.Layer = 50;
            tb.frames = 100;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            tb.CenterY = Game.RESOLUTION_HEIGHT / 2;
            HudSprites.Add(tb);
            tb.Disappeared += (b) =>
            {
                HudSprites.Remove(tb);
                tb.Dispose();
            };
            tb.Appear();
        }
    }
}
