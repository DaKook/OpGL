using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace V7
{
    public class PreviewScreen : SpritesLayer
    {
        public SpriteCollection Sprites;
        public Action<Sprite> OnClick;
        public float Scroll;
        public float MaxScroll;
        public Action<Sprite> OnRightClick;
        public bool FinishOnClick = true;

        private Point cursor;

        public SpriteCollection HudSprites;

        public PreviewScreen(IEnumerable<Sprite> sprites, Action<Sprite> onClick, Game owner)
        {
            Sprites = new SpriteCollection(sprites);
            HudSprites = new SpriteCollection();
            OnClick = onClick;
            Darken = 0.5f;
            Owner = owner;
            FreezeBelow = true;
        }

        public override void Dispose()
        {
            Sprites.Dispose();
            HudSprites.Dispose();
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            // Do nothing
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (e.Key == Keys.Escape)
            {
                FinishLayer();
            }
            else if (e.Key == Keys.W || e.Key == Keys.Up)
            {

            }
        }

        public override void HandleWheel(int e)
        {
            int inc = Owner.Shift ? Game.RESOLUTION_HEIGHT : 10;
            if (e > 0)
            {
                Scroll = Math.Max(Scroll - inc, 0);
            }
            else if (e < 0)
            {
                Scroll = Math.Min(Scroll + inc, MaxScroll);
            }
        }

        public void Close()
        {
            FinishLayer();
        }

        Point mc;
        bool lm = true, rm = true;
        public override void Process()
        {
            Point c = new Point(Owner.MouseX, Owner.MouseY);
            if (mc != c)
                cursor = mc = c;

            for (int i = 0; i < HudSprites.Count; i++)
            {
                HudSprites[i].Process();
            }

            Sprites.SortForCollisions(); 
            bool foundone = false;
            for (int i = 0; i < Sprites.Count; i++)
            {
                Sprite sprite = Sprites[i];
                if (sprite.Name is object && sprite.Name != "")
                {
                    if (sprite.Within(cursor.X, cursor.Y + Scroll, 2, 2) && !foundone)
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
            if (Owner.LeftMouse && !lm)
            {
                lm = true;
                List<Sprite> col = Sprites.GetPotentialColliders(cursor.X, cursor.Y + Scroll, 2, 2).FindAll((s) => s.Name is object && s.Name != "" && s.Color == Color.White);
                if (col.Count > 0)
                {
                    Owner.ReleaseLeftMouse();
                    Action<Sprite> cp = OnClick;
                    if (FinishOnClick)
                        FinishLayer();
                    cp?.Invoke(col[0]);
                }
            }
            else if (!Owner.LeftMouse)
            {
                lm = false;
                if (Owner.RightMouse && !rm)
                {
                    List<Sprite> col = Sprites.GetPotentialColliders(cursor.X, cursor.Y + Scroll, 2, 2).FindAll((s) => s.Name is object && s.Name != "" && s.Color == Color.White);
                    if (col.Count > 0)
                    {
                        Action<Sprite> cp = OnRightClick;
                        cp?.Invoke(col[0]);
                    }
                }
                else if (!Owner.RightMouse)
                    rm = false;
            }
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            Matrix4 cam = Matrix4.CreateTranslation(0, -Scroll, 0) * baseCamera;
            GL.UniformMatrix4(viewMatrixLocation, false, ref cam);
            Sprites.Render(Owner.FrameCount);
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            HudSprites.Render(Owner.FrameCount);
        }
    }
}
