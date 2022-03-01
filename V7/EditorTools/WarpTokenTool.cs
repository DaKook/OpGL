using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class WarpTokenTool : EditorTool
    {
        public override string DefaultName => "Warp Token";

        public override string DefaultDescription => "nah";

        public override string DefaultKey => "U";

        public override Keys DefaultKeybind => Keys.U;

        public Texture Texture;
        private string animation;
        private Size animationSize;

        private int inQuestion;

        public WarpTokenTool(LevelEditor parent, Texture texture) : base(parent)
        {
            Texture = texture;
            SetAnimation("WarpToken");
            inQuestion = -1;
            Sprites = new SpriteCollection();
        }

        public override void Process()
        {
            base.Process();
            size = new Size((int)Math.Ceiling(animationSize.Width / 8f), (int)Math.Ceiling(animationSize.Height / 8f));
            position = centerOn(mouse, size * 8);
            color = Color.Blue;
            if (inQuestion >= 0)
            {
                if (left)
                {
                    PointF p = new PointF(position.X + (size.Width * 8 - animationSize.Width) / 2f + CameraX, position.Y + (size.Height * 8 - animationSize.Height) / 2f + CameraY);
                    WarpToken.WarpData wd = Owner.Warps[inQuestion];
                    wd.OutRoom = new Point(Owner.CurrentRoom.X, Owner.CurrentRoom.Y);
                    wd.Out = p;
                    Owner.Warps[inQuestion] = wd;
                    if (wd.InRoom == new Point(Owner.CurrentRoom.X, Owner.CurrentRoom.Y))
                    {
                        List<Sprite> sprs = Parent.Sprites.GetPotentialColliders(wd.In.X, wd.In.Y, 1, 1);
                        for (int i = 0; i < sprs.Count; i++)
                        {
                            if (sprs[i] is WarpToken && (sprs[i] as WarpToken).ID == inQuestion)
                            {
                                (sprs[i] as WarpToken).Data = wd;
                            }
                        }
                    }
                    WarpTokenOutput wto = new WarpTokenOutput(p.X, p.Y, Texture, Texture.AnimationFromName(animation), wd, inQuestion);
                    PromptImportant = TakeInput = false;
                    Prompt = "";
                    inQuestion = -1;
                    Parent.Sprites.Add(wto);
                }
            }
            else
            {
                if (left)
                {
                    WarpToken wt = new WarpToken(0, 0, Texture, Texture.AnimationFromName(animation), 0, 0, 0, 0, Owner, WarpToken.FlipSettings.Unflip);
                    wt.CenterX = CenterX + CameraX;
                    wt.CenterY = CenterY + CameraY;
                    wt.ID = Owner.GetNextWarpID();
                    Owner.Warps.Add(wt.ID, new WarpToken.WarpData(wt, Owner.CurrentRoom.X, Owner.CurrentRoom.Y));
                    Prompt = "Set an output for the warp token.";
                    PromptImportant = TakeInput = true;
                    inQuestion = wt.ID;
                    Parent.Sprites.Add(wt);
                }
                else if (isRightDown)
                {
                    List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is WarpToken)
                        {
                            Owner.DeleteSprite(sprite);
                        }
                    }
                }
            }
            for (int i = 0; i < Sprites.Count; i++)
            {
                Sprites[i].Dispose();
            }
            Sprites.Clear();
            List<Sprite> sprites = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 1, 1);
            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] is WarpToken)
                {
                    WarpToken w = sprites[i] as WarpToken;
                    if (w.ID == inQuestion || !Owner.Warps.ContainsKey(w.ID)) continue;
                    StringDrawable id = new StringDrawable(w.X - CameraX, w.Y - 8 - CameraY, Owner.FontTexture, w.ID.ToString(), Color.White);
                    StringDrawable room = new StringDrawable(w.X - CameraX, w.Bottom - CameraY, Owner.FontTexture, w.Data.OutRoom.X.ToString() + ", " + w.Data.OutRoom.Y.ToString(), Color.White);
                    Sprites.Add(id);
                    Sprites.Add(room);
                    Sprites.Add(new RectangleSprite(w.X - CameraX, w.Y - CameraY, w.Width, 1) { Color = Color.Cyan });
                    Sprites.Add(new RectangleSprite(w.X - CameraX, w.Y - CameraY, 1, w.Height) { Color = Color.Cyan });
                    Sprites.Add(new RectangleSprite(w.X - CameraX, w.Bottom - 1 - CameraY, w.Width, 1) { Color = Color.Cyan });
                    Sprites.Add(new RectangleSprite(w.Right - 1 - CameraX, w.Y - CameraY, 1, w.Height) { Color = Color.Cyan });
                    if (w.Data.OutRoom == new Point(Owner.CurrentRoom.X, Owner.CurrentRoom.Y))
                    {
                        Sprites.Add(new RectangleSprite(w.Data.Out.X - CameraX, w.Data.Out.Y - CameraY, w.Width, 1) { Color = Color.Cyan });
                        Sprites.Add(new RectangleSprite(w.Data.Out.X - CameraX, w.Data.Out.Y - CameraY, 1, w.Height) { Color = Color.Cyan });
                        Sprites.Add(new RectangleSprite(w.Data.Out.X - CameraX, w.Data.Out.Y + w.Height - 1 - CameraY, w.Width, 1) { Color = Color.Cyan });
                        Sprites.Add(new RectangleSprite(w.Data.Out.X + w.Width - 1 - CameraX, w.Data.Out.Y - CameraY, 1, w.Height) { Color = Color.Cyan });
                    }
                }
                else if (sprites[i] is WarpTokenOutput)
                {
                    WarpTokenOutput w = sprites[i] as WarpTokenOutput;
                    if (!Owner.Warps.ContainsKey(w.ID)) continue;
                    WarpToken.WarpData data = Owner.Warps[w.ID];
                    StringDrawable id = new StringDrawable(w.X - CameraX, w.Y - 8 - CameraY, Owner.FontTexture, w.ID.ToString(), Color.White);
                    StringDrawable room = new StringDrawable(w.X - CameraX, w.Bottom - CameraY, Owner.FontTexture, data.InRoom.X.ToString() + ", " + data.InRoom.Y.ToString(), Color.White);
                    Sprites.Add(id);
                    Sprites.Add(room);
                    Sprites.Add(new RectangleSprite(w.X - CameraX, w.Y - CameraY, w.Width, 1) { Color = Color.Cyan });
                    Sprites.Add(new RectangleSprite(w.X - CameraX, w.Y - CameraY, 1, w.Height) { Color = Color.Cyan });
                    Sprites.Add(new RectangleSprite(w.X - CameraX, w.Bottom - 1 - CameraY, w.Width, 1) { Color = Color.Cyan });
                    Sprites.Add(new RectangleSprite(w.Right - 1 - CameraX, w.Y - CameraY, 1, w.Height) { Color = Color.Cyan });
                    if (data.InRoom == new Point(Owner.CurrentRoom.X, Owner.CurrentRoom.Y))
                    {
                        Sprites.Add(new RectangleSprite(data.In.X - CameraX, data.In.Y - CameraY, w.Width, 1) { Color = Color.Cyan });
                        Sprites.Add(new RectangleSprite(data.In.X - CameraX, data.In.Y - CameraY, 1, w.Height) { Color = Color.Cyan });
                        Sprites.Add(new RectangleSprite(data.In.X - CameraX, data.In.Y + w.Height - 1 - CameraY, w.Width, 1) { Color = Color.Cyan });
                        Sprites.Add(new RectangleSprite(data.In.X + w.Width - 1 - CameraX, data.In.Y - CameraY, 1, w.Height) { Color = Color.Cyan });
                    }
                }
            }
        }

        public override void HandleKey(PassedKeyEvent e)
        {
            if (inQuestion >= 0)
            {
                if (!e.Alt && !e.Control && !e.Shift)
                {
                    if (e.Key == Keys.Up || e.Key == Keys.Down || e.Key == Keys.Left || e.Key == Keys.Right || e.Key == Keys.M)
                        e.Pass = true;
                }
                else if (e.Control && !e.Alt && !e.Shift)
                {
                    if (e.Key == Keys.R)
                        e.Pass = true;
                }
            }
        }

        private void SetAnimation(string a)
        {
            Animation anim;
            if ((anim = Texture.AnimationFromName(a)) is object)
            {
                animation = a;
                animationSize = anim.Hitbox.Size;
            }
        }

        public override JObject Save()
        {
            throw new NotImplementedException();
        }
    }
}
