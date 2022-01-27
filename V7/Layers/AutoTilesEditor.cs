using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using System.Drawing;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace V7
{
    class AutoTilesEditor : SpritesLayer
    {
        public SpriteCollection Sprites;
        public readonly TextureEditor Editor;
        public TileTexture Texture => Editor.EditingTexture as TileTexture;

        private RectangleSprite previewBg;
        private Sprite previewTiles;

        private int group;
        private int preset;
        private int autoTiles = 1;
        private int size;

        public AutoTilesEditor(Game owner, TextureEditor texture)
        {
            Sprites = new SpriteCollection();
            Owner = owner;
            Editor = texture;
            previewBg = new RectangleSprite(0, 0, 0, 0) { Layer = 1, Color = Color.Black };
            previewTiles = new Sprite(0, 0, Texture, 0, 0) { Layer = 2 };
            if (Texture is object && Texture.GroupCount > 0 && Texture.GetPresetList(0).Count > 0)
            {
                SetPreview(0, 0, 1);
            }
            else
            {
                previewBg.Visible = previewTiles.Visible = false;
            }
            Sprites.Add(previewBg);
            Sprites.Add(previewTiles);
        }

        private void SetPreview(int group, int preset, int autoTiles)
        {
            AutoTileSettings.RoomPreset roomPreset = Texture.GetPresetList(group)[preset];
            AutoTileSettings.Initializer initializer;
            switch (autoTiles)
            {
                case 1:
                    initializer = roomPreset.Ground;
                    break;
                case 2:
                    initializer = roomPreset.Background;
                    break;
                case 3:
                    initializer = roomPreset.Spikes;
                    break;
                default:
                    initializer = new AutoTileSettings.Initializer();
                    break;
            }
            previewTiles.Animation = Animation.Static(initializer.Origin.X, initializer.Origin.Y, Texture);
            switch (initializer.Size)
            {
                case 3:
                    previewTiles.ExtendTexture(3, 1);
                    Editor.SelectionSize = new SizeF(3 * Texture.TileSizeX, Texture.TileSizeY);
                    break;
                case 4:
                    previewTiles.ExtendTexture(4, 1);
                    Editor.SelectionSize = new SizeF(4 * Texture.TileSizeX, Texture.TileSizeY);
                    break;
                case 13:
                    previewTiles.ExtendTexture(3, 5);
                    Editor.SelectionSize = new SizeF(3 * Texture.TileSizeX, 5 * Texture.TileSizeY);
                    break;
                case 47:
                    previewTiles.ExtendTexture(8, 6);
                    Editor.SelectionSize = new SizeF(8 * Texture.TileSizeX, 6 * Texture.TileSizeY);
                    break;
                default:
                    previewTiles.ExtendTexture(1, 1);
                    Editor.SelectionSize = new SizeF(Texture.TileSizeX, Texture.TileSizeY);
                    break;
            }
            size = initializer.Size;
            previewBg.SetSize(previewTiles.Width + 4, previewTiles.Height + 4);
            previewBg.Right = Game.RESOLUTION_WIDTH - 4;
            previewBg.Y = TextureEditor.TOOLBAR_HEIGHT + 4;
            previewBg.Color = roomPreset.Color;
            previewTiles.X = previewBg.X + 2;
            previewTiles.Y = previewBg.Y + 2;
            previewBg.Visible = previewTiles.Visible = true;
        }

        public override void Dispose()
        {
            Sprites.Dispose();
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                AutoTileSettings.RoomPreset pre = Texture.GetPresetList(group)[preset];
                Point p = new Point((int)Editor.Selection.X / Texture.TileSizeX, (int)Editor.Selection.Y / Texture.TileSizeY);
                switch (autoTiles)
                {
                    case 1:
                        pre.Ground.Origin = p;
                        break;
                    case 2:
                        pre.Background.Origin = p;
                        break;
                    case 3:
                        pre.Spikes.Origin = p;
                        break;
                    default:
                        break;
                }
                Texture.GetPresetList(group).RemoveAt(preset);
                Texture.GetPresetList(group).Insert(preset, pre);
                SetPreview(group, preset, autoTiles);
            }
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (e.Key == Keys.D1)
            {
                autoTiles = 1;
                SetPreview(group, preset, autoTiles);
            }
            else if (e.Key == Keys.D2)
            {
                autoTiles = 2;
                SetPreview(group, preset, autoTiles);
            }
            else if (e.Key == Keys.D3)
            {
                autoTiles = 3;
                SetPreview(group, preset, autoTiles);
            }
            else if (e.Key == Keys.W)
            {
                group += 1;
                if (group >= Texture.GroupCount)
                    group = 0;
                preset = 0;
                SetPreview(group, preset, autoTiles);
            }
            else if (e.Key == Keys.S)
            {
                group -= 1;
                if (group < 0)
                    group = Texture.GroupCount - 1;
                preset = 0;
                SetPreview(group, preset, autoTiles);
            }
            else if (e.Key == Keys.D)
            {
                preset += 1;
                if (preset >= Texture.GetPresetList(group).Count)
                    preset = 0;
                SetPreview(group, preset, autoTiles);
            }
            else if (e.Key == Keys.A)
            {
                preset -= 1;
                if (preset < 0)
                    preset = Texture.GetPresetList(group).Count - 1;
                SetPreview(group, preset, autoTiles);
            }
            else if (e.Key == Keys.C)
            {
                Color pc = Texture.GetPresetList(group)[preset].Color;
                string v;
                System.Drawing.Color c = System.Drawing.Color.FromArgb(pc.A, pc.R, pc.G, pc.B);
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
                        if (cl.HasValue)
                        {
                            AutoTileSettings.RoomPreset roomPreset = Texture.GetPresetList(group)[preset];
                            Texture.GetPresetList(group).RemoveAt(preset);
                            roomPreset.Color = cl.Value;
                            Texture.GetPresetList(group).Add(roomPreset);
                            SetPreview(group, preset, autoTiles);
                        }
                    }
                }, 36, 26, true);
                Owner.AddLayer(d);
            }
        }

        public override void HandleWheel(int e)
        {
            Editor.HandleWheel(e);
        }

        public override void Process()
        {
            
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            Sprites?.Render(Owner.FrameCount);
        }
    }
}
