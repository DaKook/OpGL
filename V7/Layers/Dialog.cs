using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using System.Drawing;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace V7
{
    class Dialog : SpritesLayer
    {
        private BoxSprite dialogSprite;
        private StringDrawable input;
        private StringDrawable promptText;
        private string[] choices;
        private List<string> availableChoices;
        private List<StringDrawable> choiceSprites;
        private int selectedChoice = -1;
        private int maxChoices;
        private int choiceScroll;
        private string currentText;
        private bool exiting;
        private int lastClick = -60;
        private RectangleSprite colorPreview;

        public SpriteCollection Sprites { get; private set; }

        public Dialog(Game game, string prompt, string defaultAnswer, string[] choices, Action<bool, string> closed, int width = 36, int height = 26, bool colorDialog = false)
        {
            Owner = game;
            Sprites = new SpriteCollection();
            dialogSprite = new BoxSprite(0, 0, Owner.TextureFromName("dialog"), width, height) { Layer = 0 };
            dialogSprite.CenterX = Game.RESOLUTION_WIDTH / 2;
            dialogSprite.CenterY = Game.RESOLUTION_HEIGHT / 2;
            promptText = new StringDrawable(dialogSprite.X + 8, dialogSprite.Y + 24, Owner.NonMonoFont, "", Color.Black) { Layer = 1 };
            promptText.MaxWidth = (int)dialogSprite.Width - 16;
            promptText.Text = prompt;
            input = new StringDrawable(promptText.X, promptText.Bottom + 8, Owner.FontTexture, "", Color.Black) { Layer = 2 };
            if (colorDialog)
            {
                colorPreview = new RectangleSprite(dialogSprite.X + 16, input.Bottom + 8, dialogSprite.Width - 32, dialogSprite.Bottom - input.Bottom - 24) { Layer = 1 };
                choices = null;
                Sprites.Add(colorPreview);
            }
            Darken = 0;
            FreezeBelow = true;
            Owner.StartTyping(input, (r, t) => 
            {
                ProcessAnyway = true;
                ExitLayer();
                exiting = true;
                closed(r, t);
            });
            input.SelectionLength = defaultAnswer?.Length ?? 0;
            input.SelectionStart = 0;
            input.Text = defaultAnswer;
            this.choices = choices;
            choiceSprites = new List<StringDrawable>();
            Sprites.Add(dialogSprite);
            Sprites.Add(promptText);
            Sprites.Add(input);
            Sprites.Color = Color.FromArgb(0, 255, 255, 255);
            maxChoices = (int)(dialogSprite.Bottom - input.Bottom - 16) / 8;
            currentText = defaultAnswer;
            RefreshChoices(null);
        }
        private void RefreshChoices(string s = null)
        {
            currentText = input.Text;
            foreach (StringDrawable choice in choiceSprites)
            {
                Sprites.Remove(choice);
                choice.Dispose();
            }
            choiceSprites.Clear();
            if (s is object && choices is object)
            {
                availableChoices = choices.ToList();
                availableChoices.RemoveAll((c) => !c.ToLower().Contains(s.ToLower()));
                int v = 0;
                for (int i = 0; i < availableChoices.Count; i++)
                {
                    if (availableChoices[i].StartsWith(s))
                    {
                        v = i;
                        break;
                    }
                }
                selectedChoice = v;
            }
            if (availableChoices is null) availableChoices = choices?.ToList() ?? new List<string>();
            int y = (int)dialogSprite.Bottom - 16 - (maxChoices * 8);
            for (int i = choiceScroll; i < choiceScroll + maxChoices && i < availableChoices.Count; i++)
            {
                StringDrawable ch = new StringDrawable(24, y, Owner.FontTexture, availableChoices[i], i == selectedChoice ? Color.Blue : Color.Black);
                ch.Layer = 6;
                Sprites.Add(ch);
                choiceSprites.Add(ch);
                y += 8;
            }
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            if (input.Within(0, Owner.MouseY, Game.RESOLUTION_WIDTH, 1))
            {
                int ind = input.GetIndexFromPoint(Owner.MouseX - input.X + 8, Owner.MouseY - input.Y, 1);
                input.SelectionStart = ind;
                input.SelectionLength = 0;
                input.SelectingFromLeft = true;
                input.Text = input.Text;
            }
            else if (Owner.MouseY > input.Bottom && Owner.MouseY < input.Bottom + maxChoices * 8)
            {
                int time = Owner.FrameCount - lastClick;
                int sel = (int)(Owner.MouseY - input.Bottom) / 8 + choiceScroll;
                if (time < 20 && sel == selectedChoice)
                {
                    string s = availableChoices[selectedChoice];
                    input.SelectionStart = s.Length;
                    input.SelectionLength = 0;
                    input.SelectingFromLeft = true;
                    input.Text = s;
                    RefreshChoices();
                }
                else
                {
                    selectedChoice = sel;
                    RefreshChoices();
                    lastClick = Owner.FrameCount;
                }
            }
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (e.Key == Keys.Down && availableChoices is object && availableChoices.Count > 0)
            {
                selectedChoice += 1;
                if (selectedChoice >= availableChoices.Count)
                {
                    selectedChoice = 0;
                    choiceScroll = 0;
                }
                if (selectedChoice > choiceScroll + maxChoices - 1)
                {
                    choiceScroll = selectedChoice - (maxChoices - 1);
                }
                string s = availableChoices[selectedChoice];
                input.SelectionStart = s.Length;
                input.SelectionLength = 0;
                input.SelectingFromLeft = true;
                input.Text = s;
                RefreshChoices();
            }
            else if (e.Key == Keys.Up && availableChoices is object && availableChoices.Count > 0)
            {
                selectedChoice -= 1;
                if (selectedChoice < 0)
                {
                    selectedChoice = availableChoices.Count - 1;
                    choiceScroll = Math.Max(0, availableChoices.Count - maxChoices);
                }
                if (selectedChoice < choiceScroll)
                {
                    choiceScroll = selectedChoice;
                }
                string s = availableChoices[selectedChoice];
                input.SelectionStart = s.Length;
                input.SelectionLength = 0;
                input.SelectingFromLeft = true;
                input.Text = s;
                RefreshChoices();
            }
            else if (e.Key == Keys.Tab && availableChoices is object && availableChoices.Count > 0)
            {
                string s = availableChoices[selectedChoice];
                input.SelectionStart = s.Length;
                input.SelectionLength = 0;
                input.SelectingFromLeft = true;
                input.Text = s;
                RefreshChoices();
            }
        }

        public override void HandleWheel(int e)
        {
            int cs = choiceScroll;
            if (e < 0)
            {
                choiceScroll += 2;
            }
            else if (e > 0)
            {
                choiceScroll -= 2;
            }
            if (choiceScroll < 0 || availableChoices.Count < maxChoices)
                choiceScroll = 0;
            else if (choiceScroll > availableChoices.Count - maxChoices)
                choiceScroll = availableChoices.Count - maxChoices;
            if (cs != choiceScroll)
                RefreshChoices();
        }

        public override void Process()
        {
            if (Darken < 0.5f && !exiting)
                Darken += 0.025f;
            else if (Darken > 0 && exiting)
                Darken -= 0.025f;
            Darken = (float)Math.Round(Darken, 3);
            Sprites.Color = Color.FromArgb((int)(Darken * 510), 255, 255, 255);
            if (Darken <= 0)
            {
                FinishLayer();
                return;
            }
            if (currentText != input.Text)
            {
                currentText = input.Text;
                RefreshChoices(currentText);
                if (colorPreview is object)
                {
                    Color? c = Owner.GetColor(input.Text ?? "");
                    if (c.HasValue)
                        colorPreview.Color = c.Value;
                }
            }
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            float offset = (0.5f - Darken) * 100;
            if (exiting)
                offset *= -1;
            baseCamera = Matrix4.CreateTranslation(0, offset, 0) * baseCamera;
            GL.UniformMatrix4(viewMatrixLocation, false, ref baseCamera);
            Sprites.Render(Owner.FrameCount);
        }

        public override void Dispose()
        {
            Sprites.Dispose();
        }
    }
}
