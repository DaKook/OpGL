using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Drawing;

namespace V7
{
    class ContextMenu : SpritesLayer
    {
        public SpriteCollection Sprites;

        private RectangleSprite bg;
        private RectangleSprite select;
        private StringDrawable choices;
        private List<VMenuItem> items;
        private int lineSize;
        private bool click;

        public ContextMenu(float x, float y, IEnumerable<VMenuItem> items, Game game)
        {
            Owner = game;
            this.items = new List<VMenuItem>();
            StringBuilder sb = new StringBuilder();
            foreach (VMenuItem item in items)
            {
                this.items.Add(item);
                sb.Append(item.Text);
                sb.Append('\n');
            }
            sb.Remove(sb.Length - 1, 1);
            choices = new StringDrawable(x + 2, y + 2, game.NonMonoFont, sb.ToString()) { Layer = 2 };
            bg = new RectangleSprite(x, y, choices.Width + 4, choices.Height + 4) { Layer = 0 };
            if (bg.Bottom > Game.RESOLUTION_HEIGHT)
            {
                bg.Bottom = bg.Y;
                if (bg.Y < 0)
                {
                    bg.Y = 0;
                }
            }
            choices.Y = bg.Y + 2;
            if (bg.Right > Game.RESOLUTION_WIDTH)
            {
                bg.Right = bg.X;
                if (bg.X < 0)
                {
                    bg.X = 0;
                }
            }
            choices.X = bg.X + 2;
            lineSize = choices.Texture.TileSizeY;
            select = new RectangleSprite(0, 0, choices.Width + 2, lineSize) { Layer = 1 };
            bg.Color = Color.FromArgb(255, 100, 100, 100);
            select.Color = Color.FromArgb(255, 0, 0, 127);
            select.Visible = false;
            Sprites = new SpriteCollection();
            Sprites.Add(bg);
            Sprites.Add(choices);
            Sprites.Add(select);
        }

        public override void Dispose()
        {
            Sprites?.Dispose();
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                Owner.ReleaseLeftMouse();
                click = true;
            }
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (e.Key == Keys.Escape)
                FinishLayer();
        }

        public override void HandleWheel(int e)
        {
            // Do nothing
        }

        public override void Process()
        {
            int sel = (int)(Owner.MouseY - choices.Y) / lineSize;
            if (choices.Within(Owner.MouseX, Owner.MouseY, 1, 1) && sel >= 0 && sel < items.Count)
            {
                select.Visible = true;
                select.X = bg.X + 1;
                select.Y = bg.Y + 2 + sel * lineSize;
            }
            else
            {
                select.Visible = false;
            }
            if (click)
            {
                FinishLayer();
                if (select.Visible)
                {
                    items[sel].Action();
                }
            }
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            Sprites?.Render(Owner.FrameCount);
        }
    }
}
