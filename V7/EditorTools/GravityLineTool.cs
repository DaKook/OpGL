using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace V7
{
    internal class GravityLineTool : EditorTool
    {
        public override string DefaultName => "Gravity Line";

        public override string DefaultDescription => "nah";

        public override string DefaultKey => "0";

        public override Keys DefaultKeybind => Keys.D0;

        public Texture Texture;
        private string vAnimation;
        private string hAnimation;

        private Point lastPlacement;
        private GravityLine lastPlaced;
        private Point lastRoom;

        private bool creatingLine;
        private Point anchor;

        public GravityLineTool(LevelEditor parent, Texture texture) : base(parent)
        {
            Texture = texture;
            vAnimation = "VGravLine";
            hAnimation = "HGravLine";
        }

        public override void Process()
        {
            base.Process();
            if (lastRoom != new Point(Owner.CurrentRoom.X, Owner.CurrentRoom.Y))
            {
                lastPlaced = null;
                lastPlacement = new Point(0, 0);
            }
            lastRoom = new Point(Owner.CurrentRoom.X, Owner.CurrentRoom.Y);
            if (creatingLine)
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
                }
                if (!isLeftDown)
                {
                    GravityLine g = new GravityLine(position.X + CameraX, position.Y + CameraY, Texture, Texture.AnimationFromName(h ? hAnimation : vAnimation), h, h ? size.Width : size.Height);
                    if (h)
                        g.Y += 3;
                    else
                        g.X += 3;
                    creatingLine = false;
                    anchor = new Point(0, 0);
                    Parent.Sprites.Add(g);
                }
            }
            else if (shift)
            {
                color = Color.Cyan;
                size = new Size(1, 1);
                position = centerOn(mouse, size * 8);
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
                    if (ctrl)
                    {
                        List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                        foreach (Sprite sprite in spr)
                        {
                            if (sprite is GravityLine)
                            {
                                GravityLine g = sprite as GravityLine;
                                g.Horizontal = !g.Horizontal;
                                SetLine(g, position, g.Horizontal);
                            }
                        }
                    }
                    else
                    {
                        if (lastPlaced is object && position == lastPlacement)
                        {
                            lastPlaced.Horizontal = !lastPlaced.Horizontal;
                            SetLine(lastPlaced, lastPlacement, lastPlaced.Horizontal);
                        }
                        else
                        {
                            lastPlacement = position;
                            GravityLine g = new GravityLine(0, 0, Texture, Texture.AnimationFromName(vAnimation), !key(Keys.Z), 1);
                            SetLine(g, position, g.Horizontal);
                            Parent.Sprites.Add(g);
                            lastPlaced = g;
                        }
                    }
                }
                else if (isRightDown)
                {
                    List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is GravityLine)
                        {
                            Owner.DeleteSprite(sprite);
                        }
                    }
                }
            }
        }

        private void SetLine(GravityLine l, Point center, bool horizontal)
        {
            if (horizontal)
            {
                l.X = center.X + CameraX;
                l.Y = center.Y + 3 + CameraY;
                int x = center.X;
                while (x < Owner.CurrentRoom.Width + 8)
                {
                    x += 8;
                    List<Sprite> c = Parent.Sprites.GetPotentialColliders(x, center.Y);
                    if (c.Any((s) => s is Tile && s.Solid != Sprite.SolidState.NonSolid))
                        break;
                }
                int farx = x;
                x = center.X;
                while (x > -8)
                {
                    x -= 8;
                    List<Sprite> c = Parent.Sprites.GetPotentialColliders(x, center.Y);
                    if (c.Any((s) => s is Tile && s.Solid != Sprite.SolidState.NonSolid))
                    {
                        x += 8;
                        break;
                    }
                }
                l.X = x + CameraX;
                l.LengthTiles = (farx - x) / 8;
                l.Animation = l.Texture.AnimationFromName(hAnimation);
            }
            else
            {
                l.X = center.X + 3 + CameraX;
                l.Y = center.Y + CameraY;
                int y = center.Y;
                while (y < Owner.CurrentRoom.Height + 8)
                {
                    y += 8;
                    List<Sprite> c = Parent.Sprites.GetPotentialColliders(center.X, y);
                    if (c.Any((s) => s is Tile && s.Solid != Sprite.SolidState.NonSolid))
                        break;
                }
                int fary = y;
                y = center.Y;
                while (y > -8)
                {
                    y -= 8;
                    List<Sprite> c = Parent.Sprites.GetPotentialColliders(center.X, y);
                    if (c.Any((s) => s is Tile && s.Solid != Sprite.SolidState.NonSolid))
                    {
                        y += 8;
                        break;
                    }
                }
                l.Y = y + CameraY;
                l.LengthTiles = (fary - y) / 8;
                l.Animation = l.Texture.AnimationFromName(vAnimation);
            }
        }

        public override JObject Save()
        {
            throw new NotImplementedException();
        }
    }
}
