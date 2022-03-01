using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace V7
{
    public class CheckpointTool : EditorTool
    {
        public override string DefaultName => "Checkpoint";

        public override string DefaultDescription => "When a crewman dies, they will respawn at the last checkpoint they touched.\n" +
            "Use LEFT-CLICK to place a checkpoint, and RIGHT-CLICK to delete checkpoints.\n" +
            "Hold Z when placing a checkpoint to place it upside-down.\n" +
            "Hold X when placing a checkpoint to place it facing left.\n" +
            "Press A to change the animation, and S to change the texture.";

        public override string DefaultKey => "5";

        public override Keys DefaultKeybind => Keys.D5;

        public Texture Texture;
        private string onAnimation;
        private string offAnimation;

        public CheckpointTool(LevelEditor parent, Texture texture) : base(parent)
        {
            Texture = texture;
            onAnimation = "CheckOn";
            offAnimation = "CheckOff";
            SetSize();
        }

        private void SetSize()
        {
            if (Texture is object)
            {
                Animation a;
                if (offAnimation is object && (a = Texture.AnimationFromName(offAnimation)) is object)
                {
                    size = new Size((int)Math.Ceiling(a.Hitbox.Width / 8f), (int)Math.Ceiling(a.Hitbox.Height / 8f));
                }
                else
                    size = new Size(1, 1);
            }
            else
                size = new Size(1, 1);
        }

        public override void Process()
        {
            base.Process();
            position = centerOn(mouse, size * 8);
            color = Color.Blue;
            Prompt = offAnimation + " / " + onAnimation;
            bool flipX = key(Keys.X);
            bool flipY = key(Keys.Z);
            if (left)
            {
                if (Texture is null || Texture.AnimationFromName(offAnimation) is null || Texture.AnimationFromName(onAnimation) is null) return;
                Checkpoint cp = new Checkpoint(position.X + CameraX, position.Y + CameraY, Owner, Texture, Texture.AnimationFromName(offAnimation), Texture.AnimationFromName(onAnimation), flipX, flipY);
                cp.CenterX = CenterX + CameraX;
                if (!flipY)
                    cp.Bottom = Bottom + CameraY;
                cp.InitializePosition();
                Parent.Sprites.Add(cp);
            }
            else if (isRightDown)
            {
                List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                foreach (Sprite sprite in spr)
                {
                    if (sprite is Checkpoint)
                    {
                        Parent.Sprites.RemoveFromCollisions(sprite);
                    }
                }
            }
        }

        public override void HandleKey(PassedKeyEvent e)
        {
            if (!e.Control && !e.Shift && !e.Alt)
            {
                if (e.Key == Keys.A)
                {
                    PreviewScreen ps = new PreviewScreen(new Sprite[] { }, null, Owner);
                    VTextBox add = new VTextBox(0, 0, Owner.FontTexture, "Activated Animation", Color.White);
                    add.Visible = true;
                    add.Name = "on";
                    add.CenterX = Game.RESOLUTION_WIDTH / 2;
                    add.Bottom = Game.RESOLUTION_HEIGHT / 2 - 4;
                    ps.Sprites.Add(add);
                    add = new VTextBox(0, 0, Owner.FontTexture, "Deactivated Animation", Color.White);
                    add.Visible = true;
                    add.Name = "off";
                    add.CenterX = Game.RESOLUTION_WIDTH / 2;
                    add.Y = Game.RESOLUTION_HEIGHT / 2 + 4;
                    ps.Sprites.Add(add);
                    ps.OnClick = (s) =>
                    {
                        ps = Parent.AnimationPreviews(Texture);
                        ps.OnClick = (a) =>
                        {
                            if (s.Name == "on")
                                onAnimation = a.Name;
                            else
                                offAnimation = a.Name;
                            SetSize();
                        };
                        Owner.AddLayer(ps);
                    };
                    Owner.AddLayer(ps);
                }
                else if (e.Key == Keys.S)
                {
                    string[] textures = new string[Owner.Textures.Count];
                    for (int i = 0; i < textures.Length; i++)
                    {
                        textures[i] = Owner.Textures.Keys[i];
                    }
                    Owner.ShowDialog("Choose a texture", Texture.Name, textures, (r, s) =>
                    {
                        if (r)
                        {
                            Texture t = Owner.TextureFromName(s);
                            if (t is object)
                            {
                                Texture = t;
                                if (t.AnimationFromName(onAnimation) is null)
                                {
                                    bool set = false;
                                    for (int i = 0; i < t.Animations.Count; i++)
                                    {
                                        string a = t.Animations.Keys[i];
                                        if (a.ToLower().Contains("check") && a.ToLower().Contains("on"))
                                        {
                                            onAnimation = a;
                                            set = true;
                                            break;
                                        }
                                    }
                                    if (!set)
                                        onAnimation = t.Animations.Keys.FirstOrDefault() ?? "";
                                }
                                if (t.AnimationFromName(offAnimation) is null)
                                {
                                    bool set = false;
                                    for (int i = 0; i < t.Animations.Count; i++)
                                    {
                                        string a = t.Animations.Keys[i];
                                        if (a.ToLower().Contains("check") && a.ToLower().Contains("off"))
                                        {
                                            offAnimation = a;
                                            set = true;
                                            break;
                                        }
                                    }
                                    if (!set)
                                        offAnimation = t.Animations.Keys.FirstOrDefault() ?? "";
                                }
                                SetSize();
                            }
                        }
                    });
                }
            }
        }

        public override JObject Save()
        {
            return getBaseSave(DefaultName);
        }
    }
}
