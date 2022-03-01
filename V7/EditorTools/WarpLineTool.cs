using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace V7
{
    public class WarpLineTool : EditorTool
    {
        public override string DefaultName => "Warp Line";

        public override string DefaultDescription => "nah";

        public override string DefaultKey => "I";

        public override Keys DefaultKeybind => Keys.I;

        private bool creatingLine;
        private Point anchor;

        private WarpLine inQuestion;

        private Texture texture;
        private string vAnimation;
        private string hAnimation;

        public WarpLineTool(LevelEditor parent) : base(parent)
        {
            texture = Owner.TextureFromName("lines");
            vAnimation = "VWarpLine";
            hAnimation = "HWarpLine";
        }

        public override void Process()
        {
            base.Process();
            Prompt = "";
            PromptImportant = false;
            if (inQuestion is object)
            {
                Prompt = "Choose an offset";
                PromptImportant = true;
                size = new Size(inQuestion.Horizontal ? inQuestion.Length : 1, inQuestion.Horizontal ? 1 : inQuestion.Length);
                Point c = mouse;
                if (inQuestion.Horizontal)
                    c.Y -= 3;
                else
                    c.X -= 3;
                position = centerOn(c, size * 8);
                if (inQuestion.Horizontal)
                    position.Y += 3;
                else
                    position.X += 3;
                if (left)
                {
                    PointF offset = new PointF(position.X - inQuestion.X - CameraX, position.Y - inQuestion.Y - CameraY);
                    if (inQuestion.Horizontal)
                    {
                        if (inQuestion.Direction < 0)
                            offset.Y += 5;
                        else
                            offset.Y += 4;
                    }
                    else
                    {
                        if (inQuestion.Direction < 0)
                            offset.X += 5;
                        else
                            offset.X += 4;
                    }
                    inQuestion.Offset = offset;
                    inQuestion = null;
                }
            }
            else if (creatingLine)
            {
                position = anchor;
                color = Color.Cyan;
                Point pos = centerOn(mouse, new Size(8, 8));
                Point p = new Point(pos.X - anchor.X, pos.Y - anchor.Y);
                bool h;
                if (h = Math.Abs(p.X) >= Math.Abs(p.Y))
                {
                    size.Height = 1;
                    if (p.X >= 0)
                        size.Width = p.X / 8 + 1;
                    else
                    {
                        size.Width = -p.X / 8 + 1;
                        position.X += p.X;
                    }
                    if (mouse.Y < anchor.Y + 4)
                        position.Y -= 3;
                    else
                        position.Y += 3;
                }
                else
                {
                    size.Width = 1;
                    if (p.Y >= 0)
                        size.Height = p.Y / 8 + 1;
                    else
                    {
                        size.Height = -p.Y / 8 + 1;
                        position.Y += p.Y;
                    }
                    if (mouse.X < anchor.X + 4)
                        position.X -= 3;
                    else
                        position.X += 3;
                }
                if (!isLeftDown)
                {
                    WarpLine w = new WarpLine(position.X + CameraX, position.Y + CameraY, texture, texture.AnimationFromName(h ? hAnimation : vAnimation), h ? size.Width : size.Height, h, 0, 0, 0);
                    if (h)
                    {
                        if (position.Y < anchor.Y)
                        {
                            w.Y += 3;
                            w.Direction = -1;
                        }
                        else
                        {
                            w.Y += 4;
                            w.Direction = 1;
                        }
                    }
                    else
                    {
                        if (position.X < anchor.X)
                        {
                            w.X += 3;
                            w.Direction = -1;
                        }
                        else
                        {
                            w.X += 4;
                            w.Direction = 1;
                        }
                    }
                    Parent.Sprites.Add(w);
                    inQuestion = w;
                    creatingLine = false;
                }
            }
            else if (shift)
            {
                size = new Size(1, 1);
                position = centerOn(mouse, size * 8);
                color = Color.Cyan;
                if (left)
                {
                    creatingLine = true;
                    anchor = position;
                }
            }
            else
            {
                size = new Size(1, 1);
                position = centerOn(mouse, size * 8);
                color = Color.Blue;
                if (left)
                {
                    List<Sprite> sprites = Parent.Sprites.GetPotentialColliders(position.X + CameraX, position.Y + CameraY, 8, 8);
                    if (sprites.Any((s) => s is WarpLine))
                    {
                        foreach (Sprite sprite in sprites)
                        {
                            if (sprite is WarpLine)
                            {
                                WarpLine w1 = sprite as WarpLine;
                                if (/*CheckGap(w1)*/true)
                                {
                                    WarpLine w2 = new WarpLine(w1.X + w1.Offset.X, w1.Y + w1.Offset.Y, w1.Texture, w1.Animation, w1.Length, w1.Horizontal, -w1.Offset.X, -w1.Offset.Y, -w1.Direction);
                                    if (w2.Horizontal)
                                        w2.Y -= w2.Direction;
                                    else
                                        w2.X -= w2.Direction;
                                    sprites = Parent.Sprites.GetPotentialColliders(w2);
                                    if (sprites.Any((s) => s is WarpLine && (s as WarpLine).Horizontal == w2.Horizontal))
                                    {
                                        w2.Dispose();
                                    }
                                    else
                                        Parent.Sprites.Add(w2);
                                }
                            }
                        }
                    }
                    else
                    {
                        WarpLine wl = null;
                        if (position.X == 0)
                        {
                            wl = new WarpLine(position.X + CameraX, position.Y + CameraY, texture, texture.AnimationFromName(vAnimation), 1, false, Game.RESOLUTION_WIDTH, 0, -1);
                            ExtendLine(wl, position);
                            Parent.Sprites.Add(wl);
                        }
                        else if (position.X == Game.RESOLUTION_WIDTH - 8)
                        {
                            wl = new WarpLine(position.X + 7 + CameraX, position.Y + CameraY, texture, texture.AnimationFromName(vAnimation), 1, false, -Game.RESOLUTION_WIDTH, 0, 1);
                            ExtendLine(wl, position);
                            Parent.Sprites.Add(wl);
                        }
                        else if (position.Y == 0)
                        {
                            wl = new WarpLine(position.X + CameraX, position.Y + CameraY, texture, texture.AnimationFromName(vAnimation), 1, true, 0, Game.RESOLUTION_HEIGHT, -1);
                            ExtendLine(wl, position);
                            Parent.Sprites.Add(wl);
                        }
                        else if (position.Y == Game.RESOLUTION_HEIGHT - 8)
                        {
                            wl = new WarpLine(position.X + CameraX, position.Y + 7 + CameraY, texture, texture.AnimationFromName(vAnimation), 1, true, 0, -Game.RESOLUTION_HEIGHT, 1);
                            ExtendLine(wl, position);
                            Parent.Sprites.Add(wl);
                        }
                        if (wl is object && (ctrl || !CheckGap(wl)))
                        {
                            inQuestion = wl;
                        }
                    }
                }
                else if (isRightDown)
                {
                    List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is WarpLine)
                        {
                            Owner.DeleteSprite(sprite);
                        }
                    }
                }
            }
        }

        private void ExtendLine(WarpLine line, Point center)
        {
            bool horizontal = line.Horizontal;
            if (horizontal)
            {
                line.X = center.X + CameraX;
                int x = center.X + (int)CameraX;
                while (x < Owner.CurrentRoom.Width + 8)
                {
                    x += 8;
                    List<Sprite> c = Parent.Sprites.GetPotentialColliders(x, center.Y + CameraY);
                    if (c.Any((s) => s is Tile && s.Solid != Sprite.SolidState.NonSolid))
                        break;
                }
                int farx = x;
                x = center.X + (int)CameraX;
                while (x > -8)
                {
                    x -= 8;
                    List<Sprite> c = Parent.Sprites.GetPotentialColliders(x, center.Y + CameraY);
                    if (c.Any((s) => s is Tile && s.Solid != Sprite.SolidState.NonSolid))
                    {
                        x += 8;
                        break;
                    }
                }
                line.X = x + CameraX;
                line.Length = (farx - x) / 8;
                line.Animation = line.Texture.AnimationFromName(hAnimation);
            }
            else
            {
                line.Y = center.Y + CameraY;
                int y = center.Y + (int)CameraY;
                while (y < Owner.CurrentRoom.Height + 8)
                {
                    y += 8;
                    List<Sprite> c = Parent.Sprites.GetPotentialColliders(center.X + CameraX, y);
                    if (c.Any((s) => s is Tile && s.Solid != Sprite.SolidState.NonSolid))
                        break;
                }
                int fary = y;
                y = center.Y + (int)CameraY;
                while (y > -8)
                {
                    y -= 8;
                    List<Sprite> c = Parent.Sprites.GetPotentialColliders(center.X + CameraX, y);
                    if (c.Any((s) => s is Tile && s.Solid != Sprite.SolidState.NonSolid))
                    {
                        y += 8;
                        break;
                    }
                }
                line.Y = y + CameraY;
                line.Length = (fary - y) / 8;
                line.Animation = line.Texture.AnimationFromName(vAnimation);
            }
        }

        private bool CheckGap(WarpLine line)
        {
            Func<Sprite, bool> test = (s) => s is Tile && s.Solid == Sprite.SolidState.Ground;
            if (line.Horizontal)
            {
                bool match = true;
                int x = (int)line.X - 8;
                for (int i = 0; i < line.Length + 2; i++)
                {
                    List<Sprite> s1 = Parent.Sprites.GetPotentialColliders(x + i * 8, Game.RESOLUTION_HEIGHT - 8 + CameraY, 1, 1);
                    List<Sprite> s2 = Parent.Sprites.GetPotentialColliders(x + i * 8, CameraY, 1, 1);
                    if (!(match = s1.Any(test) == s2.Any(test)))
                        break;
                }
                return match;
            }
            else
            {
                bool match = true;
                int y = (int)line.Y - 8;
                for (int i = 0; i < line.Length + 2; i++)
                {
                    List<Sprite> s1 = Parent.Sprites.GetPotentialColliders(Game.RESOLUTION_WIDTH - 8 + CameraX, y + i * 8, 1, 1);
                    List<Sprite> s2 = Parent.Sprites.GetPotentialColliders(CameraX, y + i * 8, 1, 1);
                    if (!(match = s1.Any(test) == s2.Any(test)))
                        break;
                }
                return match;
            }
        }

        public override JObject Save()
        {
            throw new NotImplementedException();
        }
    }
}
