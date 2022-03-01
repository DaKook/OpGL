using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class TerminalTool : EditorTool
    {
        public override string DefaultName => "Terminal";

        public override string DefaultDescription => "nah";

        public override string DefaultKey => "T";

        public override Keys DefaultKeybind => Keys.T;

        public Texture Texture;
        private string deactivated;
        private string activated;
        private Size animationSize;

        private Terminal inQuestion;

        public TerminalTool(LevelEditor parent, Texture texture) : base(parent)
        {
            Texture = texture;
            SetAnimation("TerminalOff", "TerminalOn");
        }

        private void SetAnimation(string d, string a)
        {
            Animation da = Texture.AnimationFromName(d);
            Animation aa = Texture.AnimationFromName(a);
            if (da is object && aa is object && da.Hitbox.Size == aa.Hitbox.Size)
            {
                deactivated = d;
                activated = a;
                animationSize = da.Hitbox.Size;
            }
        }

        public override void Process()
        {
            base.Process();
            if (inQuestion is object)
            {

            }
            else
            {
                size = new Size((int)Math.Ceiling(animationSize.Width / 8f), (int)Math.Ceiling(animationSize.Height / 8f));
                position = centerOn(mouse, size * 8);
                color = Color.Blue;
                bool flipX = key(Keys.X);
                bool flipY = key(Keys.Z);
                if (left)
                {
                    Terminal t = new Terminal(0, 0, Texture, Texture.AnimationFromName(deactivated), Texture.AnimationFromName(activated), null, false, Owner);
                    t.CenterX = CenterX + CameraX;
                    if (flipY)
                        t.Y = position.Y + CameraY;
                    else
                        t.Bottom = Bottom + CameraY;
                    t.FlipX = flipX;
                    t.FlipY = flipY;
                    Parent.Sprites.Add(t);
                    inQuestion = t;
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
                    });
                }
                else if (isRightDown)
                {
                    List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is Terminal)
                        {
                            Owner.DeleteSprite(sprite);
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
