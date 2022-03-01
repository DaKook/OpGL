using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    internal class EnemyTool : EditorTool
    {
        public override string DefaultName => "Enemy";

        public override string DefaultDescription => "Place enemies that move and kill crewmen.\n" +
            "Use LEFT-CLICK to place an enemy, and RIGHT-CLICK to delete enemies.\n" + // DONE
            "Use MIDDLE-CLICK on an enemy to set its bounds.\n" + // DONE
            "Use CONTROL+RIGHT-CLICK on an enemy to modify its settings.\n" + // 
            "Press A to change the animation, and S to change the texture.\n" + // DONE
            "Hold Z to flip the enemy upside-down, or X to flip the enemy to face left.\n" + // 
            "Press C to toggle whether enemies will inherit room color.\n" + // DONE
            "Press V to change default speed."; // DONE

        public override string DefaultKey => "9";

        public override Keys DefaultKeybind => Keys.D9;

        public Texture Texture;
        private string animation;
        private Size animationSize;
        private bool inheritColor;
        private bool askingDir;
        private bool setBounds;
        private Enemy inQuestion;
        private Point direction;
        private bool settingRectangle;
        private Point anchor;
        private float speed;
        
        public EnemyTool(LevelEditor parent, Texture texture) : base(parent)
        {
            Texture = texture;
            SetAnimation("Enemy1");
            inheritColor = true;
            speed = 2;
        }

        private void SetAnimation(string a)
        {
            Animation anim;
            if (Texture.Animations.TryGetValue(a, out anim))
            {
                animation = a;
                animationSize = anim.Hitbox.Size;
            }
        }

        public override void Process()
        {
            base.Process();
            if (askingDir)
            {
                Prompt = "Press a direction to move.";
                PromptImportant = true;
                if (key(Keys.Right))
                    direction.X = 1;
                else if (key(Keys.Left))
                    direction.X = -1;
                if (key(Keys.Down))
                    direction.Y = 1;
                else if (key(Keys.Up))
                    direction.Y = -1;
                if (direction.X != 0 || direction.Y != 0)
                {
                    bool arrow = key(Keys.Right) || key(Keys.Left) || key(Keys.Up) || key(Keys.Down);
                    if (!arrow)
                    {
                        inQuestion.XVelocity = direction.X * speed;
                        inQuestion.YVelocity = direction.Y * speed;
                        inQuestion = null;
                        askingDir = false;
                        TakeInput = false;
                    }
                }
            }
            else if (setBounds)
            {
                Prompt = "Select bounds rectangle.";
                PromptImportant = true;
                color = Color.Lime;
                if (settingRectangle)
                {
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
                        inQuestion.Bounds = new Rectangle(new Point((int)CameraX + position.X - (int)inQuestion.X, (int)CameraY + position.Y - (int)inQuestion.Y), size * 8);
                        setBounds = false;
                        TakeInput = false;
                        inQuestion = null;
                        settingRectangle = false;
                    }
                }
                else
                {
                    size = new Size(1, 1);
                    position = centerOn(mouse, size * 8);
                    if (left)
                    {
                        anchor = position;
                        settingRectangle = true;
                    }
                }
            }
            else
            {
                direction = new Point(0, 0);
                Prompt = animation;
                PromptImportant = false;
                size = new Size((int)Math.Ceiling(animationSize.Width / 8f), (int)Math.Ceiling(animationSize.Height / 8f));
                position = centerOn(mouse, size * 8);
                color = Color.Blue;
                if (left)
                {
                    Animation a = Texture.AnimationFromName(animation);
                    if (a is object)
                    {
                        inQuestion = new Enemy(0, 0, Texture, a, 0, 0, inheritColor ? Owner.CurrentRoom.Color : Color.White);
                        inQuestion.CenterX = CenterX + CameraX;
                        inQuestion.CenterY = CenterY + CameraY;
                        askingDir = true;
                        TakeInput = true;
                        inQuestion.InitializePosition();
                        Parent.Sprites.Add(inQuestion);
                    }
                    else
                    {
                        Owner.Shake(15);
                        Parent.Notify("Invalid animation! Press\nA to change animation!", position.X, Bottom, Color.FromArgb(255, 255, 55, 55), 90);
                    }
                }
                else if (middle)
                {
                    List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is Enemy)
                        {
                            TakeInput = true;
                            inQuestion = sprite as Enemy;
                            setBounds = true;
                            break;
                        }
                    }
                }
                else if (isRightDown && !ctrl)
                {
                    List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                    foreach (Sprite sprite in spr)
                    {
                        if (sprite is Enemy)
                        {
                            Owner.DeleteSprite(sprite);
                        }
                    }
                }
            }
        }

        public override void HandleKey(PassedKeyEvent e)
        {
            if (settingRectangle)
            {
                if (e.Key == Keys.Enter)
                {
                    inQuestion.Bounds = Rectangle.Empty;
                    settingRectangle = false;
                    TakeInput = false;
                    inQuestion = null;
                }
                else if (e.Key == Keys.Escape)
                {
                    settingRectangle = false;
                    TakeInput = false;
                    inQuestion = null;
                }
            }
            else if (askingDir)
            {
                if (e.Key == Keys.Enter)
                {
                    askingDir = false;
                    TakeInput = false;
                    inQuestion = null;
                }
                else if (e.Key == Keys.Escape)
                {
                    Owner.DeleteSprite(inQuestion);
                    askingDir = false;
                    TakeInput = false;
                    inQuestion = null;
                }
            }
            else
            {
                if (e.Key == Keys.A)
                {
                    ChangeAnimation();
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
                            if (Texture.Name != s && Owner.Textures.ContainsKey(s))
                            {
                                Texture = Owner.TextureFromName(s);
                                ChangeAnimation();
                            }
                        }
                    });
                }
                else if (e.Key == Keys.C)
                {
                    inheritColor = !inheritColor;
                }
                else if (e.Key == Keys.V)
                {
                    Owner.ShowDialog("Enemy speed", speed.ToString(), new string[] { }, (r, s) =>
                    {
                        if (r)
                        {
                            if (float.TryParse(s, out float v))
                            {
                                speed = v;
                            }
                        }
                    });
                }
            }
        }

        private void ChangeAnimation(Enemy e = null)
        {
            PreviewScreen ps = Parent.AnimationPreviews(e?.Texture ?? Texture);
            VTextBox info = new VTextBox(0, 0, Owner.FontTexture, "Choose an animation.", Color.Green);
            info.Visible = true;
            info.Right = Game.RESOLUTION_WIDTH - 2;
            info.Bottom = Game.RESOLUTION_HEIGHT - 2;
            ps.HudSprites.Add(info);
            info.Appear();
            ps.MaxScroll += info.Height;
            ps.FinishOnClick = false;
            ps.OnClick = (s) =>
            {
                if (e is object)
                    e.Animation = e.Texture.AnimationFromName(s.Name);
                else
                {
                    SetAnimation(s.Name);
                }
            };
            Owner.AddLayer(ps);
        }

        public override JObject Save()
        {
            throw new NotImplementedException();
        }
    }
}
