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
    class PreviewScreen : SpritesLayer
    {
        public SpriteCollection Sprites;
        public Action<Sprite> OnClick;
        public float Scroll;
        public float MaxScroll;
        public Action<Sprite> OnRightClick;

        public PreviewScreen(IEnumerable<Sprite> sprites, Action<Sprite> onClick, Game owner)
        {
            Sprites = new SpriteCollection(sprites);
            OnClick = onClick;
            Darken = 0.5f;
            Owner = owner;
            FreezeBelow = true;
        }

        public override void Dispose()
        {
            Sprites.Dispose();
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                while (Sprites.IsSorting)
                    ;
                List<Sprite> col = Sprites.GetPotentialColliders(Owner.MouseX, Owner.MouseY + Scroll, 1, 1).FindAll((s) => s.Name is object && s.Name != "" && s.Color == Color.White);
                if (col.Count > 0)
                {
                    Owner.ReleaseLeftMouse();
                    Action<Sprite> cp = OnClick;
                    FinishLayer();
                    cp?.Invoke(col[0]);
                }
            }
            else if (e.Button == MouseButton.Right)
            {
                while (Sprites.IsSorting)
                    ;
                List<Sprite> col = Sprites.GetPotentialColliders(Owner.MouseX, Owner.MouseY + Scroll, 1, 1).FindAll((s) => s.Name is object && s.Name != "" && s.Color == Color.White);
                if (col.Count > 0)
                {
                    Action<Sprite> cp = OnRightClick;
                    cp?.Invoke(col[0]);
                }
            }
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (e.Key == Keys.Escape)
            {
                FinishLayer();
            }
        }

        public override void HandleWheel(int e)
        {
            if (e > 0)
            {
                Scroll = Math.Max(Scroll - 10, 0);
            }
            else if (e < 0)
            {
                Scroll = Math.Min(Scroll + 10, MaxScroll);
            }
        }

        public override void Process()
        {
            Sprites.SortForCollisions(); 
            bool foundone = false;
            for (int i = 0; i < Sprites.Count; i++)
            {
                Sprite sprite = Sprites[i];
                if (sprite.Name is object && sprite.Name != "")
                {
                    if (sprite.Within(Owner.MouseX, Owner.MouseY + Scroll, 1, 1) && !foundone)
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

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            baseCamera = Matrix4.CreateTranslation(0, -Scroll, 0) * baseCamera;
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            Sprites.Render(Owner.FrameCount);
        }
    }
}
