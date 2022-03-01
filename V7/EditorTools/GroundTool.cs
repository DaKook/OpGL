using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class GroundTool : TilesTool
    {
        public override string DefaultName => "Ground";

        public override string DefaultDescription => "Place tiles using basic auto tiles.\n" +
            "Use LEFT-CLICK to place tiles, and RIGHT-CLICK to delete tiles.\n" +
            "Press TAB to open the tileset. In the tileset:\n" +
            "    Press the ARROWS to scroll, or use the mouse wheel.\n" +
            "    Use 1 (3x5), 2 (3x1), 3 (8x6) or 4 (4x1) to change pallette size.\n" +
            "    Press TAB again (or ESC) to close the tileset.\n" +
            "Use Z, X, C, and V for extra brush sizes. You can press the button briefly to lock the brush size.\n" +
            "Hold SHIFT when pressing a brush size key to edit the custom size.\n" +
            "Hold F to enter fill mode, then click to fill a region with the selected tiles. You can also use RIGHT-CLICK to delete a region.\n" +
            "Hold SHIFT and click and drag to fill a rectangle with the selected tiles. You can also use RIGHT-CLICK to delete a rectangle.\n" +
            "Press L to set the layer. Default layer is -2.";

        public override string DefaultKey => "1";

        public override Keys DefaultKeybind => Keys.D1;

        public override bool IsAuto => true;

        protected AutoTileSettings autoTiles => Owner.CurrentRoom?.Ground;

        public GroundTool(LevelEditor parent, TileTexture texture) : base(parent, texture)
        {

        }

        protected override void SetHud()
        {
            PreviewTexture = Texture;
            PreviewPoint = autoTiles.Origin;
            Prompt = "  {" + autoTiles.Origin.X.ToString() + "," + autoTiles.Origin.Y.ToString() + "} Auto Tiles";
        }

        protected override void PlaceTiles(bool delete = false)
        {
            List<PointF> pts = new List<PointF>();
            for (int y = 0; y < size.Height; y++)
            {
                for (int x = 0; x < size.Width; x++)
                {
                    pts.Add(new PointF(Owner.CameraX + position.X + x * 8, Owner.CameraY + position.Y + y * 8));
                }
            }
            Owner.AutoTilesToolMulti(pts, !delete, LevelEditor.Tools.Ground, autoTiles, Layer, Texture, 'g');
        }

        protected override void Fill()
        {
            Owner.TileFillTool(Owner.CameraX + position.X, Owner.CameraY + position.Y, left, LevelEditor.Tools.Ground, autoTiles, Tile, Layer, Texture, 'g');
        }
    }
}
