using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class TilesTool : EditorTool
    {
        public override string DefaultName => "Tiles";

        public override string DefaultDescription => "Place tiles without using auto tiles.\n" +
            "Use LEFT-CLICK to place tiles, and RIGHT-CLICK to delete tiles.\n" +
            "Use MIDDLE-CLICK to select a tile from the room.\n" +
            "Press TAB to open the tileset. In the tileset:\n" +
            "    Press the ARROWS to scroll, or use the mouse wheel.\n" +
            "    Press TAB again (or ESC) to close the tileset.\n" +
            "Alternatively, you can use WASD to change the selected tile without opening the tileset.\n" +
            "Use Z, X, C, and V for extra brush sizes. You can press the button briefly to lock the brush size.\n" +
            "Hold SHIFT when pressing a brush size key to edit the custom size.\n" +
            "Hold F to enter fill mode, then click to fill a region with the selected tile. You can also use RIGHT-CLICK to delete a region.\n" +
            "Hold SHIFT and click and drag to fill a rectangle with the selected tile. You can also use RIGHT-CLICK to delete a rectangle.\n" +
            "Press L to set the layer. Default layer is -2.";

        public override string DefaultKey => "-";

        public override Keys DefaultKeybind => Keys.Minus;

        public virtual bool IsAuto => false;

        public Size BrushSize;

        public TileTexture Texture;

        public Point Tile;

        public static int Layer;

        // Custom Sizes
        public Size ZSize = new Size(2, 2);
        public Size XSize = new Size(3, 3);
        public Size CSize = new Size(5, 5);
        public Size VSize = new Size(7, 7);
        private Size GetSize(Keys key)
        {
            if (key == Keys.Z) return ZSize;
            else if (key == Keys.X) return XSize;
            else if (key == Keys.C) return CSize;
            else return VSize;
        }

        // Lock Axis ([])
        protected int lockPos;
        protected int lockAxis;

        protected int placingMode = 0;

        // Rectangle
        protected int makingRectangle;
        protected Point rectangleAnchor;

        // Brush Size Timer
        protected long _pressStart;
        protected Keys _pressedKey = Keys.Unknown;

        public override void Process()
        {
            base.Process();
            
            if (shift && makingRectangle == 0)
            {
                color = Color.Cyan;
                size = new Size(1, 1);
                position = centerOn(mouse, size * 8);
                if (left || right)
                {
                    makingRectangle = left ? 1 : 2;
                    rectangleAnchor = position;
                }
            }
            else if (makingRectangle != 0)
            {
                color = Color.Cyan;
                position = rectangleAnchor;
                Point p = centerOn(mouse, new Size(8, 8));
                size.Width = (p.X - position.X) / 8;
                if (size.Width < 1)
                {
                    position.X += size.Width * 8;
                    size.Width = -size.Width + 1;
                }
                else
                    size.Width += 1;
                size.Height = (p.Y - position.Y) / 8;
                if (size.Height < 1)
                {
                    position.Y += size.Height * 8;
                    size.Height = -size.Height + 1;
                }
                else
                    size.Height += 1;
                if (makingRectangle == 1 && !isLeftDown)
                {
                    PlaceTiles();
                    makingRectangle = 0;
                }
                else if (makingRectangle == 2 && !isRightDown)
                {
                    PlaceTiles(true);
                    makingRectangle = 0;
                }
            }
            else
            {
                if (_pressedKey != Keys.Unknown && !key(_pressedKey))
                {
                    if (Owner.FrameCount - _pressStart < 20)
                    {
                        if (_pressedKey != Keys.F)
                        {
                            Size s = BrushSize;
                            BrushSize = GetSize(_pressedKey);
                            if (s == BrushSize)
                                BrushSize = new Size(1, 1);
                        }
                    }
                    _pressedKey = Keys.Unknown;
                }
                if (key(Keys.V))
                    size = VSize;
                else if (key(Keys.C))
                    size = CSize;
                else if (key(Keys.X))
                    size = XSize;
                else if (key(Keys.Z))
                    size = ZSize;
                else
                    size = BrushSize;
                Point p = centerOn(mouse, size * 8);
                if (lockAxis == 0)
                {
                    position = p;
                    if (key(Keys.LeftBracket))
                    {
                        lockAxis = 1;
                        lockPos = p.Y;
                    }
                    else if (key(Keys.RightBracket))
                    {
                        lockAxis = 2;
                        lockPos = p.X;
                    }
                }
                else if (lockAxis == 1)
                {
                    position = new Point(p.X, lockPos);
                    if (!key(Keys.LeftBracket))
                    {
                        lockAxis = 0;
                        lockPos = 0;
                    }
                }
                else if (lockAxis == 2)
                {
                    position = new Point(lockPos, p.Y);
                    if (!key(Keys.RightBracket))
                    {
                        lockAxis = 0;
                        lockPos = 0;
                    }
                }

                HandlePlacement();
            }
            SetHud();
        }

        public override void HandleKey(PassedKeyEvent e)
        {
            if (!e.Shift & _pressedKey == Keys.Unknown && (e.Key == Keys.Z || e.Key == Keys.X || e.Key == Keys.C || e.Key == Keys.V || e.Key == Keys.F))
            {
                _pressStart = Owner.FrameCount;
                _pressedKey = e.Key;
            }
            else if (e.Key == Keys.Tab)
            {
                Parent.ShowTileset(this);
            }

            // Change Tile
            else if (e.Key == Keys.S)
            {
                Tile.Y = (Tile.Y + 1) % ((int)Texture.Height / Texture.TileSizeY);
            }
            else if (e.Key == Keys.W)
            {
                Tile.Y -= 1;
                if (Tile.Y < 0) Tile.Y += (int)Texture.Height / Texture.TileSizeY;
            }
            else if (e.Key == Keys.D)
            {
                Tile.X = (Tile.X + 1) % ((int)Texture.Width / Texture.TileSizeX);
            }
            else if (e.Key == Keys.A)
            {
                Tile.X -= 1;
                if (Tile.X < 0) Tile.X += (int)Texture.Width / Texture.TileSizeX;
            }
            // Change Layer
            else if (e.Key == Keys.L)
            {
                Owner.ShowDialog("Tile Layer", Layer.ToString(), new string[] { }, (r, s) =>
                {
                    if (r && int.TryParse(s, out int l))
                    {
                        Layer = l;
                    }
                });
            }
            else if (e.Shift)
            {
                if (e.Key == Keys.Z || e.Key == Keys.X || e.Key == Keys.C || e.Key == Keys.V)
                {
                    Size s = GetSize(e.Key);
                    Owner.ShowDialog(e.Key.ToString() + " brush size (Width,Height or Width x Height)", s.Width.ToString() + "x" + s.Height.ToString(), new string[] { }, (r, a) =>
                    {
                        if (r)
                        {
                            string[] split = a.Split(',', 'x', 'X');
                            for (int i = 0; i < split.Length; i++)
                            {
                                split[i] = split[i].Trim();
                            }
                            if (split.Length == 0) return;
                            if (!int.TryParse(split[0], out int w)) return;
                            int h;
                            if (split.Length == 1)
                            {
                                h = w;
                            }
                            else
                            {
                                if (!int.TryParse(split[1], out h)) return;
                            }
                            s = new Size(w, h);
                            if (e.Key == Keys.Z) ZSize = s;
                            else if (e.Key == Keys.X) XSize = s;
                            else if (e.Key == Keys.C) CSize = s;
                            else if (e.Key == Keys.V) VSize = s;
                            BrushSize = s;
                        }
                    });
                }
            }
        }

        protected virtual void SetHud()
        {
            PreviewTexture = Texture;
            PreviewPoint = Tile;
            Prompt = "  {" + Tile.X.ToString() + "," + Tile.Y.ToString() + "} (Layer " + Layer.ToString() + ")";
        }

        protected virtual void HandlePlacement()
        {
            if (!key(Keys.F))
            {
                color = Color.Blue;
                if (placingMode == 0)
                {
                    if (left) placingMode = 1;
                    else if (right) placingMode = 2;
                }
                else if (placingMode == 1 && !isLeftDown)
                {
                    if (isRightDown)
                        placingMode = 2;
                    else
                        placingMode = 0;
                }
                else if (placingMode == 2 && !isRightDown)
                {
                    if (isLeftDown)
                        placingMode = 1;
                    else
                        placingMode = 0;
                }
                int pm = placingMode;
                if (pm == 1 && isRightDown)
                    pm = 2;
                else if (pm == 2 && isLeftDown)
                    pm = 1;
                if (pm == 1)
                {
                    PlaceTiles();
                }
                else if (pm == 2)
                {
                    PlaceTiles(true);
                }
                
            }
            else
            {
                placingMode = 0;
                size = new Size(1, 1);
                position = centerOn(mouse, size * 8);
                color = Color.Magenta;
                if (left || right)
                {
                    Fill();
                }
            }
        }

        protected virtual void PlaceTiles(bool delete = false)
        {
            if (delete)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    for (int x = 0; x < size.Width; x++)
                    {
                        Owner.TileTool(Owner.CameraX + position.X + x * 8, Owner.CameraY + position.Y + y * 8, false, Texture, Tile, Layer);
                    }
                }
            }
            else
            {
                for (int y = 0; y < size.Height; y++)
                {
                    for (int x = 0; x < size.Width; x++)
                    {
                        Owner.TileTool(Owner.CameraX + position.X + x * 8, Owner.CameraY + position.Y + y * 8, true, Texture, Tile, Layer);
                    }
                }
            }
        }

        protected virtual void Fill()
        {
            Owner.TileFillTool(Owner.CameraX + position.X, Owner.CameraY + position.Y, left, LevelEditor.Tools.Tiles, null, Tile, Layer, Texture, 't');
        }

        public TilesTool(LevelEditor parent, TileTexture texture) : base(parent)
        {
            BrushSize = new Size(1, 1);
            Texture = texture;
            Tile = new Point(3, 2);
            Layer = -2;
        }

        public override JObject Save()
        {
            JObject ret = getBaseSave(DefaultName);
            ret.Add("Z", new JArray(ZSize.Width, ZSize.Height));
            ret.Add("X", new JArray(XSize.Width, XSize.Height));
            ret.Add("C", new JArray(CSize.Width, CSize.Height));
            ret.Add("V", new JArray(VSize.Width, VSize.Height));
            return ret;
        }
    }
}
