using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    class StringHighlighter
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
        public bool ShowingChoices;
        public bool ChoicesVisible;
        public int SelectedChoice = 0;
        string[] currentChoices;
        int choiceScroll;
        public List<string> Choices;

        int xPosition, yPosition;
        Texture tex;

        int start = 0;
        int end = 0;

        Game owner;
        float size;

        public StringHighlighter(int xPos, int yPos, Texture texture, Game game, float size = 1)
        {
            owner = game;
            xPosition = xPos;
            yPosition = yPos;
            tex = texture;
            this.size = size;
            AllTogether = new SpriteCollection();
            colors = new SortedList<string, StringDrawable>();
            white = new StringDrawable(xPos, yPos, texture, "", Color.White);
            white.Size = size;
            command = new StringDrawable(xPos, yPos, texture, "", Color.FromArgb(137, 113, 255));
            command.Size = size;
            delim = new StringDrawable(xPos, yPos, texture, "", Color.Purple);
            delim.Size = size;
            keyword = new StringDrawable(xPos, yPos, texture, "", Color.Blue);
            keyword.Size = size;
            number = new StringDrawable(xPos, yPos, texture, "", Color.Yellow);
            number.Size = size;
            keyword2 = new StringDrawable(xPos, yPos, texture, "", Color.SeaGreen);
            keyword2.Size = size;
            choicesBG = new RectangleSprite(0, 0, 0, 0);
            choicesBG.Color = Color.FromArgb(50, 50, 50);
            choicesBG.Layer = 1;
            choicesSel = new RectangleSprite(0, 0, 0, 0);
            choicesSel.Color = Color.FromArgb(35, 35, 150);
            choicesSel.Layer = 2;
            choices = new StringDrawable(0, 0, texture, "", Color.White);
            choices.Layer = 3;
            AllTogether.Add(white);
            AllTogether.Add(command);
            AllTogether.Add(delim);
            AllTogether.Add(keyword);
            AllTogether.Add(number);
            AllTogether.Add(keyword2);
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
            ShowingChoices = false;
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
                        foreach (string sprite in owner.UserAccessSprites.Keys)
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
                        string[] ret = new string[owner.Textures.Count];
                        for (int i = 0; i < ret.Length; i++)
                        {
                            ret[i] = owner.Textures.Keys[i];
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
                        string[] ret = new string[owner.Sounds.Count];
                        for (int i = 0; i < ret.Length; i++)
                        {
                            ret[i] = owner.Sounds.Keys[i];
                        }
                        return ret;
                    }
                case Command.ArgTypes.Color:
                    {
                        SortedSet<string> ret = new SortedSet<string>(Enum.GetNames(typeof(KnownColor)));
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
                        return owner.Vars.Keys.ToArray();
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
                        return owner.Scripts.Keys.ToArray();
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
                        return owner.Songs.Keys.ToArray();
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

        static SortedSet<string> commands = new SortedSet<string>(Command.CommandNames);
        static SortedSet<string> keywords = new SortedSet<string> { "player", "this", "self", "target" };
        static SortedSet<string> keywords2 = new SortedSet<string> { "happy", "sad", "left", "right", "true", "false", "centerx", "centery", "bottom" };
        static SortedSet<char> delims = new SortedSet<char> { ',', '(', ')', '\n' };
        static SortedSet<char> delims2 = new SortedSet<char> { '+', '-', '*', '/', '>', '<', '=', ':', '?', '&', '|' };
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
                else if (((argNumber == 2 && commandName == "say") || (argNumber == 1 && commandName == "text")) && (c = owner.GetColor(toAdd)).HasValue)
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
            int argNumber = 0;
            Command.ArgTypes[] argTypes = new Command.ArgTypes[] { };
            bool isColoring = false;
            bool isQ = false;
            bool isC = false;
            bool wasQ = false;
            bool wasC = false;
            int index = 0;
            string lastAdded = "";
            char[] allDelims = delims.Concat(delims2).ToArray();
            string toAdd = "";
            SortedSet<string> newSprites = new SortedSet<string>();
            Texture texture = null;
            start = 0;
            end = 0;
            bool lastDelim = false;
            while (index < text.Length)
            {
                int ind = text.IndexOfAny(allDelims, index);
                if (ind == -1)
                    ind = text.Length;
                toAdd = text.Substring(index, ind - index);
                if (text[index] == '-' && index > 0 && allDelims.Contains(text[index -1]))
                {
                    ind = text.IndexOfAny(allDelims, ind + 1);
                    if (ind == -1)
                        ind = text.Length;
                    toAdd = text.Substring(index, ind - index);
                }
                StringDrawable addTo = null;
                if (toAdd == "" && (!isColoring || text[index] == '\n'))
                {
                    wasQ = isQ;
                    wasC = isC;
                    isQ = false;
                    isC = false;
                    char dl = text[index];
                    toAdd = dl.ToString();
                    addTo = delim;
                    if (lastDelim) lastAdded = "";
                    lastDelim = true;
                    if (dl == '\n')
                    {
                        argNumber = 0;
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
                    else if (delims.Contains(dl))
                        argNumber += 1;
                    else
                    {
                        if (dl == '?')
                            isQ = true;
                        else if (dl == ':')
                            isC = true;
                    }
                    if (!foundArg && selection is object && selection.SelectionStart + selection.SelectionLength <= index && selection.SelectionStart <= index)
                    {
                        Command.ArgTypes type = Command.ArgTypes.Command;
                        if (argNumber > 0)
                        {
                            type = argTypes.ElementAtOrDefault(argNumber - 2);
                            if (wasQ) type = Command.ArgTypes.Identifier;
                            else if (wasC) type = Command.ArgTypes.Property;
                        }
                        currentChoices = GetChoices(type, newSprites, texture);
                        Choices = currentChoices.ToList().FindAll((s) =>
                        {
                            return s.StartsWith(lastAdded);
                        });
                        foundArg = true;
                        end = index;
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
                                        if ((texture = owner.TextureFromName(toAdd)) is object)
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
                                        if (owner.GetSound(toAdd) is object)
                                            addTo = keyword2;
                                        else
                                            addTo = white;
                                        break;
                                    }
                                case Command.ArgTypes.Color:
                                    {
                                        Color? c = owner.GetColor(toAdd, null, null);
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
                                            else
                                                addTo = isSprite(toAdd, newSprites);
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
                                        else if (owner.Vars.TryGetValue(toAdd, out Number n))
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
                                        if (owner.ScriptFromName(toAdd) is object)
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
                                        if (!owner.GetMusic(toAdd).IsNull)
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
                foundArg = true;
                end = index;
            }
        }

        private StringDrawable isSprite(string s, SortedSet<string> newSprites)
        {
            string str = s.ToLower();
            if (newSprites.Contains(s))
                return keyword2;
            else if (owner.SpriteFromName(s) is object)
                return keyword2;
            else if (str == "player" || str == "this" || str == "self" || str == "target")
                return keyword;
            else
                return white;
        }
    }
}
