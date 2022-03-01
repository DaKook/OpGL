using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class CrewmanTool : EditorTool
    { 
        public override string DefaultName => "Crewman";

        public override string DefaultDescription => "nah";

        public override string DefaultKey => "O";

        public override Keys DefaultKeybind => Keys.O;



        public CrewmanTool(LevelEditor parent) : base(parent)
        {
            currentTexture = Owner.TextureFromName("vermilion") as CrewmanTexture;
            name = "Vermilion";
        }

        private CrewmanTexture currentTexture;
        private string name;

        public override void Process()
        {
            base.Process();
            Prompt = name;
            if (currentTexture.Name.ToLower() != name.ToLower())
            {
                Prompt += " (" + currentTexture.Name + ")";
            }
            size = new Size(2, 3);
            position = centerOn(mouse, size * 8);
            color = Color.Blue;
            if (left)
            {
                Crewman c;
                if ((c = Owner.SpriteFromName(name) as Crewman) is null)
                {
                    if (Owner.UserAccessSprites.ContainsKey(name))
                    {
                        Owner.Shake(15);
                        Parent.Notify("Name points to a sprite\nthat is not a crewman!", position.X, Bottom, Color.FromArgb(255, 255, 55, 55), 120);
                        return;
                    }
                    c = new Crewman(0, 0, currentTexture, Owner, name);
                    Owner.UserAccessSprites.Add(name, c);
                }
                if (!Parent.Sprites.Contains(c))
                {
                    Parent.Sprites.Add(c);
                }
                c.CenterX = CenterX + CameraX;
                bool flipY = key(Keys.Z), flipX = key(Keys.X);
                if (flipY)
                {
                    c.Y = position.Y + CameraY;
                    c.Gravity = -c.Gravity;
                }
                else
                    c.Bottom = Bottom + CameraY;
                c.FlipX = flipX;
                c.FlipY = flipY;
            }
            else if (right)
            {
                List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                foreach (Sprite sprite in spr)
                {
                    if (sprite is Crewman)
                    {
                        List<VMenuItem> items = new List<VMenuItem>();
                        if (sprite != Owner.ActivePlayer)
                            items.Add(new VMenuItem("Delete " + sprite.Name, () =>
                            {
                                Owner.UserAccessSprites.Remove(sprite.Name);
                                Owner.DeleteSprite(sprite);
                            }));
                        items.Add(new VMenuItem("Remove from room", () =>
                        {
                            Owner.DeleteSprite(sprite);
                        }));
                        ContextMenu cm = new ContextMenu(mouse.X, mouse.Y, items, Owner);
                        Owner.AddLayer(cm);
                        break;
                    }
                }
            }
        }

        public override void HandleKey(PassedKeyEvent e)
        {
            if (e.Key == Keys.S)
            {
                PreviewScreen ps = ShowCrewmen(true);
                ps.OnClick = (s) =>
                {
                    if (s.Name == "add crewman")
                    {
                        AddCrewman();
                    }
                    else
                    {
                        name = s.Name;
                        if (Owner.UserAccessSprites.ContainsKey(name))
                        {
                            currentTexture = Owner.SpriteFromName(name).Texture as CrewmanTexture;
                        }
                    }
                };
                Owner.AddLayer(ps);
            }
        }

        private void AddCrewman(bool set = true)
        {
            List<string> options = new List<string>();
            for (int i = 0; i < Owner.Textures.Count; i++)
            {
                if (Owner.Textures.Values[i] is CrewmanTexture)
                {
                    options.Add(Owner.Textures.Keys[i]);
                }
            }
            Owner.ShowDialog("Choose a crewman texture", "", options.ToArray(), (r, s) =>
            {
                if (r)
                {
                    CrewmanTexture t = Owner.TextureFromName(s) as CrewmanTexture;
                    if (t is object)
                    {
                        SetTexture(t);
                    }
                }
            });
        }

        private void SetTexture(CrewmanTexture t)
        {
            currentTexture = t;
            string nm = t.Name;
            nm = nm[0].ToString().ToUpper() + nm.Substring(1);
            if (Owner.UserAccessSprites.ContainsKey(nm))
            {
                Owner.ShowDialog("There is already a crewman with that name. Choose a new name.", nm, new string[] { }, (r, s) =>
                {
                    if (r)
                    {
                        if (s != nm)
                        {
                            SetTexture(t);
                        }
                        else
                        {
                            name = s;
                        }
                    }
                    else
                        name = s;
                });
            }
            else
                name = nm;
        }

        private PreviewScreen ShowCrewmen(bool add = false)
        {
            PreviewScreen ret = new PreviewScreen(new Sprite[] { }, null, Owner);
            int y = 20;
            for (int i = 0; i < Owner.UserAccessSprites.Count; i++)
            {
                Crewman c = Owner.UserAccessSprites.Values[i] as Crewman;
                if (c is null) continue;
                VTextBox tb = new VTextBox(20, y, Owner.FontTexture, "\n    " + Owner.UserAccessSprites.Keys[i] + "\n", Color.White);
                tb.Visible = true;
                tb.Name = Owner.UserAccessSprites.Keys[i];
                tb.Layer = 0;
                ret.Sprites.Add(tb);
                Sprite cm = new Sprite(28, y + 11, c.Texture, c.StandingAnimation);
                cm.Layer = 1;
                ret.Sprites.Add(cm);
                y += (int)tb.Height + 2;
            }
            if (add)
            {
                VTextBox tb = new VTextBox(20, y, Owner.FontTexture, "\n +  Add crewman\n", Color.White);
                tb.Visible = true;
                tb.Name = "add crewman";
                tb.Layer = 0;
                ret.Sprites.Add(tb);
                y += (int)tb.Height + 2;
            }
            ret.MaxScroll = Math.Max(y - Game.RESOLUTION_HEIGHT + 18, 0);
            return ret;
        }

        public override JObject Save()
        {
            throw new NotImplementedException();
        }
    }
}
