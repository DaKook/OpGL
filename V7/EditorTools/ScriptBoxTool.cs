using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class ScriptBoxTool : EditorTool
    {
        public override string DefaultName => "Script Box";

        public override string DefaultDescription => "nah";

        public override string DefaultKey => "Y";

        public override Keys DefaultKeybind => Keys.Y;

        private Texture texture;

        private bool creatingBox;
        private Point anchor;

        private ScriptBox inQuestion;

        public ScriptBoxTool(LevelEditor parent) : base(parent)
        {
            texture = Owner.BoxTexture;
            Sprites = new SpriteCollection();
        }

        public override void Process()
        {
            base.Process();
            if (inQuestion is object)
            {
                color = Color.Blue;
                position = new Point((int)inQuestion.X, (int)inQuestion.Y);
                size = new Size(inQuestion.WidthTiles, inQuestion.HeightTiles);
            }
            else if (creatingBox)
            {
                color = Color.Cyan;
                position = anchor;
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
                if (!isLeftDown)
                {
                    creatingBox = false;
                    ScriptBox sb = new ScriptBox(position.X + CameraX, position.Y + CameraY, texture, size.Width, size.Height, null, Owner);
                    Parent.Sprites.Add(sb);
                    inQuestion = sb;
                    Owner.OpenScripts((s) =>
                    {
                        if (s is object)
                        {
                            inQuestion.Script = Owner.ScriptFromName(s, true);
                        }
                        else
                        {
                            Owner.DeleteSprite(inQuestion);
                        }
                        inQuestion = null;
                        anchor = new Point();
                    });
                }
            }
            else
            {
                size = new Size(1, 1);
                position = centerOn(mouse, size * 8);
                color = Color.Cyan;
                if (left)
                {
                    creatingBox = true;
                    anchor = position;
                }
                else if (isRightDown)
                {
                    List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is ScriptBox && (shift || (position.X - sprite.X < 8 && position.Y - sprite.Y < 8)))
                        {
                            Owner.DeleteSprite(sprite);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < Sprites.Count; i++)
                    {
                        Sprites[i].Dispose();
                    }
                    Sprites.Clear();
                    List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is ScriptBox && position.X - sprite.X < 8 && position.Y - sprite.Y < 8)
                        {
                            StringDrawable name = new StringDrawable(sprite.X - CameraX, sprite.Y - 8 - CameraY, Owner.FontTexture, (sprite as ScriptBox).Script?.Name ?? "None", Color.White);
                            if (name.Y < 0)
                                name.Y = 0;
                            Sprites.Add(name);
                        }
                    }
                }
            }
        }

        public override JObject Save()
        {
            throw new NotImplementedException();
        }
    }
}
