using System;
using System.Collections.Generic;
//using System.Drawing;
using OpenTK;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace V7
{
    public class StringHighlighter : SpritesLayer
    {
        StringDrawable white;
        StringDrawable command;
        StringDrawable delim;
        StringDrawable keyword;
        StringDrawable number;
        StringDrawable keyword2;
        StringDrawable selection;
        SortedList<string, StringDrawable> colors;
        StringDrawable choices;
        RectangleSprite choicesBG;
        RectangleSprite choicesSel;
        RectangleSprite descriptionBG;
        StringDrawable description;
        public bool ShowingChoices;
        public bool ChoicesVisible;
        public int SelectedChoice = 0;
        string[] currentChoices;
        int choiceScroll;
        public List<string> Choices;
        private Command.ArgTypes arg;
        private string argText;
        private StringDrawable whText;

        private Script s;

        public float ScrollX, ScrollY, Zoom = 1;

        int xPosition, yPosition;
        FontTexture tex;

        int start = 0;
        int end = 0;
        float size;

        public StringHighlighter(int xPos, int yPos, FontTexture texture, Game game, Script script, float size = 1)
        {
            Darken = 1f;
            FreezeBelow = true;
            Owner = game;
            xPosition = xPos;
            yPosition = yPos;
            tex = texture;
            this.size = size;
            AllTogether = new SpriteCollection();
            colors = new SortedList<string, StringDrawable>();
            white = new StringDrawable(xPos, yPos, texture, "", Color.White);
            white.Size = size;
            command = new StringDrawable(xPos, yPos, texture, "", Color.FromArgb(255, 137, 113, 255));
            command.Size = size;
            delim = new StringDrawable(xPos, yPos, texture, "", Color.Purple);
            delim.Size = size;
            keyword = new StringDrawable(xPos, yPos, texture, "", Color.Blue);
            keyword.Size = size;
            number = new StringDrawable(xPos, yPos, texture, "", Color.Yellow);
            number.Size = size;
            keyword2 = new StringDrawable(xPos, yPos, texture, "", Color.SeaGreen);
            keyword2.Size = size;
            whText = new StringDrawable(0, 0, texture, "", Color.White);
            whText.Size = size;
            whText.Layer = 1;
            choicesBG = new RectangleSprite(0, 0, 0, 0);
            choicesBG.Color = Color.FromArgb(255, 50, 50, 50);
            choicesBG.Layer = 12;
            choicesSel = new RectangleSprite(0, 0, 0, 0);
            choicesSel.Color = Color.FromArgb(255, 35, 35, 150);
            choicesSel.Layer = 13;
            choices = new StringDrawable(0, 0, texture, "", Color.White);
            choices.Layer = 14;
            descriptionBG = new RectangleSprite(0, 0, Game.RESOLUTION_WIDTH, 32) { Layer = 10, Color = Color.FromArgb(255, 25, 25, 70), Visible = false };
            description = new StringDrawable(2, 2, game.NonMonoFont, "description", Color.FromArgb(255, 180, 180, 180)) { Layer = 11, Visible = false, MaxWidth = Game.RESOLUTION_WIDTH - 4 };
            AllTogether.Add(white);
            AllTogether.Add(command);
            AllTogether.Add(delim);
            AllTogether.Add(keyword);
            AllTogether.Add(number);
            AllTogether.Add(keyword2);
            AllTogether.Add(descriptionBG);
            AllTogether.Add(description);
            AllTogether.Add(whText);
            s = script;
        }

        public void SetSize(float size)
        {
            foreach (StringDrawable sd in AllTogether)
            {
                sd.Size = size;
            }
        }

        public void SetSelectionSprite(StringDrawable sel)
        {
            selection = sel;
            sel.Layer = -1;
            AllTogether.Add(sel);
        }

        public void CompleteChoice()
        {
            if (Choices is null || !ChoicesVisible) return;
            string sc = Choices.ElementAtOrDefault(SelectedChoice);
            if (sc is null) return;
            if (start > end)
                end = selection.Text.Length;
            string newText = selection.Text.Substring(0, start);
            string curText = selection.Text.Substring(start, end - start);
            newText += sc;
            newText += selection.Text.Substring(end);
            selection.SelectionStart = start + sc.Length;
            selection.SelectionLength = 0;
            selection.Text = newText;
            SetBuffers2(newText);
            ChoicesVisible = false;
            AllTogether.Remove(choices);
            AllTogether.Remove(choicesBG);
            AllTogether.Remove(choicesSel);
        }

        public void ShowChoices(float camx, float camy)
        {
            ShowingChoices = true;
            UpdateChoices(camx, camy);
        }

        public void HideChoices()
        {
            ChoicesVisible = false;
            AllTogether.Remove(choices);
            AllTogether.Remove(choicesBG);
            AllTogether.Remove(choicesSel);
        }

        public void UpdateChoices(float camx, float camy)
        {
            if (ShowingChoices)
            {
                if (!AllTogether.Contains(choices))
                {
                    AllTogether.Add(choices);
                    AllTogether.Add(choicesBG);
                    AllTogether.Add(choicesSel);
                }
                if (SelectedChoice >= Choices.Count)
                    SelectedChoice = Choices.Count - 1;
                else if (SelectedChoice == -1)
                {
                    SelectedChoice = 0;
                    choiceScroll = 0;
                }
                if (SelectedChoice >= choiceScroll + 7)
                    choiceScroll = SelectedChoice - 6;
                else if (SelectedChoice < choiceScroll)
                    choiceScroll = SelectedChoice;
                if (selection.SelectionStart < start || selection.SelectionStart > end)
                    Choices = new List<string>();
                choicesBG.X = selection.SelectionX + 16 + (start - selection.SelectionStart) * 8;
                choicesBG.Y = selection.SelectionY + 16;
                StringBuilder sb = new StringBuilder();
                for (int i = choiceScroll; i < choiceScroll + 7; i++)
                {
                    if (Choices.Count() > i)
                    {
                        sb.Append(Choices.ElementAtOrDefault(i));
                        sb.Append('\n');
                    }
                }
                if (sb.Length > 1)
                    sb.Remove(sb.Length - 1, 1);
                choices.Text = sb.ToString();
                choicesBG.SetSize(Math.Max(choices.Width + 2, 64), choices.Height + 2);
                if (choicesBG.Right > Game.RESOLUTION_WIDTH + camx)
                    choicesBG.Right = Game.RESOLUTION_WIDTH + camx;
                if (choicesBG.Bottom > Game.RESOLUTION_HEIGHT + camy)
                    choicesBG.Bottom = selection.SelectionY;
                choices.X = choicesBG.X + 1;
                choices.Y = choicesBG.Y + 1;
                choicesSel.SetSize(choicesBG.Width - 2, 8 * choices.Size);
                bool iv = Choices.Count() > 0;
                choicesBG.Visible = choicesSel.Visible = choices.Visible = iv;
                choicesSel.X = choicesBG.X + 1;
                choicesSel.Y = choicesBG.Y + 1 + (8 * choices.Size * (SelectedChoice - choiceScroll));
                ChoicesVisible = iv;
            }
        }

        private string[] GetChoices(Command.ArgTypes argType, SortedSet<string> createdSprites, Texture texture)
        {
            switch (argType)
            {
                case Command.ArgTypes.Command:
                    {
                        return Command.CommandNames;
                    }
                case Command.ArgTypes.Sprite:
                    {
                        SortedSet<string> spriteNames = new SortedSet<string>(createdSprites);
                        foreach (string sprite in Owner.UserAccessSprites.Keys)
                        {
                            spriteNames.Add(sprite);
                        }
                        spriteNames.Add("player");
                        spriteNames.Add("self");
                        spriteNames.Add("target");
                        return spriteNames.ToArray();
                    }
                case Command.ArgTypes.Texture:
                    {
                        string[] ret = new string[Owner.Textures.Count];
                        for (int i = 0; i < ret.Length; i++)
                        {
                            ret[i] = Owner.Textures.Keys[i];
                        }
                        return ret;
                    }
                case Command.ArgTypes.Animation:
                    {
                        if (texture is null) return new string[] { };
                        string[] ret = new string[texture.Animations.Count];
                        for (int i = 0; i < ret.Length; i++)
                        {
                            ret[i] = texture.Animations.Keys[i];
                        }
                        return ret;
                    }
                case Command.ArgTypes.Sound:
                    {
                        string[] ret = new string[Owner.Sounds.Count];
                        for (int i = 0; i < ret.Length; i++)
                        {
                            ret[i] = Owner.Sounds.Keys[i];
                        }
                        return ret;
                    }
                case Command.ArgTypes.Color:
                    {
                        SortedSet<string> ret = new SortedSet<string>();
                        foreach (string clr in Game.colors.Keys)
                        {
                            if (!ret.Contains(clr))
                                ret.Add(clr);
                        }
                        foreach (string sp in GetChoices(Command.ArgTypes.Sprite, createdSprites, texture))
                        {
                            if (!ret.Contains(sp))
                                ret.Add(sp);
                        }
                        return ret.ToArray();
                    }
                case Command.ArgTypes.Bool:
                    {
                        return new string[] { "false", "true" };
                    }
                case Command.ArgTypes.Number:
                    {
                        return Owner.Vars.Keys.ToArray();
                    }
                case Command.ArgTypes.Mood:
                    {
                        return new string[] { "happy", "sad" };
                    }
                case Command.ArgTypes.Position1:
                    {
                        SortedSet<string> ret = new SortedSet<string>(GetChoices(Command.ArgTypes.Sprite, createdSprites, texture));
                        if (!ret.Contains("center"))
                            ret.Add("center");
                        if (!ret.Contains("centerx"))
                            ret.Add("centerx");
                        if (!ret.Contains("centery"))
                            ret.Add("centery");
                        return ret.ToArray();
                    }
                case Command.ArgTypes.Position2:
                    {
                        return new string[] { "above", "below" };
                    }
                case Command.ArgTypes.AI:
                    {
                        return new string[] { "face", "follow", "stand" };
                    }
                case Command.ArgTypes.Squeak:
                    {
                        SortedSet<string> ret = new SortedSet<string>(Game.colors.Keys);
                        foreach (string sp in GetChoices(Command.ArgTypes.Sprite, createdSprites, texture))
                        {
                            if (!ret.Contains(sp))
                                ret.Add(sp);
                        }
                        return ret.ToArray();
                    }
                case Command.ArgTypes.Script:
                    {
                        return Owner.Scripts.Keys.ToArray();
                    }
                case Command.ArgTypes.If:
                    {
                        return new string[] { "continue", "stop", "wait" };
                    }
                case Command.ArgTypes.PosX:
                    {
                        return new string[] { "centerx", "right", "x" };
                    }
                case Command.ArgTypes.PosY:
                    {
                        return new string[] { "bottom", "centery", "y" };
                    }
                case Command.ArgTypes.Music:
                    {
                        return Owner.Songs.Keys.ToArray();
                    }
                case Command.ArgTypes.Identifier:
                    {
                        SortedSet<string> ret = new SortedSet<string>(GetChoices(Command.ArgTypes.Sprite, createdSprites, texture));
                        foreach (string item in Command.Pointers)
                        {
                            ret.Add(item);
                        }
                        return ret.ToArray();
                    }
                case Command.ArgTypes.Property:
                    {
                        return Command.SpritePointers.ToArray();
                    }
                case Command.ArgTypes.NumberFormat:
                    {
                        return new string[] { "x.x", "x.00", "h:mm:ss", "mm:ss", "mm:ss.00" };
                    }
            }
            return new string[] { };
        }

        public SpriteCollection AllTogether;

        static readonly SortedSet<string> commands = new SortedSet<string>(Command.CommandNames);
        static readonly SortedSet<string> keywords = new SortedSet<string> { "player", "this", "self", "target" };
        static readonly SortedSet<string> keywords2 = new SortedSet<string> { "happy", "sad", "left", "right", "true", "false", "centerx", "centery", "bottom" };
        static readonly SortedSet<char> delims = new SortedSet<char> { ',', '(', ')', '\n' };
        static readonly SortedSet<char> delims2 = new SortedSet<char> { '+', '-', '*', '/', '>', '<', '=', ':', '?', '&', '|', '#' };
        static readonly SortedSet<char> alldelims = new SortedSet<char> { ',', '(', ')', '\n', '+', '-', '*', '/', '>', '<', '=', ':', '?', '&', '|', '#' };
        public void SetBuffers(string text)
        {
            colors.Clear();
            foreach (StringDrawable sd in AllTogether)
            {
                if (sd == selection) continue;
                sd.Text = "";
            }
            int x = 0, y = 0;
            int index = 0;
            text = text.Replace(Environment.NewLine, "\n");
            int argNumber = 0;
            string commandName = null;
            int colorLines = 0;
            string color = "";
            bool colorText = false;
            Color currentColor;
            while (index < text.Length)
            {
                StringDrawable addTo = null;
                string toAdd;
                if ((delims.Contains(text[index]) || delims2.Contains(text[index])) && !colorText)
                {
                    toAdd = text[index].ToString();
                    addTo = delim;
                    if (toAdd == "\n")
                    {
                        argNumber = 0;
                        commandName = null;
                        if (colorLines > 0 && !colorText)
                        {
                            colorText = true;
                        }
                        else if (colorLines > 0 && colorText)
                        {
                            colorLines -= 1;
                            if (colorLines == 0 && colorText)
                            {
                                colorText = false;
                                color = "";
                            }
                        }
                    }
                }
                else
                {
                    if (colorText)
                    {
                        int ind = text.IndexOf('\n', index) - index;
                        if (ind >= 0)
                            toAdd = text.Substring(index, ind);
                        else
                            toAdd = text.Substring(index);
                    }
                    else
                    {
                        int ind = text.IndexOfAny(delims.ToArray(), index) - index;
                        if (ind >= 0)
                            toAdd = text.Substring(index, ind);
                        else
                            toAdd = text.Substring(index);
                    }
                }
                if (argNumber == 0 && addTo != delim) commandName = toAdd;
                Color? c;
                if (colorText && addTo != delim)
                {
                    if (colors.ContainsKey(color))
                        addTo = colors[color];
                    else
                        addTo = white;
                }
                else if (((argNumber == 2 && commandName == "say") || (argNumber == 1 && commandName == "text")) && (c = Owner.GetColor(toAdd)).HasValue)
                {
                    color = toAdd;
                    currentColor = c.Value;
                    if (!colors.ContainsKey(color))
                    {
                        StringDrawable newColor = new StringDrawable(xPosition, yPosition, tex, "", currentColor);
                        newColor.Size = size;
                        AllTogether.Add(newColor);
                        colors.Add(color, newColor);
                    }
                    addTo = colors[color];
                }
                else if (commands.Contains(toAdd))
                    addTo = command;
                else if (keywords.Contains(toAdd))
                    addTo = keyword;
                else if (keywords2.Contains(toAdd))
                    addTo = keyword2;
                else if (float.TryParse(toAdd, out float i))
                {
                    addTo = number;
                    if ((argNumber == 1 && commandName == "say") || (argNumber == 4 && commandName == "text"))
                    {
                        colorLines = (int)i;
                    }
                }
                else if (addTo is null)
                    addTo = white;
                Point p = addTo.AddText(x, y, toAdd);
                x = p.X;
                y = p.Y;
                index += toAdd.Length;
                if (addTo != delim)
                    argNumber += 1;
            }
        }

        public void SetBuffers2(string text)
        {
            bool foundArg = false;
            arg = Command.ArgTypes.Command;
            argText = "";
            Command.ArgTypes lastArg = Command.ArgTypes.None;
            //foreach (StringDrawable sd in colors.Values)
            //{
            //    sd.Dispose();
            //}
            //colors.Clear();
            foreach (Sprite sd in AllTogether)
            {
                if (!(sd is StringDrawable)) continue;
                if (sd == selection || sd == choices) continue;
                (sd as StringDrawable).Text = "";
            }
            int curX = 0, curY = 0;
            string toColor = null;
            string cmd = "";
            int colorLines = 0;
            bool comment = false;
            int argNumber = 0;
            Command.ArgTypes[] argTypes = new Command.ArgTypes[] { };
            bool isColoring = false;
            bool isQ = false;
            bool isC = false;
            bool wasQ = false;
            bool wasC = false;
            bool qHud = false;
            int index = 0;
            string lastAdded = "";
            char[] allDelims = delims.Concat(delims2).ToArray();
            string toAdd = "";
            SortedSet<string> newSprites = new SortedSet<string>();
            Texture texture = null;
            start = 0;
            end = 0;
            bool lastDelim = false;
            description.Visible = descriptionBG.Visible = false;
            int par = 0;
            while (index < text.Length)
            {
                // Get the index of the next delim or the end of the text
                int ind = text.IndexOfAny(allDelims, index);
                if (ind == -1)
                    ind = text.Length;
                toAdd = text.Substring(index, ind - index);
                // Ignore if the '-' is just a negative sign and not a minus operator.
                if (text[index] == '-' && index > 0 && allDelims.Contains(text[index - 1]))
                {
                    ind = text.IndexOfAny(allDelims, ind + 1);
                    if (ind == -1)
                        ind = text.Length;
                    toAdd = text.Substring(index, ind - index);
                }
                StringDrawable addTo = null;
                if (toAdd == "" && (!(isColoring || comment) || text[index] == '\n'))
                {
                    if (!(qHud && isQ))
                    {
                        wasQ = isQ;
                        wasC = isC;
                        isQ = false;
                        isC = false;
                        qHud = false;
                    }
                    else
                    {
                        isQ = false;
                    }
                    // What delim character is it?
                    char dl = text[index];
                    toAdd = dl.ToString();
                    addTo = delim;
                    if (lastDelim) lastAdded = "";
                    lastDelim = true;
                    if (!foundArg && selection is object && selection.SelectionStart + selection.SelectionLength <= index && selection.SelectionStart <= index)
                    {
                        Command.ArgTypes type = Command.ArgTypes.Command;
                        if (argNumber > 0)
                        {
                            type = argTypes.ElementAtOrDefault(argNumber - 1);
                            if (wasQ) type = Command.ArgTypes.Identifier;
                            else if (wasC) type = Command.ArgTypes.Property;
                        }
                        currentChoices = GetChoices(type, newSprites, texture);
                        Choices = currentChoices.ToList().FindAll((s) =>
                        {
                            return s.StartsWith(lastAdded);
                        });
                        argText = lastAdded;
                        foundArg = true;
                        ShowDesc(cmd, argNumber, argTypes);
                        end = index;
                    }
                    if (dl == '\n')
                    {
                        par = 0;
                        argNumber = 0;
                        cmd = "";
                        texture = null;
                        if (colorLines > 0 && toColor is object && !isColoring)
                        {
                            isColoring = true;
                        }
                        else if (isColoring)
                        {
                            colorLines -= 1;
                            if (colorLines <= 0)
                            {
                                colorLines = 0;
                                toColor = null;
                                isColoring = false;
                            }
                        }
                    }
                    else if (dl == ',' || (dl == '(' && argNumber == 0) || (dl == ')' && par == 0))
                    {
                        argNumber += 1;
                        if (dl == ')')
                            argNumber = argTypes.Length + 1;
                    }
                    else if (dl == '#')
                    {
                        comment = true;
                        toColor = "comment";
                        if (!colors.TryGetValue("comment", out addTo))
                        {
                            addTo = new StringDrawable(xPosition, yPosition, tex, "", Color.FromArgb(255, 0, 100, 0));
                            addTo.Size = size;
                            AllTogether.Add(addTo);
                            colors.Add("comment", addTo);
                        }
                    }
                    else if (dl == '>')
                    {
                        comment = true;
                        toColor = "marker";
                        if (!colors.TryGetValue("marker", out addTo))
                        {
                            addTo = new StringDrawable(xPosition, yPosition, tex, "", Color.FromArgb(255, 100, 190, 230));
                            addTo.Size = size;
                            AllTogether.Add(addTo);
                            colors.Add("marker", addTo);
                        }
                    }
                    else
                    {
                        if (dl == '?')
                            isQ = true;
                        else if (dl == ':')
                            isC = true;
                        else if (dl == '(')
                            par++;
                        else if (dl == ')')
                            par--;
                    }
                    if (!foundArg)
                        start = index + 1;
                }
                else
                {
                    lastDelim = false;
                    lastAdded = toAdd;
                    if (isColoring)
                    {
                        ind = text.IndexOf('\n', index);
                        if (ind == -1)
                            ind = text.Length;
                        toAdd = text.Substring(index, ind - index);
                        addTo = colors[toColor];
                    }
                    else if (comment)
                    {
                        ind = text.IndexOf('\n', index);
                        if (ind == -1)
                            ind = text.Length;
                        toAdd = text.Substring(index, ind - index);
                        addTo = colors[toColor];
                        comment = false;
                    }
                    else if (argNumber == 0)
                    {
                        if (commands.Contains(toAdd))
                            addTo = command;
                        else
                            addTo = white;
                        cmd = toAdd;
                        argTypes = Command.GetArgs(cmd);
                    }
                    else
                    {
                        if (argTypes.Length >= argNumber)
                        {
                            lastArg = argTypes[argNumber - 1];
                            switch (lastArg)
                            {
                                case Command.ArgTypes.None:
                                    {
                                        addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Int:
                                    {
                                        if (int.TryParse(toAdd, out int i))
                                        {
                                            addTo = number;
                                            colorLines = i;
                                        }
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Sprite:
                                    {
                                        addTo = isSprite(toAdd, newSprites);
                                        break;
                                    }
                                case Command.ArgTypes.Texture:
                                    {
                                        if ((texture = Owner.TextureFromName(toAdd)) is object)
                                        {
                                            addTo = keyword2;
                                        }
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Animation:
                                    {
                                        if (texture is object && texture.AnimationFromName(toAdd) is object)
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Sound:
                                    {
                                        if (Owner.GetSound(toAdd) is object)
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Color:
                                    {
                                        Color? c = Owner.GetColor(toAdd, null, null);
                                        if (c.HasValue)
                                        {
                                            Color color = c.Value;
                                            if (!colors.ContainsKey(toAdd))
                                            {
                                                StringDrawable newColor = new StringDrawable(xPosition, yPosition, tex, "", color);
                                                newColor.Size = size;
                                                AllTogether.Add(newColor);
                                                colors.Add(toAdd, newColor);
                                            }
                                            toColor = toAdd;
                                            addTo = colors[toAdd];
                                        }
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Bool:
                                    {
                                        if (bool.TryParse(toAdd, out bool b))
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Number:
                                    {
                                        if (isQ)
                                        {
                                            if (Command.Pointers.Contains(toAdd))
                                                addTo = keyword2;
                                            else if (toAdd == "hud")
                                            {
                                                addTo = keyword;
                                                qHud = true;
                                            }
                                            else
                                                addTo = isSprite(toAdd, newSprites);
                                        }
                                        else if (qHud)
                                        {
                                            addTo = white;
                                        }
                                        else if (isC)
                                        {
                                            if (Command.SpritePointers.Contains(toAdd))
                                                addTo = keyword2;
                                            else
                                                addTo = white;
                                        }
                                        else if (float.TryParse(toAdd, out float f))
                                            addTo = number;
                                        else if (Owner.Vars.TryGetValue(toAdd, out Variable n))
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Mood:
                                    {
                                        string ta = toAdd.ToLower();
                                        if (ta == "sad" || ta == "happy")
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Position1:
                                    {
                                        string ta = toAdd.ToLower();
                                        if (ta == "centerx" || ta == "centery" || ta == "center")
                                            addTo = keyword2;
                                        else
                                            addTo = isSprite(toAdd, newSprites);
                                        break;
                                    }
                                case Command.ArgTypes.Position2:
                                    {
                                        string ta = toAdd.ToLower();
                                        if (ta == "above" || ta == "below")
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.AI:
                                    {
                                        string ta = toAdd.ToLower();
                                        if (ta == "stand" || ta == "face" || ta == "follow")
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Squeak:
                                    {
                                        if (Command.presetcolors.Contains(toAdd))
                                            addTo = keyword2;
                                        else
                                            addTo = isSprite(toAdd, newSprites);
                                        break;
                                    }
                                case Command.ArgTypes.Script:
                                    {
                                        if (Owner.ScriptFromName(toAdd) is object)
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.If:
                                    {
                                        string ta = toAdd.ToLower();
                                        if (ta == "stop" || ta == "wait" || ta == "continue")
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.SpriteName:
                                    {
                                        newSprites.Add(toAdd);
                                        addTo = keyword2;
                                        break;
                                    }
                                case Command.ArgTypes.PosX:
                                    {
                                        string ta = toAdd.ToLower();
                                        if (ta == "x" || ta == "centerx" || ta == "right")
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.PosY:
                                    {
                                        string ta = toAdd.ToLower();
                                        if (ta == "y" || ta == "centery" || ta == "bottom")
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Music:
                                    {
                                        if (!Owner.GetMusic(toAdd).IsNull)
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Marker:
                                    {
                                        break;
                                    }
                                case Command.ArgTypes.Type:
                                    {
                                        if (Command.Types.ContainsKey(toAdd))
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.NumberFormat:
                                    {
                                        addTo = keyword2;
                                        break;
                                    }
                            }
                            if (addTo is null)
                                addTo = white;
                        }
                        else
                            addTo = white;
                    }
                }
                Point p = addTo.AddText(curX, curY, toAdd);
                index += toAdd.Length;
                curX = p.X;
                curY = p.Y;
            }
            if (!foundArg)
            {
                Command.ArgTypes type = Command.ArgTypes.Command;
                if (argNumber > 0)
                {
                   type = argTypes.ElementAtOrDefault(argNumber - 1);
                   if (isQ) type = Command.ArgTypes.Identifier;
                   else if (isC) type = Command.ArgTypes.Property;
                }
                currentChoices = GetChoices(type, newSprites, texture);
                Choices = currentChoices.ToList().FindAll((s) =>
                {
                    return s.StartsWith(lastAdded);
                });
                argText = lastAdded;
                foundArg = true;
                ShowDesc(cmd, argNumber, argTypes);
                end = index;
            }
        }

        private void ShowDesc(string cmd, int argNumber, Command.ArgTypes[] argTypes)
        {
            if (Owner.ScriptInfo is object)
            {
                Newtonsoft.Json.Linq.JToken s = Owner.ScriptInfo[cmd];
                if (s is object && s.HasValues)
                {
                    string str = (string)s["Description"] ?? "";
                    if (argNumber > 0 && argNumber < argTypes.Length + 1)
                    {
                        Newtonsoft.Json.Linq.JArray jarr = (Newtonsoft.Json.Linq.JArray)s["Args"];
                        str += '\n';
                        str += (arg = argTypes.ElementAtOrDefault(argNumber - 1)).ToString() + ": " + ((string)jarr.ElementAtOrDefault(argNumber - 1) ?? "???");
                    }
                    description.Text = str;
                    descriptionBG.SetHeight(description.Height + 4);
                    description.Visible = descriptionBG.Visible = true;
                    MoveDesc();
                }
            }
        }

        public void CtrlEnter()
        {
            if (arg == Command.ArgTypes.Script && Owner.ScriptFromName(argText) is null)
            {
                Script s = new Script(new Command[] { }, argText, "");
                Owner.Scripts.Add(argText, s);
                SetBuffers2(selection.Text);
            }
            else if (arg == Command.ArgTypes.Number && !Owner.Vars.ContainsKey(argText))
            {
                DecimalVariable v = new DecimalVariable(argText, 0);
                Owner.Vars.Add(argText, v);
                SetBuffers2(selection.Text);
            }
            else if (arg == Command.ArgTypes.Sound)
            {
                SoundEffect se = Owner.GetSound(argText ?? "");
                if (SelectedChoice > -1 && ChoicesVisible && SelectedChoice < Choices.Count)
                {
                    se = Owner.GetSound(Choices[SelectedChoice]);
                }
                se?.Play();
            }
        }
        public PointF GetSelectionLocation
        {
            get
            {
                if (selection is null)
                    return new PointF(0, 0);
                PointF p = new PointF(selection.SelectionX - ScrollX, selection.SelectionY - ScrollY);
                return p;
            }
        }

        public void MoveDesc()
        {
            PointF selLoc = GetSelectionLocation;
            if (selLoc.Y > 40)
            {
                description.Y = 2 + ScrollY;
                descriptionBG.Y = ScrollY;
            }
            else
            {
                description.Bottom = Game.RESOLUTION_HEIGHT - 2 + ScrollY;
                descriptionBG.Bottom = Game.RESOLUTION_HEIGHT + ScrollY;
            }
            description.X = 2 + ScrollX;
            descriptionBG.X = ScrollX;
        }

        private StringDrawable isSprite(string s, SortedSet<string> newSprites)
        {
            string str = s.ToLower();
            if (newSprites.Contains(s))
                return keyword2;
            else if (Owner.SpriteFromName(s) is object)
                return keyword2;
            else if (str == "player" || str == "this" || str == "self" || str == "target")
                return keyword;
            else
                return white;
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            var scrCam = Owner.Camera;
            scrCam = Matrix4.CreateTranslation(-ScrollX, -ScrollY, 0) * scrCam;
            scrCam = Matrix4.CreateScale(Zoom, Zoom, 1) * scrCam;
            Owner.SetView(ref scrCam);
            AllTogether.Render(Owner.FrameCount);
        }

        public override void Process()
        {
            if (YieldInput) return;
            if (Owner.TypingTo != selection)
            {
                Owner.StartTyping(selection, (str) =>
                {
                    SetBuffers2(str);
                    CheckScroll();
                    ShowChoices(ScrollX, ScrollY);
                }, (r, st) =>
                {
                    s.Contents = selection.Text;
                    s.ClearMarkers();
                    s.Commands = Command.ParseScript(Owner, selection.Text, s);
                    Owner.RemoveLayer(this);
                    Owner.ScriptEditor = null;
                });
            }
            float x = (Owner.MouseX + ScrollX) * Zoom;
            float y = (Owner.MouseY + ScrollY) * Zoom;
            int sel = selection.GetIndexFromPoint(x, y, Zoom);
            if (sel < 0 || sel >= selection.Text.Length)
            {
                whText.Visible = false;
                return;
            }
            char sc = selection.Text[sel];
            if (!alldelims.Contains(sc))
            {
                int start = selection.Text.LastIndexOfAny(alldelims.ToArray(), sel);
                int end = selection.Text.IndexOfAny(alldelims.ToArray(), sel);
                if (end == -1) end = selection.Text.Length;
                start++;
                string text = selection.Text.Substring(start, end - start);
                whText.Text = text;
                PointF cl = selection.GetCharacterLocation(start);
                cl.X += selection.X;
                cl.Y += selection.Y;
                whText.X = cl.X;
                whText.Y = cl.Y;
                whText.Visible = true;
            }
            else
            {
                whText.Visible = false;
            }
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            if (YieldInput) return;
            float x = (Owner.MouseX + ScrollX) * Zoom;
            float y = (Owner.MouseY + ScrollY) * Zoom;
            int sel = selection.GetIndexFromPoint(x, y, Zoom);
            selection.SelectionStart = sel;
            selection.SelectionLength = 0;
            selection.Text = selection.Text;
            SetBuffers2(selection.Text);
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (typing)
            {
                if (e.Control && e.Key == Keys.R)
                {
                    Darken = 0f;
                    AllTogether.Visible = false;
                    YieldInput = true;
                    Owner.RoomTool(() =>
                    {
                        Darken = 1f;
                        YieldInput = false;
                        AllTogether.Visible = true;
                    });
                }
                else if (e.Control && e.Key == Keys.P)
                {
                    //CurrentEditingFocus = FocusOptions.Level;
                    //tool = Tools.Point;
                    //editorTool.Text = "- Select Point -";
                    //toolPrompt.Text = "Click a point...";
                    //previewTile.Visible = false;
                    //typing = false;
                }
                else if (e.Control && e.Key == Keys.Enter)
                {
                    CtrlEnter();
                }
                if (e.Key == Keys.Right || e.Key == Keys.Left || e.Key == Keys.Up || e.Key == Keys.Down)
                {
                    CheckScroll();
                    UpdateChoices(ScrollX, ScrollY);
                    SetBuffers2(selection.Text);
                }
                if (e.Key == Keys.Tab)
                {
                    CompleteChoice();
                }
                if (ChoicesVisible)
                {
                    if (e.Key == Keys.Up)
                    {
                        SelectedChoice -= 1;
                        if (SelectedChoice < 0)
                            SelectedChoice = Choices.Count - 1;
                        UpdateChoices(ScrollX, ScrollY);
                    }
                    else if (e.Key == Keys.Down)
                    {
                        SelectedChoice += 1;
                        if (SelectedChoice >= Choices.Count)
                            SelectedChoice = 0;
                        UpdateChoices(ScrollX, ScrollY);
                    }
                    else if (e.Key == Keys.Escape)
                    {
                        HideChoices();
                        ShowingChoices = false;
                    }
                }
            }
        }

        public override void HandleWheel(int e)
        {
            if (e > 0)
            {
                if (Owner.IsKeyHeld(Keys.LeftShift))
                    ScrollX = Math.Max(ScrollX - 10, 0);
                else
                    ScrollY = Math.Max(ScrollY - 10, 0);
            }
            else if (e < 0)
            {
                if (Owner.IsKeyHeld(Keys.LeftShift))
                    ScrollX = Math.Max(Math.Min(ScrollX + 10, selection.Width + 16), 0);
                else
                    ScrollY = Math.Max(Math.Min(ScrollY + 10, selection.Height + 16), 0);
            }
            MoveDesc();
        }

        public override void Dispose()
        {
            AllTogether.Dispose();
        }

        public void CheckScroll()
        {
            if (selection.SelectionY < ScrollY - 8)
            {
                ScrollY = selection.SelectionY + 8;
            }
            else if (selection.SelectionY > ScrollY + Game.RESOLUTION_HEIGHT - 16)
            {
                ScrollY = selection.SelectionY - Game.RESOLUTION_HEIGHT + 16;
            }
            if (selection.SelectionX < ScrollX - 8)
            {
                ScrollX = selection.SelectionX + 8;
            }
            else if (selection.SelectionX > ScrollX + Game.RESOLUTION_WIDTH - 16)
            {
                ScrollX = selection.SelectionX - Game.RESOLUTION_WIDTH + 16;
            }
            MoveDesc();
        }
    }
}
