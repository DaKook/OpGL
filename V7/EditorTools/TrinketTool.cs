using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace V7
{
    public class TrinketTool : EditorTool
    {
        public override string DefaultName => "Trinket";

        public override string DefaultDescription => "Place trinkets in the level for the player to collect.\n" +
            "Use LEFT-CLICK to place a trinket, and RIGHT-CLICK to delete trinkets.\n" +
            "Use MIDDLE-CLICK to change the ID of a trinket.\n" +
            "When placing a trinket, an ID value is automatically assigned to it. The first positive integer ID value available will be used, including 0.\n" +
            "Press A to change the animation, and S to change the texture.\n" +
            "Press C to change the script. As with other properties, this only affects trinkets placed after the change.";

        public override string DefaultKey => "4";

        public override Keys DefaultKeybind => Keys.D4;

        public Texture Texture;
        private string animation;
        private string script;

        public TrinketTool(LevelEditor parent, Texture texture) : base(parent)
        {
            Texture = texture;
            animation = "Trinket";
            script = "trinket";
            SetSize();
        }

        private void SetSize()
        {
            if (Texture is object)
            {
                Animation a;
                if (animation is object && (a = Texture.AnimationFromName(animation)) is object)
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
            color = Color.Blue;
            position = centerOn(mouse, size * 8);
            if (left)
            {
                int id = 0;
                while (Owner.LevelTrinkets.ContainsKey(id))
                {
                    id++;
                }
                Trinket t = new Trinket(0, 0, Texture, Texture.AnimationFromName(animation), Owner.ScriptFromName(script), Owner);
                if (t is Trinket)
                {
                    t.CenterX = CenterX + CameraX;
                    t.CenterY = CenterY + CameraY;
                    t.InitializePosition();
                    t.SetID(id);
                    Parent.Sprites.Add(t);
                }
            }
            else if (middle)
            {
                List<Sprite> s = Parent.Sprites.GetPotentialColliders(mouse.X, mouse.Y, 1, 1);
                Trinket t = null;
                for (int i = 0; i < s.Count; i++)
                {
                    if (s[i] is Trinket)
                    {
                        t = s[i] as Trinket;
                        break;
                    }
                }
                if (t is object)
                {
                    Owner.ShowDialog("Trinket ID", t.ID.ToString(), new string[] { }, (r, s) =>
                    {
                        if (r)
                        {
                            if (int.TryParse(s, out int i))
                            {
                                t.SetID(i);
                                if (Owner.LevelTrinkets[i] > 1)
                                {
                                    Parent.Notify("There are now " + Owner.LevelTrinkets[i].ToString() + " trinkets\nwith the ID value of " + i.ToString(), t.X - CameraX, t.Y - 34 - CameraY, Color.Green, 150);
                                }
                            }
                            else
                            {
                                Parent.Notify("Invalid ID value!", t.X - CameraX, t.Y - 26 - CameraY, Color.FromArgb(255, 255, 55, 55), 90);
                            }
                        }
                    });
                }
            }
            else if (isRightDown)
            {
                List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                foreach (Sprite sprite in spr)
                {
                    if (sprite is Trinket)
                    {
                        Owner.DeleteSprite(sprite);
                    }
                }
            }
            Prompt = "Current: " + Owner.LevelTrinkets.Count + " (" + animation + ") - " + script;
        }

        public override void HandleKey(PassedKeyEvent e)
        {
            if (!e.Control && !e.Shift && !e.Alt)
            {
                // A - Animation
                if (e.Key == Keys.A)
                {
                    PreviewScreen ps = Parent.AnimationPreviews(Texture);
                    ps.OnClick = (s) =>
                    {
                        animation = s.Name;
                        SetSize();
                    };
                    Owner.AddLayer(ps);
                }
                // S - Texture
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
                                if (t.AnimationFromName(animation) is null)
                                {
                                    bool set = false;
                                    for (int i = 0; i < t.Animations.Count; i++)
                                    {
                                        string a = t.Animations.Keys[i];
                                        if (a.ToLower().Contains("trinket"))
                                        {
                                            animation = a;
                                            set = true;
                                            break;
                                        }
                                    }
                                    if (!set)
                                        animation = t.Animations.Keys.FirstOrDefault() ?? "";
                                }
                                SetSize();
                            }
                        }
                    });
                }
                // C - Script
                else if (e.Key == Keys.C)
                {
                    string[] scripts = new string[Owner.Scripts.Count];
                    for (int i = 0; i < scripts.Length; i++)
                    {
                        scripts[i] = Owner.Scripts.Keys[i];
                    }
                    Owner.ShowDialog("Choose a script.", script, scripts, (r, s) =>
                    {
                        if (r && !string.IsNullOrWhiteSpace(s))
                        {
                            script = s;
                            if (!Owner.Scripts.ContainsKey(script))
                            {
                                Owner.Scripts.Add(script, new Script(new Command[] { }, script, ""));
                            }
                        }
                    });
                }
            }
        }

        public override JObject Save()
        {
            JObject ret = getBaseSave(DefaultName);
            ret.Add("Animation", animation);
            ret.Add("Script", script);
            ret.Add("Texture", Texture.Name);
            return ret;
        }
    }
}
