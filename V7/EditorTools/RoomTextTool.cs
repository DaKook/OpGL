using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class RoomTextTool : EditorTool
    {
        public override string DefaultName => "Roomtext";

        public override string DefaultDescription => "nah";

        public override string DefaultKey => "R";

        public override Keys DefaultKeybind => Keys.R;

        private FontTexture texture;

        public RoomTextTool(LevelEditor parent) : base(parent)
        {
            texture = Owner.FontTexture;
        }

        public override void Process()
        {
            base.Process();
            size = new Size(1, 1);
            position = centerOn(mouse, size * 8);
            color = Color.Blue;
            if (left)
            {
                if (Owner.TypingTo is object)
                {
                    Owner.EscapeTyping();
                }
                StringDrawable sd = new StringDrawable(position.X + CameraX, position.Y + CameraY, texture, "", Color.White);
                sd.Layer = 10;
                Parent.Sprites.Add(sd);
                Owner.StartTyping(sd, (r, s) =>
                {
                    if (!r)
                    {
                        Owner.DeleteSprite(sd);
                    }
                }, true);
            }
            else if (middle)
            {
                List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                foreach (Sprite sprite in spr)
                {
                    if (sprite is StringDrawable)
                    {
                        string text = (sprite as StringDrawable).Text;
                        Owner.StartTyping(sprite as StringDrawable, (r, s) =>
                        {
                            if (!r)
                            {
                                (sprite as StringDrawable).Text = text;
                            }
                        }, true);
                    }
                }
            }
            else if (isRightDown)
            {
                List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                foreach (Sprite sprite in spr)
                {
                    if (sprite is StringDrawable)
                    {
                        if (sprite == Owner.TypingTo)
                            Owner.EscapeTyping();
                        Owner.DeleteSprite(sprite);
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
