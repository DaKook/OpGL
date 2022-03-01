using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class PlatformTool : EditorTool
    {
        public override string DefaultName => "Vanishing Platform";

        public override string DefaultDescription => "\n" +
            "Use LEFT-CLICK to place a platform, and RIGHT-CLICK to delete platforms.\n" + // Done
            "Use MIDDLE-CLICK on a platform to set its bounds.\n" + // Done
            "Use CONTROL+RIGHT-CLICK on a platform to modify its settigns.\n" + // TO BE ADDED
            "Press A to change the animation, and S to change the texture.\n" + // Done
            "Hold SHIFT and then click and drag to make a custom-length platform. This only works with animations with a width of 8.\n" + // Done
            "Hold Z to flip the platform upside-down, or X to flip the platform to face left.\n" + // Done
            "Press C to toggle whether platforms will inherit room color.\n" + // Done
            "Press V for other settings, such as events and movement."; // Done

        private const string disappearDesc = "Place platforms that disappear when boarded.";
        private const string conveyorDesc = "Place platforms that push crewmen who are on top of them.";
        private const string movingDesc = "Place platforms that move.";

        public override string DefaultKey => "6";

        public override Keys DefaultKeybind => Keys.D6;

        public Texture Texture;
        protected string animation;
        protected string disappearAnimation;
        protected bool separateLeftConveyor;
        protected string leftConveyor = null;
        protected string leftConveyorDisappear = null;
        protected bool disappear;
        protected bool inheritColor;

        private Size animationSize;
        private Size defaultSize;

        protected bool askDirection;
        protected float speed;
        protected bool askConveyor;
        protected float conveyorSpeed;
        protected int askingFor;
        protected Point direction;
        protected Platform inQuestion;

        protected bool creatingPlatform;
        protected Point anchor;

        protected string boardEvent;
        protected string leaveEvent;

        public PlatformTool(LevelEditor parent, Texture texture, int preset) : base(parent)
        {
            Texture = texture;
            SetAnimation("platform1");
            disappearAnimation = "disappear";
            defaultSize = new Size(4, 1);
            conveyorSpeed = 2;
            speed = 2;
            inheritColor = true;
            switch (preset)
            {
                case 0:
                    disappear = true;
                    askDirection = false;
                    askConveyor = false;
                    Description = disappearDesc + DefaultDescription;
                    break;
                case 1:
                    disappear = false;
                    askDirection = false;
                    askConveyor = true;
                    animation = "conveyor1";
                    Keybind = Keys.D7;
                    Key = "7";
                    Name = "Conveyor";
                    Description = conveyorDesc + DefaultDescription;
                    break;
                case 2:
                    disappear = false;
                    askDirection = true;
                    askConveyor = false;
                    Keybind = Keys.D8;
                    Key = "8";
                    Name = "Moving Platform";
                    Description = movingDesc + DefaultDescription;
                    break;
            }
        }

        private void SetAnimation(string a)
        {
            Animation test = Texture.AnimationFromName(a);
            if (test is object)
            {
                animation = a;
                animationSize = test.Hitbox.Size;
            }
        }

        public override void Process()
        {
            base.Process();
            if (askingFor == 0)
            {
                direction = new Point(0, 0);
                if (!PromptImportant)
                    Prompt = animation + " / " + disappearAnimation;
                bool flipX = key(Keys.X);
                bool flipY = key(Keys.Z);
                if (creatingPlatform)
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
                        CreatePlatform(size, flipX, flipY);
                        creatingPlatform = false;
                    }
                }
                else if (shift && animationSize == new Size(8, 8))
                {
                    color = Color.Cyan;
                    size = new Size(1, 1);
                    position = centerOn(mouse, size * 8);
                    if (left)
                    {
                        creatingPlatform = true;
                        anchor = position;
                    }
                }
                else
                {
                    Size s = animationSize;
                    if (s.Width == 8 && s.Height == 8)
                    {
                        s.Width *= defaultSize.Width;
                        s.Height *= defaultSize.Height;
                    }
                    s.Width = (int)Math.Ceiling(s.Width / 8f);
                    s.Height = (int)Math.Ceiling(s.Height / 8f);
                    size = s;
                    position = centerOn(mouse, size * 8);
                    color = Color.Blue;
                    if (left)
                    {
                        CreatePlatform(defaultSize, flipX, flipY);
                    }
                    else if (isRightDown && !ctrl)
                    {
                        List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                        foreach (Sprite sprite in spr)
                        {
                            if (sprite is Platform)
                            {
                                Owner.DeleteSprite(sprite);
                            }
                        }
                    }
                    else if (middle)
                    {
                        List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                        foreach (Sprite sprite in spr)
                        {
                            if (sprite is Platform)
                            {
                                Prompt = "Select bounds rectangle.";
                                PromptImportant = true;
                                TakeInput = true;
                                inQuestion = sprite as Platform;
                                askingFor = 3;
                                break;
                            }
                        }
                    }
                    else if (ctrl && right)
                    {
                        List<Sprite> spr = Parent.Sprites.GetPotentialColliders(mouse.X + CameraX, mouse.Y + CameraY, 2, 2);
                        foreach (Sprite sprite in spr)
                        {
                            if (sprite is Platform)
                            {
                                PlatformSettings(sprite as Platform);
                                break;
                            }
                        }
                    }
                }
            }
            else if (askingFor == 1)
            {
                if (key(Keys.Right))
                    direction.X = 1;
                else if (key(Keys.Left))
                    direction.X = -1;
                if (direction.X != 0)
                {
                    bool arrow = key(Keys.Right) || key(Keys.Left);
                    if (!arrow)
                    {
                        inQuestion.Conveyor = conveyorSpeed * direction.X;
                        if (direction.X == -1)
                        {
                            if (leftConveyor is object && leftConveyorDisappear is object)
                            {
                                inQuestion.Animation = inQuestion.NormalAnimation = Texture.AnimationFromName(leftConveyor);
                                inQuestion.DisappearAnimation = Texture.AnimationFromName(leftConveyorDisappear);
                            }
                            else
                                inQuestion.FlipX = !inQuestion.FlipX;
                        }
                        if (askDirection)
                        {
                            askingFor = 2;
                            Prompt = "Press a direction to move.";
                        }
                        else
                        {
                            inQuestion = null;
                            askingFor = 0;
                            TakeInput = false;
                            PromptImportant = false;
                        }
                    }
                }
            }
            else if (askingFor == 2)
            {
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
                        askingFor = 0;
                        TakeInput = false;
                        PromptImportant = false;
                    }
                }
            }
            else if (askingFor == 3)
            {
                color = Color.Lime;
                if (creatingPlatform)
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
                        askingFor = 0;
                        PromptImportant = false;
                        TakeInput = false;
                        inQuestion = null;
                        creatingPlatform = false;
                    }
                }
                else
                {
                    size = new Size(1, 1);
                    position = centerOn(mouse, size * 8);
                    if (left)
                    {
                        anchor = position;
                        creatingPlatform = true;
                    }
                }
            }
        }

        public override void HandleKey(PassedKeyEvent e)
        {
            if (askingFor == 1 || askingFor == 2)
            {
                if (e.Key == Keys.Escape)
                {
                    Owner.DeleteSprite(inQuestion);
                    askingFor = 0;
                    inQuestion = null;
                    PromptImportant = false;
                    TakeInput = false;
                }
                if (askingFor == 2)
                {
                    if (e.Key == Keys.Enter)
                    {
                        askingFor = 0;
                        inQuestion = null;
                        PromptImportant = false;
                        TakeInput = false;
                    }
                }
            }
            else if (askingFor == 3)
            {
                if (e.Key == Keys.Escape)
                {
                    askingFor = 0;
                    inQuestion = null;
                    PromptImportant = false;
                    TakeInput = false;
                }
                else if (e.Key == Keys.Enter)
                {
                    inQuestion.Bounds = Rectangle.Empty;
                    askingFor = 0;
                    inQuestion = null;
                    PromptImportant = false;
                    TakeInput = false;
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
                    OpenSettings();
                }
            }
        }

        private void ChangeAnimation(bool separates = false, Platform p = null)
        {
            PreviewScreen ps = Parent.AnimationPreviews(p?.Texture ?? Texture);
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
                if (p is object)
                    p.Animation = p.NormalAnimation = p.Texture.AnimationFromName(s.Name);
                else
                {
                    if (separates)
                        leftConveyor = s.Name;
                    else
                        SetAnimation(s.Name);
                }
                info.Text = "Now choose a disappearing animation.";
                info.Right = Game.RESOLUTION_WIDTH - 2;
                info.Bottom = Game.RESOLUTION_HEIGHT - 2;
                ps.FinishOnClick = true;
                ps.OnClick = (a) =>
                {
                    if (p is object)
                        p.DisappearAnimation = p.Texture.AnimationFromName(s.Name);
                    else
                    {
                        if (separates)
                            leftConveyorDisappear = s.Name;
                        else
                            disappearAnimation = a.Name;
                    }
                };
            };
            Owner.AddLayer(ps);
        }

        private void CreatePlatform(Size pSize, bool flipX, bool flipY)
        {
            Platform p = new Platform(0, 0, Texture, Texture.AnimationFromName(animation), 0, 0, 0, disappear, Texture.AnimationFromName(disappearAnimation), 1, 1);
            if (animationSize == new Size(8, 8))
            {
                p.SetSize(pSize.Width, pSize.Height);
            }
            p.CenterX = CenterX + CameraX;
            if (flipY)
            {
                p.Bottom = Bottom + CameraY;
                p.FlipY = true;
            }
            else
                p.Y = position.Y + CameraY;
            p.FlipX = flipX;
            if (inheritColor)
            {
                Color c = Owner.CurrentRoom.Color;
                int r = c.R + (255 - c.R) / 2;
                int g = c.G + (255 - c.G) / 2;
                int b = c.B + (255 - c.B) / 2;
                p.Color = Color.FromArgb(255, r, g, b);
            }
            p.InitializePosition();
            if (boardEvent is object)
                p.BoardEvent = Owner.ScriptFromName(boardEvent);
            if (leaveEvent is object)
                p.LeaveEvent = Owner.ScriptFromName(leaveEvent);
            if (askConveyor)
            {
                Prompt = "Press a direction for the conveyor.";
                PromptImportant = true;
                TakeInput = true;
                askingFor = 1;
                inQuestion = p;
            }
            else if (askDirection)
            {
                Prompt = "Press a direction to move.";
                PromptImportant = true;
                TakeInput = true;
                askingFor = 2;
                inQuestion = p;
            }
            Parent.Sprites.Add(p);
        }

        private void OpenSettings()
        {
            PreviewScreen ps = new PreviewScreen(new Sprite[] { }, null, Owner);
            RectangleSprite rs = new RectangleSprite(0, 0, Game.RESOLUTION_WIDTH, 20);
            rs.Color = Color.Black;
            ps.HudSprites.Add(rs);
            VTextBox tb = new VTextBox(0, 2, Owner.FontTexture, "Settings", Color.Green);
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            tb.Appear();
            ps.HudSprites.Add(tb);
            tb = new VTextBox(0, 2, Owner.FontTexture, "X", Color.White);
            tb.Name = "x";
            tb.Right = Game.RESOLUTION_WIDTH - 2;
            tb.Appear();
            ps.HudSprites.Add(tb);
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, 34, Owner.FontTexture, "Use separate animations for\nleft conveyors: " + (separateLeftConveyor ? "(X) On " : "( ) Off"), Color.White);
            tb.Name = "sep";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, 66, Owner.FontTexture, "Set separate animations...", Color.White);
            tb.Name = "setsep";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, 114, Owner.FontTexture, "Disappear: " + (disappear ? "(X) On " : "( ) Off"), Color.White);
            tb.Name = "dis";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, 138, Owner.FontTexture, "Move: " + (askDirection ? "(X) " + speed.ToString() : "( ) Off"), Color.White);
            tb.Name = "mov";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, 162, Owner.FontTexture, "Conveyor: " + (askConveyor ? "(X) " + conveyorSpeed.ToString() : "( ) Off"), Color.White);
            tb.Name = "cnv";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, 186, Owner.FontTexture, "Board Event: " + (boardEvent ?? "None"), Color.White);
            tb.Name = "evb";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, 210, Owner.FontTexture, "Leave Event: " + (leaveEvent ?? "None"), Color.White);
            tb.Name = "evl";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            ps.FinishOnClick = false;

            ps.OnClick = (s) =>
            {
                switch (s.Name)
                {
                    case "x":
                        {
                            ps.Close();
                            break;
                        }
                    case "sep":
                        {
                            separateLeftConveyor = !separateLeftConveyor;
                            (s as VTextBox).Text = "Use separate animations for\nleft conveyors: " + (separateLeftConveyor ? "(X) On " : "( ) Off");
                            break;
                        }
                    case "setsep":
                        {
                            ChangeAnimation(true);
                            break;
                        }
                    case "dis":
                        {
                            disappear = !disappear;
                            (s as VTextBox).Text = "Disappear: " + (disappear ? "(X) On " : "( ) Off");
                            break;
                        }
                    case "mov":
                        {
                            Owner.ShowDialog("Movement Speed? (Default = 2)", askDirection ? speed.ToString() : "Off", new string[] { }, (r, a) =>
                            {
                                if (r)
                                {
                                    if (float.TryParse(a, out float spd) && spd > 0)
                                    {
                                        speed = spd;
                                        askDirection = true;
                                    }
                                    else
                                    {
                                        askDirection = false;
                                    }
                                    (s as VTextBox).Text = "Move: " + (askDirection ? "(X) " + speed.ToString() : "( ) Off");
                                    s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                }
                            });
                            break;
                        }
                    case "cnv":
                        {
                            Owner.ShowDialog("Conveyor Speed? (Default = 2)", askConveyor ? conveyorSpeed.ToString() : "Off", new string[] { }, (r, a) =>
                            {
                                if (r)
                                {
                                    if (float.TryParse(a, out float spd) && spd > 0)
                                    {
                                        conveyorSpeed = spd;
                                        askConveyor = true;
                                    }
                                    else
                                    {
                                        askConveyor = false;
                                    }
                                    (s as VTextBox).Text = "Conveyor: " + (askConveyor ? "(X) " + conveyorSpeed.ToString() : "( ) Off");
                                    s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                }
                            });
                            break;
                        }
                    case "evb":
                        {
                            string[] options = new string[Owner.Scripts.Count];
                            for (int i = 0; i < options.Length; i++)
                            {
                                options[i] = Owner.Scripts.Keys[i];
                            }
                            Owner.ShowDialog("Set an event when a player boards a platform.", boardEvent ?? "", options, (r, c) =>
                            {
                                if (r)
                                {
                                    if ((c == "None" && !Owner.Scripts.ContainsKey("None")) || c == "")
                                    {
                                        boardEvent = null;
                                    }
                                    else
                                    {
                                        if (!Owner.Scripts.ContainsKey(c))
                                            Owner.Scripts.Add(c, new Script(new Command[] { }, c, ""));
                                        boardEvent = c;
                                    }
                                    (s as VTextBox).Text = "Board Event: " + (boardEvent ?? "None");
                                    s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                }
                            });
                            break;
                        }
                    case "evl":
                        {
                            string[] options = new string[Owner.Scripts.Count];
                            for (int i = 0; i < options.Length; i++)
                            {
                                options[i] = Owner.Scripts.Keys[i];
                            }
                            Owner.ShowDialog("Set an event when a player leaves a platform.", leaveEvent ?? "", options, (r, c) =>
                            {
                                if (r)
                                {
                                    if ((c == "None" && !Owner.Scripts.ContainsKey("None")) || c == "")
                                    {
                                        leaveEvent = null;
                                    }
                                    else
                                    {
                                        if (!Owner.Scripts.ContainsKey(c))
                                            Owner.Scripts.Add(c, new Script(new Command[] { }, c, ""));
                                        leaveEvent = c;
                                    }
                                    (s as VTextBox).Text = "Leave Event: " + (leaveEvent ?? "None");
                                    s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                }
                            });
                            break;
                        }
                }
            };

            Owner.AddLayer(ps);
        }

        private void PlatformSettings(Platform p)
        {
            PreviewScreen ps = new PreviewScreen(new Sprite[] { }, null, Owner);
            RectangleSprite rs = new RectangleSprite(0, 0, Game.RESOLUTION_WIDTH, 20);
            rs.Color = Color.Black;
            ps.HudSprites.Add(rs);
            VTextBox tb = new VTextBox(0, 2, Owner.FontTexture, "Settings", Color.Green);
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            tb.Appear();
            ps.HudSprites.Add(tb);
            tb = new VTextBox(0, 22, Owner.FontTexture, "X", Color.White);
            tb.Name = "x";
            tb.Right = Game.RESOLUTION_WIDTH - 2;
            tb.Visible = true;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, 34, Owner.FontTexture, "Change Color", Color.White);
            tb.Name = "clr";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Change Animations", Color.White);
            tb.Name = "anm";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Disappear: " + (p.CanDisappear ? "(X) On " : "( ) Off"), Color.White);
            tb.Name = "dis";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "X Speed: " + p.XVelocity.ToString(), Color.White);
            tb.Name = "xsp";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Y Speed: " + p.YVelocity.ToString(), Color.White);
            tb.Name = "ysp";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Conveyor: " + p.Conveyor.ToString(), Color.White);
            tb.Name = "cnv";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Length: " + p.Length.ToString(), Color.White);
            tb.Name = "lnt";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Height: " + p.VLength.ToString(), Color.White);
            tb.Name = "hgt";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Sticky: " + (p.Sticky ? "(X) On " : "( ) Off"), Color.White);
            tb.Name = "stk";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            string ood = "Invalid";
            switch (p.State)
            {
                case Tile.TileStates.Normal:
                    ood = "None";
                    break;
                case Tile.TileStates.OneWayU:
                    ood = "Up";
                    break;
                case Tile.TileStates.OneWayD:
                    ood = "Down";
                    break;
                case Tile.TileStates.OneWayR:
                    ood = "Left";
                    break;
                case Tile.TileStates.OneWayL:
                    ood = "Right";
                    break;
            }
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "One-Way Direction: " + ood, Color.White);
            tb.Name = "ood";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Board Event: " + (p.BoardEvent?.Name ?? "None"), Color.White);
            tb.Name = "evb";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);
            tb = new VTextBox(0, tb.Bottom, Owner.FontTexture, "Leave Event: " + (p.LeaveEvent?.Name ?? "None"), Color.White);
            tb.Name = "evl";
            tb.Visible = true;
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ps.Sprites.Add(tb);

            ps.OnClick = (s) =>
            {
                switch (s.Name)
                {
                    case "x":
                        {
                            ps.Close();
                            break;
                        }
                    case "clr":
                        {
                            string v = "White";
                            Color c = p.Color;
                            v = c.Name;
                            if (v == "0")
                            {
                                v = ((int)p.Color.ToArgb()).ToString("X8");
                            }
                            Owner.ShowColorDialog("Platform color", v, (r, c) =>
                            {
                                if (r)
                                {
                                    Color? clr = Owner.GetColor(c);
                                    if (clr.HasValue)
                                    {
                                        p.Color = clr.Value;
                                    }
                                }
                            });
                            break;
                        }
                    case "anm":
                        {
                            ChangeAnimation(false, p);
                            break;
                        }
                    case "dis":
                        {
                            p.CanDisappear = !p.CanDisappear;
                            (s as VTextBox).Text = "Disappear: " + (p.CanDisappear ? "(X) On " : "( ) Off");
                            break;
                        }
                    case "xsp":
                        {
                            Owner.ShowDialog("X Speed", p.XVelocity.ToString(), new string[] { }, (r, v) =>
                            {
                                if (r)
                                {
                                    if (float.TryParse(v, out float vel))
                                    {
                                        p.XVelocity = vel;
                                        (s as VTextBox).Text = "X Speed: " + p.XVelocity.ToString();
                                        s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                    }
                                }
                            });
                            break;
                        }
                    case "ysp":
                        {
                            Owner.ShowDialog("Y Speed", p.YVelocity.ToString(), new string[] { }, (r, v) =>
                            {
                                if (r)
                                {
                                    if (float.TryParse(v, out float vel))
                                    {
                                        p.YVelocity = vel;
                                        (s as VTextBox).Text = "Y Speed: " + p.YVelocity.ToString();
                                        s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                    }
                                }
                            });
                            break;
                        }
                    case "cnv":
                        {
                            Owner.ShowDialog("Conveyor", p.Conveyor.ToString(), new string[] { }, (r, v) =>
                            {
                                if (r)
                                {
                                    if (float.TryParse(v, out float vel))
                                    {
                                        p.Conveyor = vel;
                                        (s as VTextBox).Text = "Conveyor: " + p.Conveyor.ToString();
                                        s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                    }
                                }
                            });
                            break;
                        }
                    case "lnt":
                        {
                            Owner.ShowDialog("Length", p.Length.ToString(), new string[] { }, (r, v) =>
                            {
                                if (r)
                                {
                                    if (int.TryParse(v, out int vel))
                                    {
                                        p.Length = vel;
                                        (s as VTextBox).Text = "Length: " + p.Length.ToString();
                                        s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                    }
                                }
                            });
                            break;
                        }
                    case "hgt":
                        {
                            Owner.ShowDialog("Height", p.VLength.ToString(), new string[] { }, (r, v) =>
                            {
                                if (r)
                                {
                                    if (int.TryParse(v, out int vel))
                                    {
                                        p.VLength = vel;
                                        (s as VTextBox).Text = "Height: " + p.VLength.ToString();
                                        s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                    }
                                }
                            });
                            break;
                        }
                    case "stk":
                        {
                            p.Sticky = !p.Sticky;
                            (s as VTextBox).Text = "Sticky: " + (p.Sticky ? "(X) On " : "( ) Off");
                            break;
                        }
                    case "ood":
                        {
                            string ood = "Invalid";
                            switch (p.State)
                            {
                                case Tile.TileStates.Normal:
                                    ood = "None";
                                    break;
                                case Tile.TileStates.OneWayU:
                                    ood = "Up";
                                    break;
                                case Tile.TileStates.OneWayD:
                                    ood = "Down";
                                    break;
                                case Tile.TileStates.OneWayR:
                                    ood = "Left";
                                    break;
                                case Tile.TileStates.OneWayL:
                                    ood = "Right";
                                    break;
                            }
                            Owner.ShowDialog("One-Way Direction", ood, new string[] { "None", "Up", "Down", "Left", "Right" }, (r, d) =>
                              {
                                  if (r)
                                  {
                                      switch (d)
                                      {
                                          case "None":
                                              p.State = Tile.TileStates.Normal;
                                              break;
                                          case "Up":
                                              p.State = Tile.TileStates.OneWayU;
                                              break;
                                          case "Down":
                                              p.State = Tile.TileStates.OneWayD;
                                              break;
                                          case "Left":
                                              p.State = Tile.TileStates.OneWayL;
                                              break;
                                          case "Right":
                                              p.State = Tile.TileStates.OneWayR;
                                              break;
                                      }
                                      switch (p.State)
                                      {
                                          case Tile.TileStates.Normal:
                                              ood = "None";
                                              break;
                                          case Tile.TileStates.OneWayU:
                                              ood = "Up";
                                              break;
                                          case Tile.TileStates.OneWayD:
                                              ood = "Down";
                                              break;
                                          case Tile.TileStates.OneWayR:
                                              ood = "Left";
                                              break;
                                          case Tile.TileStates.OneWayL:
                                              ood = "Right";
                                              break;
                                      }
                                      (s as VTextBox).Text = "One-Way Direction: " + ood;
                                      s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                  }
                              });
                            break;
                        }
                    case "evb":
                        {
                            string[] options = new string[Owner.Scripts.Count];
                            for (int i = 0; i < options.Length; i++)
                            {
                                options[i] = Owner.Scripts.Keys[i];
                            }
                            Owner.ShowDialog("Board Event", p.BoardEvent?.Name ?? "None", options, (r, c) =>
                            {
                                if (r)
                                {
                                    if ((c == "None" && !Owner.Scripts.ContainsKey("None")) || c == "")
                                    {
                                        p.BoardEvent = null;
                                    }
                                    else
                                    {
                                        if (!Owner.Scripts.ContainsKey(c))
                                            Owner.Scripts.Add(c, new Script(new Command[] { }, c, ""));
                                        p.BoardEvent = Owner.ScriptFromName(c);
                                    }
                                    (s as VTextBox).Text = "Board Event: " + (p.BoardEvent?.Name ?? "None");
                                    s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                }
                            });
                            break;
                        }
                    case "evl":
                        {
                            string[] options = new string[Owner.Scripts.Count];
                            for (int i = 0; i < options.Length; i++)
                            {
                                options[i] = Owner.Scripts.Keys[i];
                            }
                            Owner.ShowDialog("Leave Event", p.LeaveEvent?.Name ?? "", options, (r, c) =>
                            {
                                if (r)
                                {
                                    if ((c == "None" && !Owner.Scripts.ContainsKey("None")) || c == "")
                                    {
                                        p.LeaveEvent = null;
                                    }
                                    else
                                    {
                                        if (!Owner.Scripts.ContainsKey(c))
                                            Owner.Scripts.Add(c, new Script(new Command[] { }, c, ""));
                                        p.LeaveEvent = Owner.ScriptFromName(c);
                                    }
                                    (s as VTextBox).Text = "Leave Event: " + (p.LeaveEvent?.Name ?? "None");
                                    s.CenterX = Game.RESOLUTION_WIDTH / 2;
                                }
                            });
                            break;
                        }
                }
            };

            ps.MaxScroll = tb.Bottom + 8 - Game.RESOLUTION_HEIGHT;
            ps.FinishOnClick = false;
            Owner.AddLayer(ps);
        }

        public override JObject Save()
        {
            throw new NotImplementedException();
        }
    }
}
