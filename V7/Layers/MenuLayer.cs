using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class MenuLayer : SpritesLayer
    {
        private static int currentColor;
        private static Color[] menuColors = new Color[] { Color.Cyan, Color.FromArgb(255, 160, 30, 255), Color.FromArgb(255, 255, 30, 255), Color.FromArgb(255, 255, 50, 50), Color.Yellow, Color.Lime };

        public override bool UsesExtraHud => true;

        public Color CurrentColor { get; private set; }
        public BGSpriteCollection Background;
        public SpriteCollection ExtraSprites;
        public SpriteCollection Sprites;
        public List<VMenuItem> MenuItems;
        public int SelectedItem;
        RectangleSprite ItemSelector;
        public int EscapeItem;

        public SpriteCollection ExtraHud;
        public StringDrawable DescriptionText;

        public Action OnUpdateSelection;

        private MenuLayer(Game owner, List<VMenuItem> items)
        {
            Darken = 1;
            FreezeBelow = true;
            Owner = owner;
            SetColor();
            Sprites = new SpriteCollection();
            ExtraHud = new SpriteCollection();
            DescriptionText = new StringDrawable(8, Game.RESOLUTION_HEIGHT + 4, Owner.NonMonoFont, "");
            ItemSelector = new RectangleSprite(0, 0, 0, 0);
            Sprites.Add(ItemSelector);
            ExtraSprites = new SpriteCollection();
            MenuItems = items;
            SelectedItem = 1;
            CreateMenuSprites();
        }

        private void SetColor()
        {
            CurrentColor = menuColors[currentColor];
            currentColor++;
            currentColor %= menuColors.Length;
        }

        public void CreateMenuSprites()
        {
            for (int i = 1; i < Sprites.Count; i++)
            {
                Sprites[i].Dispose();
            }
            Sprites.Clear();
            Sprites.Add(ItemSelector);
            float y = 0;
            //MaxMenuWidth = 0;
            for (int i = 0; i < MenuItems.Count; i++)
            {
                y += MenuItems[i].Offset.Y;
                StringDrawable sd = new StringDrawable(MenuItems[i].Offset.X, y, Owner.FontTexture, MenuItems[i].Text);
                if (SelectedItem == i + 1)
                    sd.Color = CurrentColor;
                else
                    sd.Color = Color.FromArgb(255, CurrentColor.R / 2, CurrentColor.G / 2, CurrentColor.B / 2);
                sd.CenterX = Game.RESOLUTION_WIDTH / 2 + MenuItems[i].Offset.X;
                y += sd.Height + 4;
                sd.Layer = i;
                //if (sd.Width > MaxMenuWidth) MaxMenuWidth = sd.Width;
                Sprites.Add(sd);
                //hudSprites.Add(sd);
            }
            if (Sprites.Count > 1)
            {
                float remainder = Game.RESOLUTION_HEIGHT - y;
                remainder /= 2;
                for (int i = 1; i < Sprites.Count; i++)
                {
                    StringDrawable sd = (StringDrawable)Sprites[i];
                    sd.Y += remainder;
                }
                //ItemSelector.SetSize(MaxMenuWidth + 16, ItemSprites[SelectedItem].Height + 4);
                ItemSelector.Color = Color.FromArgb(255, CurrentColor.R / 4, CurrentColor.G / 4, CurrentColor.B / 4);
                ItemSelector.CenterX = Sprites[SelectedItem].CenterX;
                ItemSelector.CenterY = Sprites[SelectedItem].CenterY;
            }
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            Background?.RenderPrep(viewMatrixLocation, baseCamera);
            Background?.Render(Owner.FrameCount);
            Sprites.Render(Owner.FrameCount);
            ExtraSprites.Render(Owner.FrameCount);
        }
        public override void DrawExtraHud(Matrix4 baseCamera, int viewMatrixLocation)
        {
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            ExtraHud.Render(Owner.FrameCount);
        }

        public override void Process()
        {
            for (int i = 0; i < ExtraSprites.Count; i++)
            {
                Sprite s = ExtraSprites[i];
                s.Process();
            }
            if (Background is object)
            {
                Background.BaseColor = CurrentColor;
                Background.Process();
            }
            if (Sprites.Count > 1)
            {
                if (Owner.JustMoved)
                {
                    for (int i = 1; i < Sprites.Count; i++)
                    {
                        if (Owner.MouseY >= Sprites[i].Y && Owner.MouseY <= Sprites[i].Bottom && Owner.MouseX > Sprites[i].X && Owner.MouseX < Sprites[i].Right)
                        {
                            if (SelectedItem != i)
                            {
                                SelectedItem = i;
                                UpdateMenuSelection();
                            }
                            break;
                        }
                    }
                }
                RectangleF target = new RectangleF(Sprites[SelectedItem].CenterX, Sprites[SelectedItem].CenterY, Sprites[SelectedItem].Width + 32, Sprites[SelectedItem].Height + 4);
                if (ItemSelector.CenterY < target.Y)
                {
                    ItemSelector.Y += (float)Math.Ceiling((target.Y - ItemSelector.CenterY) / 3);
                    if (ItemSelector.CenterY > target.Y)
                        ItemSelector.CenterY = target.Y;
                }
                else if (ItemSelector.CenterY > target.Y)
                {
                    ItemSelector.Y += (float)Math.Floor((target.Y - ItemSelector.CenterY) / 3);
                    if (ItemSelector.CenterY < target.Y)
                        ItemSelector.CenterY = target.Y;
                }
                if (ItemSelector.CenterX < target.X)
                {
                    ItemSelector.X += (float)Math.Ceiling((target.X - ItemSelector.CenterX) / 3);
                    if (ItemSelector.CenterX > target.X)
                        ItemSelector.CenterX = target.X;
                }
                else if (ItemSelector.CenterX > target.X)
                {
                    ItemSelector.X += (float)Math.Floor((target.X - ItemSelector.CenterX) / 3);
                    if (ItemSelector.CenterX < target.X)
                        ItemSelector.CenterX = target.X;
                }
                if (ItemSelector.Width < target.Width)
                {
                    float cx = ItemSelector.CenterX;
                    ItemSelector.SetWidth(ItemSelector.Width + (float)Math.Ceiling((target.Width - ItemSelector.Width) / 3));
                    if (ItemSelector.Width > target.Width)
                        ItemSelector.SetWidth(target.Width);
                    ItemSelector.CenterX = cx;
                }
                else if (ItemSelector.Width > target.Width)
                {
                    float cx = ItemSelector.CenterX;
                    ItemSelector.SetWidth(ItemSelector.Width + (float)Math.Floor((target.Width - ItemSelector.Width) / 3));
                    if (ItemSelector.Width < target.Width)
                        ItemSelector.SetWidth(target.Width);
                    ItemSelector.CenterX = cx;
                }
                if (ItemSelector.Height < target.Height)
                {
                    float cx = ItemSelector.CenterY;
                    ItemSelector.SetHeight(ItemSelector.Height + (float)Math.Ceiling((target.Height - ItemSelector.Height) / 3));
                    if (ItemSelector.Height > target.Height)
                        ItemSelector.SetHeight(target.Height);
                    ItemSelector.CenterY = cx;
                }
                else if (ItemSelector.Height > target.Height)
                {
                    float cx = ItemSelector.CenterY;
                    ItemSelector.SetHeight(ItemSelector.Height + (float)Math.Floor((target.Height - ItemSelector.Height) / 3));
                    if (ItemSelector.Height < target.Height)
                        ItemSelector.SetHeight(target.Height);
                    ItemSelector.CenterY = cx;
                }
            }
            DescriptionText.Color = CurrentColor;
            if (Owner.EnableExtraHud && !ExtraHud.Contains(DescriptionText))
            {
                ExtraHud.Add(DescriptionText);
                ExtraSprites.Remove(DescriptionText);
                DescriptionText.Y = Game.RESOLUTION_HEIGHT + 4;
            }
            else if (!Owner.EnableExtraHud && !Sprites.Contains(DescriptionText))
            {
                ExtraHud.Remove(DescriptionText);
                ExtraSprites.Add(DescriptionText);
                DescriptionText.Y = Game.RESOLUTION_HEIGHT - 20;
            }
            DescriptionText.Text = MenuItems[SelectedItem - 1].Description ?? "";
        }

        private void UpdateMenuSelection()
        {
            for (int i = 1; i < Sprites.Count; i++)
            {
                Sprite sd = Sprites[i];
                if (SelectedItem == i)
                    sd.Color = CurrentColor;
                else
                    sd.Color = Color.FromArgb(255, CurrentColor.R / 2, CurrentColor.G / 2, CurrentColor.B / 2);
            }
            //ItemSelector.SetHeight(Sprites[SelectedItem].Height + 4);
            OnUpdateSelection?.Invoke();
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            for (int i = 1; i < Sprites.Count; i++)
            {
                if (Owner.MouseY >= Sprites[i].Y && Owner.MouseY <= Sprites[i].Bottom && Owner.MouseX >= Sprites[i].X && Owner.MouseX <= Sprites[i].Right)
                {
                    MenuItems[i - 1].Action();
                    Owner.ReleaseLeftMouse();
                    break;
                }
            }
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (e.Key == Keys.Right || e.Key == Keys.Down || e.Key == Keys.D || e.Key == Keys.S)
            {
                SelectedItem += 1;
                SelectedItem = (SelectedItem - 1) % MenuItems.Count + 1;
                UpdateMenuSelection();
            }
            else if (e.Key == Keys.Left || e.Key == Keys.Up || e.Key == Keys.A || e.Key == Keys.W)
            {
                SelectedItem -= 1;
                if (SelectedItem <= 0)
                    SelectedItem = MenuItems.Count;
                UpdateMenuSelection();
            }
            else if (e.Key == Keys.Z || e.Key == Keys.Space || e.Key == Keys.Enter || e.Key == Keys.V)
            {
                MenuItems[SelectedItem - 1].Action?.Invoke();
            }
            else if (e.Key == Keys.Escape)
            {
                if (EscapeItem > -1 && EscapeItem < MenuItems.Count)
                    MenuItems[EscapeItem].Action();
            }
        }

        public override void HandleWheel(int e)
        {
            // Do nothing
        }

        public override void Dispose()
        {
            Sprites.Dispose();
            ExtraSprites.Dispose();
        }

        public class Builder
        {
            public Game Owner { get; private set; }
            List<VMenuItem> Items;
            public MenuLayer Result { get; private set; }
            public int EscapeItem { get; set; }
            public Builder(Game owner)
            {
                Owner = owner;
                Items = new List<VMenuItem>();
            }

            public void AddItem(string text, Action action, string description = "")
            {
                Items.Add(new VMenuItem(text, action, description));
            }
            public void AddItem(string text, Action action, float offsetX, float offsetY, string description = "")
            {
                Items.Add(new VMenuItem(text, action, description) { Offset = new PointF(offsetX, offsetY) });
            }

            public MenuLayer Build()
            {
                Result = new MenuLayer(Owner, Items);
                Owner.AddLayer(Result);
                Result.EscapeItem = EscapeItem;
                return Result;
            }

            public int ItemCount => Items.Count;

            public void ShowTextBox(string text, FontTexture texture, float x, float y, Color color)
            {
                VTextBox tb = new VTextBox(0, 0, texture, text, color);
                tb.Text = tb.Text;
                tb.CenterX = x;
                tb.CenterY = y;
                tb.frames = 200;
                Result.ExtraSprites.Add(tb);
                tb.Disappeared += (t) => { Result.ExtraSprites.Remove(t); t.Dispose(); };
                tb.Appear();
            }
        }
    }
}
