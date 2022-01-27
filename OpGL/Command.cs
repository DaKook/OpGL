using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using OpenTK.Input;

namespace V7
{
    public class Command
    {
        public Game game;
        private Action<Script.Executor, Sprite, Sprite> action;
        public bool Wait;
        public static readonly SortedList<string, Type> Types = new SortedList<string, Type>()
        {
            { "gravitylines", typeof(GravityLine) },
            { "platforms", typeof(Platform) },
            { "warptokens", typeof(WarpToken) },
            { "warplines", typeof(WarpLine) },
            { "enemies", typeof(Enemy) },
            { "scriptboxes", typeof(ScriptBox) },
            { "checkpoints", typeof(Checkpoint) }
        };

        public Command(Game game, Action<Script.Executor, Sprite, Sprite> action, bool wait = false)
        {
            this.game = game;
            this.action = action;
            Wait = wait;
        }

        public void Execute(Script.Executor executor, Sprite sender, Sprite target)
        {
            action(executor, sender, target);
        }

        private static Sprite[] getSprite(string s, Game game, Script.Executor e)
        {
            switch ((s ?? "").ToLower())
            {
                case "player":
                    return new Sprite[] { game.ActivePlayer };
                case "this":
                case "self":
                    return new Sprite[] { e.Sender };
                case "target":
                    return new Sprite[] { e.Target };
                case "all":
                    return game.GetAll((sp) => !sp.Static);
                case "enemies":
                    return game.GetAll((sp) => sp is Enemy);
                case "platforms":
                    return game.GetAll((sp) => sp is Platform);
                case "crewmen":
                    return game.GetAll((sp) => sp is Crewman);
                default:
                    {
                        if (!e.CreatedSprites.TryGetValue(s, out Sprite ret))
                            ret = game.SpriteFromName(s);
                        return new Sprite[] { ret };
                    }
            }
        }

        public static SortedSet<string> Pointers = new SortedSet<string>() { "trinkets", "totaltrinkets", "roomx", "roomy", "camerax", "cameray" };
        public static SortedSet<string> SpritePointers = new SortedSet<string>() { "x", "y", "centerx", "centery", "right", "bottom", "trinkets" };
        static SortedSet<char> operators = new SortedSet<char>() { '+', '-', '*', '/', '=', '!', '&', '|', '>', '<' };
        private static Number getNumber(string s, Game game)
        {
            if (s == null) return new Number("", 0f);
            s = s.Trim();
            bool hasMinus = false;
            {
                int ind = s.IndexOf('-');
                while (ind > -1)
                {
                    if (ind != 0 && !operators.Contains(s[ind - 1]))
                    {
                        hasMinus = true;
                        break;
                    }
                    ind = s.IndexOf('-', ind + 1);
                }
            }
            if (s.Contains('&'))
            {
                string[] numbers = s.Split('&');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = 1;
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (values[i].Value(e) == 0)
                        {
                            ret = 0;
                        }
                    }
                    return ret;
                });
            }
            else if (s.Contains('|'))
            {
                string[] numbers = s.Split('|');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = 0;
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (values[i].Value(e) != 0)
                        {
                            ret = 1;
                        }
                    }
                    return ret;
                });
            }
            else if (s.Contains('='))
            {
                string[] numbers = s.Split('=');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = 1;
                    float n = values[0].Value(e);
                    for (int i = 1; i < values.Length; i++)
                    {
                        if (n != values[i].Value(e))
                        {
                            ret = 0;
                        }
                    }
                    return ret;
                });
            }
            else if (s.Contains('!'))
            {
                string[] numbers = s.Split('!');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = 1;
                    float n = values[0].Value(e);
                    for (int i = 1; i < values.Length; i++)
                    {
                        if (n == values[i].Value(e))
                        {
                            ret = 0;
                        }
                    }
                    return ret;
                });
            }
            else if (s.Contains('>'))
            {
                string[] numbers = s.Split('>');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = 1;
                    float n = values[0].Value(e);
                    for (int i = 1; i < values.Length; i++)
                    {
                        if (n <= values[i].Value(e))
                        {
                            ret = 0;
                        }
                    }
                    return ret;
                });
            }
            else if (s.Contains('<'))
            {
                string[] numbers = s.Split('<');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = 1;
                    float n = values[0].Value(e);
                    for (int i = 1; i < values.Length; i++)
                    {
                        if (n >= values[i].Value(e))
                        {
                            ret = 0;
                        }
                    }
                    return ret;
                });
            }
            else if (s.Contains('+'))
            {
                string[] numbers = s.Split('+');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = values[0].Value(e);
                    for (int i = 1; i < values.Length; i++)
                    {
                        ret += values[i].Value(e);
                    }
                    return ret;
                });
            }
            else if (hasMinus)
            {
                List<string> numbers = new List<string>();
                int ind = s.IndexOf('-');
                int li = 0;
                while (ind > -1)
                {
                    if (ind != 0 && !operators.Contains(s[ind - 1]))
                    {
                        numbers.Add(s.Substring(li, ind - li));
                        li = ind + 1;
                    }
                    ind = s.IndexOf('-', ind + 1);
                }
                numbers.Add(s.Substring(li));
                Number[] values = new Number[numbers.Count];
                for (int i = 0; i < numbers.Count; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = values[0].Value(e);
                    for (int i = 1; i < values.Length; i++)
                    {
                        ret -= values[i].Value(e);
                    }
                    return ret;
                });
            }
            else if (s.Contains('*'))
            {
                string[] numbers = s.Split('*');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = values[0].Value(e);
                    for (int i = 1; i < values.Length; i++)
                    {
                        ret *= values[i].Value(e);
                    }
                    return ret;
                });
            }
            else if (s.Contains('/'))
            {
                string[] numbers = s.Split('/');
                Number[] values = new Number[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    values[i] = getNumber(numbers[i], game);
                }
                return new Number(s, (e) =>
                {
                    float ret = values[0].Value(e);
                    for (int i = 1; i < values.Length; i++)
                    {
                        ret /= values[i].Value(e);
                    }
                    return ret;
                });
            }
            if (s.StartsWith("?"))
            {
                s = s.Substring(1);
                if (s.Contains(":"))
                {
                    string[] ss = s.Split(':');
                    ss[0] = ss[0];
                    string tag = ss[1];
                    ss[1] = ss[1].ToLower();
                    if (ss.Length == 2)
                    {
                        Func<Script.Executor, float> ret = (e) =>
                        {
                            Sprite sprite = getSprite(ss[0], game, e).FirstOrDefault();
                            if (sprite is object)
                            {
                                switch (ss[1])
                                {
                                    case "x":
                                        return sprite.X - game.CurrentRoom.GetX;
                                    case "y":
                                        return sprite.Y - game.CurrentRoom.GetY;
                                    case "centerx":
                                        return sprite.CenterX - game.CurrentRoom.GetX;
                                    case "centery":
                                        return sprite.CenterY - game.CurrentRoom.GetY;
                                    case "right":
                                        return sprite.Right - game.CurrentRoom.GetX;
                                    case "bottom":
                                        return sprite.Bottom - game.CurrentRoom.GetY;
                                    case "trinkets":
                                        return sprite is Crewman ? (sprite as Crewman).PendingTrinkets.Count + (sprite as Crewman).HeldTrinkets.Count : 0;
                                    case "gravity":
                                        return sprite.Gravity;
                                    case "direction":
                                        return sprite.FlipX ? 1 : -1;
                                    case "input":
                                        return sprite is Crewman ? (sprite as Crewman).InputDirection : 0;
                                }
                                if (sprite.Tags.TryGetValue(tag, out float tagValue))
                                    return tagValue;
                            }
                            return 0f;
                        };
                        return new Number(s, ret);
                    }
                }
                else
                {
                    switch (s.ToLower())
                    {
                        case "trinkets":
                            return new Number(s, (e) => game.CollectedTrinkets.Count);
                        case "totaltrinkets":
                            return new Number(s, (e) => game.LevelTrinkets.Count);
                        case "roomx":
                            return new Number(s, (e) => game.CurrentRoom.X);
                        case "roomy":
                            return new Number(s, (e) => game.CurrentRoom.Y);
                        case "camerax":
                            return new Number(s, (e) => game.CameraX);
                        case "cameray":
                            return new Number(s, (e) => game.CameraY);
                        case "input":
                            return new Number(s, (e) => game.ActivePlayer.InputDirection);
                        case "action":
                            return new Number(s, (e) => game.IsInputActive(Game.Inputs.Jump) ? 1 : 0);
                        default:
                            break;
                    }
                }
            }
            else if (s.StartsWith("@"))
            {
                string[] args = s.Substring(1).Split(':');
                if (args.Length > 0) args[0] = args[0].ToLower();
                else args = new string[] { "" };
                if (args[0] == "key")
                {
                    string k = args.ElementAtOrDefault(1) ?? "";
                    if (k.Length == 1 && int.TryParse(k, out int _)) k = "Number" + k;
                    Enum.TryParse(k, true, out Key key);
                    return new Number(s, (e) => game.IsKeyHeld(key) ? 1 : 0);
                }
                else if (args[0] == "rand")
                {
                    Number min = getNumber(args.ElementAtOrDefault(1) ?? "", game);
                    Number max = getNumber(args.ElementAtOrDefault(2) ?? "", game);
                    return new Number(s, (e) =>
                    {
                        return new Random().Next((int)min.Value(e), (int)max.Value(e) + 1);
                    });
                }
                else
                    return 0;
            }
            if (game.Vars.ContainsKey(s))
                return game.Vars[s];
            else
            {
                if (Number.TryParse(s, out Number ret))
                    return ret;
                else
                {
                    Number n = new Number(s, 0);
                    game.Vars.Add(s, n);
                    return n;
                }
            }
        }

        private static string FormatNumber(float v, string format)
        {
            string formatC = format;
            format = format.ToLower();
            if (format == "timer")
            {
                float s = v % 60;
                float m = (int)(v / 60) % 60;
                float h = (int)(v / 3600) % 60;
                string ret = "";
                if (h > 0) ret = h.ToString() + ":";
                if (m > 0 || h > 0)
                {
                    string min = m.ToString();
                    if (min.Length == 1 && h > 0) min = "0" + min;
                    ret += min + ":";
                }
                string sec = s.ToString("F2");
                if ((m > 0 || h > 0) && (sec.Length == 1 || sec.IndexOf('.') == 1))
                    sec = "0" + sec;
                ret += sec;
                return ret;
            }
            bool spell = false;
            if (format == "true" || format == "spell") return SpellNumber(v);
            else if (format == "false" || format == "") return v.ToString();
            else if (format.Contains("ss"))
            {
                string ret = "";
                int index = 0;
                int seconds = (int)v % 60;
                if (format.Contains("mm"))
                {
                    int minutes = (int)v / 60;
                    if (format.Contains("h"))
                    {
                        minutes %= 60;
                        int hours = (int)v / 3600;
                        int h = 1;
                        index = format.IndexOf("h");
                        while (format.Length > index + h && format[index + h] == 'h')
                            h += 1;
                        string hrs = hours.ToString();
                        while (hrs.Length < h)
                            hrs = "0" + hrs;
                        ret += hrs;
                        int toM = format.IndexOf("mm", index + h);
                        if (toM > -1)
                        {
                            ret += formatC.Substring(index + h, toM - index - h);
                        }
                    }
                    int m = 1;
                    index = format.IndexOf("mm");
                    while (format.Length > index + m && format[index + m] == 'm')
                        m += 1;
                    string mins = minutes.ToString();
                    while (mins.Length < m)
                        mins = "0" + mins;
                    ret += mins;
                    int toS = format.IndexOf("ss", index + m);
                    if (toS > -1)
                    {
                        ret += formatC.Substring(index + m, toS - index - m);
                    }
                }
                int s = 1;
                index = format.IndexOf("ss");
                while (format.Length > index + s && format[index + s] == 's')
                    s += 1;
                string secs = seconds.ToString();
                while (secs.Length < s)
                    secs = "0" + secs;
                ret += secs;
                ret += formatC.Substring(index + s);
                return ret;
            }
            if (format.EndsWith("s"))
            {
                spell = true;
                format = format.Substring(0, format.Length - 1);
            }
            int point = format.LastIndexOfAny(new char[] { '.', ',', ':', '-' });
            string dp = point == -1 ? "" : format.Substring(point + 1);
            string np = point == -1 ? format : format.Substring(0, point);
            if (dp != "x")
            {
                double d = v;
                double e = Math.Pow(10, dp.Length);
                d *= e;
                d = Math.Truncate(d);
                d /= e;
                v = (float)d;
            }
            if (spell)
                return SpellNumber(v);
            else
            {
                string ret = v.ToString();
                string[] n = ret.Split('.');
                List<char> markers = new List<char>();
                List<int> places = new List<int>();
                char decp = '.';
                int sd = 0;
                for (int i = np.Length - 1; i > -1; i--)
                {
                    if (np[i] == '0')
                        sd++;
                    else if (np[i] == '.' || np[i] == ',' || np[i] == ':' || np[i] == ':')
                    {
                        markers.Add(np[i]);
                        places.Add(np.Length - i);
                    }
                }
                if (format.Length > np.Length)
                    decp = format[np.Length];
                ret = n[0];
                while (ret.Length < sd)
                {
                    ret = ret.Insert(0, "0");
                }
                for (int i = markers.Count - 1; i > -1; i--)
                {
                    int p = ret.Length - places[i] + 1;
                    if (p > 0)
                        ret = ret.Insert(p, markers[i].ToString());
                }
                if (n.Length == 2 || (dp.Length > 0 && dp != "x"))
                {
                    string ds;
                    if (n.Length == 2) ds = n[1];
                    else ds = "";
                    while (ds.Length < dp.Length)
                    {
                        ds += "0";
                    }
                    ret += decp;
                    ret += ds;
                }
                return ret;
            }
        }

        private static string[] ones = new string[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        private static string[] tens = new string[] { "Zero", "Ten", "Twenty", "Thirty", "Fourty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
        private static string[] thousands = new string[] { "Zero", "Thousand", "Million", "Billion", "Trillion" };
        public static string SpellNumber(float v, string format = null)
        {
            if (v > 999999999999999f)
                return "WAY too many";
            string s = "";
            if (v < 0)
            {
                s = "Negative ";
                v = Math.Abs(v);
            }
            string n = format is object ? v.ToString(format) : v.ToString();
            string f = "";
            if (n.Contains('.'))
            {
                f = n.Split('.').Last();
                n = n.Split('.').First();
            }
            if ((int)v > -1 && (int)v < 20) s += ones[(int)v];
            else
            {
                int fl = n.Length;
                string toSpell = "";
                bool needsAnd = false;
                bool needsDash = false;
                for (int i = 0; i < fl; i++)
                {
                    int place = (fl - i - 1) % 3;
                    int thPlace = (fl - i - 1) / 3;
                    if (thPlace == 0)
                    {
                        if (!int.TryParse(n[i].ToString(), out int digit))
                            return "NaN";
                        if (digit > 0)
                        {
                            switch (place)
                            {
                                case 0:
                                    if (digit > 0)
                                    {
                                        if (needsAnd)
                                        {
                                            s += " and ";
                                            needsAnd = false;
                                        }
                                        else if (needsDash)
                                            s += "-";
                                        else
                                            s += " ";
                                        s += ones[digit];
                                    }
                                    break;
                                case 1:
                                    if (needsAnd)
                                    {
                                        s += " and ";
                                        needsAnd = false;
                                    }
                                    else s += " ";
                                    if (digit == 1)
                                    {
                                        toSpell = n.Substring(i, 2);
                                        int spl = int.Parse(toSpell);
                                        toSpell = SpellNumber(spl);
                                        s += toSpell;
                                        i += 1;
                                        toSpell = "";
                                    }
                                    else if (digit > 0)
                                    {
                                        s += tens[digit];
                                        needsDash = true;
                                    }
                                    break;
                                case 2:
                                    s += " ";
                                    if (digit > 0)
                                    {
                                        needsAnd = true;
                                        s += ones[digit];
                                        s += " Hundred";
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        toSpell += n[i];
                        if (place == 0)
                        {
                            int spl = int.Parse(toSpell);
                            if (spl > 0)
                            {
                                toSpell = SpellNumber(spl);
                                s += " " + toSpell + " ";
                                s += thousands[thPlace];
                                needsAnd = true;
                                toSpell = "";
                            }
                        }
                    }
                }
            }
            if (f != "")
            {
                switch (f)
                {
                    case "5":
                        s += " and a Half";
                        break;
                    case "25":
                        s += " and a Quarter";
                        break;
                    case "75":
                        s += "And Three Quarters";
                        break;
                    default:
                        s += " point";
                        for (int i = 0; i < f.Length; i++)
                        {
                            if (int.TryParse(f[i].ToString(), out int j))
                                s += " " + ones[j];
                        }
                        break;
                }
            }
            return s.Trim();
        }

        public static SortedSet<string> presetcolors = new SortedSet<string> { "viridian", "vermilion", "vitellary", "verdigris", "violet", "victoria", "gray", "terminal" };
        private delegate Command cmd(Game game, string[] args);
        private static Dictionary<string, Syntax> cmdTypes = new Dictionary<string, Syntax> {
            { "say", new Syntax(SayCommand, new ArgTypes[] { ArgTypes.Int, ArgTypes.Color }) },
            { "text", new Syntax(TextCommand, new ArgTypes[] { ArgTypes.Color, ArgTypes.Number, ArgTypes.Number, ArgTypes.Int }) },
            { "changefont", new Syntax(ChangeFontCommand, new ArgTypes[] { ArgTypes.Texture }) },
            { "delay", new Syntax(WaitCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "playercontrol", new Syntax(PlayerControlCommand, new ArgTypes[] { ArgTypes.Bool }) },
            { "mood", new Syntax(MoodCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Mood }) },
            { "checkpoint", new Syntax(CheckpointCommand, new ArgTypes[] { }) },
            { "position", new Syntax(PositionCommand, new ArgTypes[] { ArgTypes.Position1, ArgTypes.Position2 }) },
            { "speak", new Syntax(SpeakCommand, new ArgTypes[] { }) },
            { "speak_active", new Syntax(SpeakActiveCommand, new ArgTypes[] { }) },
            { "showtext", new Syntax(ShowTextCommand, new ArgTypes[] { }) },
            { "endtext", new Syntax(EndTextCommand, new ArgTypes[] { }) },
            { "squeak", new Syntax(SqueakCommand, new ArgTypes[] { ArgTypes.Squeak }) },
            { "playef", new Syntax(PlaySoundCommand, new ArgTypes[] { ArgTypes.Sound }) },
            { "playsound", new Syntax(PlaySoundCommand, new ArgTypes[] { ArgTypes.Sound }) },
            { "addsprite", new Syntax(AddSpriteCommand, new ArgTypes[] { ArgTypes.Sprite }) },
            { "changeai", new Syntax(ChangeAICommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.AI, ArgTypes.Sprite }) },
            { "shake", new Syntax(ShakeCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "flash", new Syntax(FlashCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "musicfadeout", new Syntax(MusicFadeOutCommand, new ArgTypes[] { }) },
            { "musicfadein", new Syntax(MusicFadeInCommand, new ArgTypes[] { }) },
            { "pausemusic", new Syntax(PauseMusicCommand, new ArgTypes[] { }) },
            { "freeze", new Syntax(FreezeCommand, new ArgTypes[] { }) },
            { "semifreeze", new Syntax(SemiFreezeCommand, new ArgTypes[] { }) },
            { "unfreeze", new Syntax(UnfreezeCommand, new ArgTypes[] { }) },
            { "replace", new Syntax(ReplaceCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.Number, ArgTypes.NumberFormat }) },
            { "normalizetext", new Syntax(NormalizeTextCommand, new ArgTypes[] { }) },
            { "walk", new Syntax(WalkCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number }) },
            { "item", new Syntax(ItemCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Script }) },
            { "fadein", new Syntax(FadeInCommand, new ArgTypes[] { }) },
            { "fadeout", new Syntax(FadeOutCommand, new ArgTypes[] { }) },
            { "untilfade", new Syntax(UntilFadeCommand, new ArgTypes[] { }) },
            { "if", new Syntax(IfCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Script, ArgTypes.If }) },
            { "elseif", new Syntax(IfCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Script, ArgTypes.If }) },
            { "else", new Syntax(ElseCommand, new ArgTypes[] { ArgTypes.Script, ArgTypes.If }) },
            { "createenemy", new Syntax(CreateEnemyCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Texture, ArgTypes.Animation, ArgTypes.Number, ArgTypes.Number, ArgTypes.SpriteName, ArgTypes.Bool }) },
            { "setposition", new Syntax(SetPositionCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.PosX, ArgTypes.Number, ArgTypes.PosY, ArgTypes.Number }) },
            { "setbounds", new Syntax(SetBoundsCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "flipy", new Syntax(FlipYCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Bool }) },
            { "deletesprite", new Syntax(DeleteSpriteCommand, new ArgTypes[] { ArgTypes.Sprite }) },
            { "createtimer", new Syntax(CreateTimerCommand, new ArgTypes[] { ArgTypes.Script, ArgTypes.Number }) },
            { "music", new Syntax(ChangeMusicCommand, new ArgTypes[] { ArgTypes.Music }) },
            { "play", new Syntax(ChangeMusicCommand, new ArgTypes[] { ArgTypes.Music }) },
            { "exitcondition", new Syntax(ExitConditionCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "exitif", new Syntax(ExitConditionCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "tilecolor", new Syntax(TileColorCommand, new ArgTypes[] { ArgTypes.Color }) },
            { "set", new Syntax(SetCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number }) },
            { "do", new Syntax(DoCommand, new ArgTypes[] { }) },
            { "goto", new Syntax(GoToCommand, new ArgTypes[] { ArgTypes.Marker }) },
            { "while", new Syntax(WhileCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "addroom", new Syntax(AddRoomCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "autoscroll", new Syntax(AutoScrollCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "disableautoscroll", new Syntax(DisableAutoScrollCommand, new ArgTypes[] { }) },
            { "setplayer", new Syntax(SetPlayerCommand, new ArgTypes[] { ArgTypes.Sprite }) },
            { "cutscene", new Syntax(CutsceneCommand, new ArgTypes[] { }) },
            { "endcutscene", new Syntax(EndCutsceneCommand, new ArgTypes[] { }) },
            { "untilbars", new Syntax(UntilBarsCommand, new ArgTypes[] { }) },
            { "changeanimation", new Syntax(ChangeAnimationCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Animation, ArgTypes.None }) },
            { "createcrewman", new Syntax(CreateCrewmanCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Texture, ArgTypes.SpriteName }) },
            { "gotoroom", new Syntax(GoToRoomCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number}) },
            { "restore", new Syntax(RestoreCommand, new ArgTypes[] { ArgTypes.Sprite }) },
            { "setgravity", new Syntax(SetGravityCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number })},
            { "befadein", new Syntax(BeFadeInCommand, new ArgTypes[] { }) },
            { "destroy", new Syntax(DestroyCommand, new ArgTypes[] { ArgTypes.Type }) },
            { "createactivityzone", new Syntax(CreateActivityZoneCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number, ArgTypes.Number, ArgTypes.Script, ArgTypes.Color, ArgTypes.None, ArgTypes.Script, ArgTypes.Script }) },
            { "createsprite", new Syntax(CreateSpriteCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Texture, ArgTypes.Animation, ArgTypes.SpriteName }) },
            { "flip", new Syntax(FlipCommand, new ArgTypes[] { ArgTypes.Sprite }) },
            { "waituntil", new Syntax(WaitUntilCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "waitfor", new Syntax(WaitUntilCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "createplatform", new Syntax(CreatePlatformCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Texture, ArgTypes.Animation, ArgTypes.SpriteName, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Bool, ArgTypes.Animation}) },
            { "setcolor", new Syntax(SetColorCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Color }) },
            { "createwarptoken", new Syntax(CreateWarpToken, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Texture, ArgTypes.Animation, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number}) },
            { "scrollbounds", new Syntax(ScrollBoundsCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "doublejump", new Syntax(DoubleJumpCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number }) },
            { "kill", new Syntax(KillCommand, new ArgTypes[] { ArgTypes.Sprite }) },
            { "rand", new Syntax(RandomCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "ondeath", new Syntax(DeathScriptCommand, new ArgTypes[] { ArgTypes.Script }) },
            { "onrespawn", new Syntax(RespawnScriptCommand, new ArgTypes[] { ArgTypes.Script }) },
            { "hudtext", new Syntax(HudTextCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.Number, ArgTypes.Number, ArgTypes.Color, ArgTypes.Int }) },
            { "hudreplace", new Syntax(HudReplaceCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.None, ArgTypes.Number, ArgTypes.NumberFormat }) },
            { "hudsize", new Syntax(HudSizeCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.Number }) },
            { "hudremove", new Syntax(HudRemovCommand, new ArgTypes[] { ArgTypes.None }) },
            { "setnumber", new Syntax(SetNumberCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Property, ArgTypes.Number }) },
            { "setbool", new Syntax(SetBoolCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Property, ArgTypes.Bool }) },
            { "settag", new Syntax(SetTagCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.None, ArgTypes.Number }) },
            { "respawn", new Syntax(RespawnCommand, new ArgTypes[] { ArgTypes.Sprite }) },
            { "setspeed", new Syntax(SetSpeedCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number, ArgTypes.Number }) },
            { "trinket", new Syntax(TrinketCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Sprite }) }
        };

        public static ArgTypes[] GetArgs(string command)
        {
            if (cmdTypes.TryGetValue(command, out Syntax s))
                return s.args;
            else
                return new ArgTypes[] { };
        }

        private class Syntax
        {
            public cmd command;
            public ArgTypes[] args;
            public Syntax(cmd cmd, ArgTypes[] arg)
            {
                command = cmd;
                args = arg;
            }
        }

        public enum ArgTypes { None, Command, Identifier, Property, Int, Sprite, Texture, Animation, Sound, Color, Bool, Number, Mood, Position1, Position2, AI, Squeak, Script, If, SpriteName, PosX, PosY, Music, NumberFormat, Marker, Type }

        public static string[] CommandNames
        {
            get
            {
                List<string> ret = cmdTypes.Keys.ToList();
                ret.Sort();
                return ret.ToArray();
            }
        }

        public static Command[] ParseScript(Game game, string script)
        {
            string[] lines = script.Replace(Environment.NewLine, "\n").Split(new char[] { '\n' });
            List<Command> commands = new List<Command>();
            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i++];
                List<string> argsList = line.Split(new char[] { ',', '(', ')' }).ToList();
                if (line.StartsWith("#"))
                {
                    continue;
                }
                else if (line.StartsWith(">"))
                {
                    line = line.Substring(1);
                    
                }
                for (int j = 0; j < argsList.Count; j++)
                {
                    if (argsList[j] == "") argsList.RemoveAt(j);
                    else break;
                }
                for (int j = argsList.Count - 1; j > -1; j--)
                {
                    if (argsList[j] == "") argsList.RemoveAt(j);
                    else break;
                }
                if (argsList.Count == 0) continue;
                string[] args = argsList.ToArray();
                args[0] = args[0].ToLower();

                // multi-line commands
                int txLines = 0;
                if (args[0] == "say")
                {
                    if (!int.TryParse(args.ElementAtOrDefault(1), out txLines)) continue;
                }
                else if (args[0] == "text")
                {
                    if (!int.TryParse(args.LastOrDefault(), out txLines)) continue;
                }
                else if (args[0] == "hudtext")
                    if (!int.TryParse(args.ElementAtOrDefault(5), out txLines)) continue;
                if (txLines > 0)
                {
                    Array.Resize(ref args, args.Length + 1);
                    if (lines.Length >= i + txLines)
                        args[args.Length - 1] = string.Join("\n", lines, i, txLines);
                    i += txLines;
                }

                if (cmdTypes.ContainsKey(args[0]))
                    commands.Add(cmdTypes[args[0]].command(game, args));
            }
            return commands.ToArray();
        }

        private static Command SayCommand(Game game, string[] args)
        {
            Color sayTextBoxColor = Color.Gray;
            Crewman sayCrewman = game.SpriteFromName(args.ElementAtOrDefault(2) ?? "") as Crewman;
            SoundEffect squeak = null;
            if (sayCrewman != null)
            {
                sayTextBoxColor = sayCrewman.TextBoxColor;
                squeak = sayCrewman.Squeak;
            }
            else
            {
                sayTextBoxColor = game.GetColor(args.ElementAtOrDefault(2) ?? "").GetValueOrDefault(Color.White);
            }
            if (sayCrewman is null && presetcolors.Contains(args.ElementAtOrDefault(2) ?? ""))
            {
                switch (args[2])
                {
                    case "viridian":
                        squeak = game.GetSound("crew1");
                        break;
                    case "vermilion":
                        squeak = game.GetSound("crew6");
                        break;
                    case "vitellary":
                        squeak = game.GetSound("crew4");
                        break;
                    case "verdigris":
                        squeak = game.GetSound("crew2");
                        break;
                    case "violet":
                        squeak = game.GetSound("crew5");
                        break;
                    case "victoria":
                        squeak = game.GetSound("crew3");
                        break;
                    case "terminal":
                        squeak = game.GetSound("blip2");
                        break;
                }
            }

            return new Command(game, (e, sender, target) =>
            {
                if (sayCrewman is null && args.ElementAtOrDefault(2)?.ToLower() == "player")
                {
                    sayCrewman = game.ActivePlayer;
                    sayTextBoxColor = game.ActivePlayer.TextBoxColor;
                    squeak = game.ActivePlayer.Squeak;
                }
                VTextBox sayTextBox = new VTextBox(0, 0, game.FontTexture, args.Last(), sayTextBoxColor);
                if (sayCrewman is object && game.HasSprite(sayCrewman))
                {
                    sayTextBox.Bottom = sayCrewman.Y - 2 - game.CameraY;
                    sayTextBox.X = sayCrewman.X - 16 - game.CameraX;
                    if (sayTextBox.Right > Game.RESOLUTION_WIDTH - 8) sayTextBox.Right = Game.RESOLUTION_WIDTH - 8;
                    if (sayTextBox.Bottom > Game.RESOLUTION_HEIGHT - 8) sayTextBox.Bottom = Game.RESOLUTION_HEIGHT - 8;
                    if (sayTextBox.X < 8) sayTextBox.X = 8;
                    if (sayTextBox.Y < 8) sayTextBox.Y = 8;
                }
                else
                {
                    sayTextBox.CenterX = Game.RESOLUTION_WIDTH / 2;
                    sayTextBox.CenterY = Game.RESOLUTION_HEIGHT / 2;
                }
                sayTextBox.Layer = 50;
                game.hudSprites.Add(sayTextBox);
                game.TextBoxes.Add(sayTextBox);
                e.TextBoxes.Add(sayTextBox);
                squeak?.Play();
                sayTextBox.Appear();
                game.WaitingForAction = true;
                e.WaitingForAction = () =>
                {
                    sayTextBox.Disappear();
                    sayTextBox.Disappeared += (textBox) =>
                    {
                        game.hudSprites.Remove(textBox);
                        game.TextBoxes.Remove(textBox);
                        e.TextBoxes.Remove(sayTextBox);
                    };
                };
            }, true);
        }
        private static Command SqueakCommand(Game game, string[] args)
        {
            SoundEffect squeak = null;
            switch (args.ElementAtOrDefault(1))
            {
                case "viridian":
                    squeak = game.GetSound("crew1");
                    break;
                case "vermilion":
                    squeak = game.GetSound("crew6");
                    break;
                case "vitellary":
                    squeak = game.GetSound("crew4");
                    break;
                case "verdigris":
                    squeak = game.GetSound("crew2");
                    break;
                case "violet":
                    squeak = game.GetSound("crew5");
                    break;
                case "victoria":
                    squeak = game.GetSound("crew3");
                    break;
                case "valerie":
                    squeak = game.GetSound("crew7");
                    break;
                case "stigma":
                    squeak = game.GetSound("crew8");
                    break;
                case "gray":
                case "terminal":
                    squeak = game.GetSound("blip2");
                    break;
            }
            return new Command(game, (e, sender, target) => {
                if (args.ElementAtOrDefault(1).ToLower() == "player")
                    squeak = game.ActivePlayer.Squeak;
                else if (args.ElementAtOrDefault(1).ToLower() == "cry")
                    squeak = Crewman.Cry;
                squeak?.Play();
            }, false);
        }
        private static Command TextCommand(Game game, string[] args)
        {
            Color txTextBoxColor = Color.Gray;
            Crewman sayCrewman = game.SpriteFromName(args.ElementAtOrDefault(1)) as Crewman;
            int txArgOffset = 0;
            string c = args.ElementAtOrDefault(1) ?? "";
            if (sayCrewman != null)
            {
                txTextBoxColor = sayCrewman.TextBoxColor;
            }
            else if (presetcolors.Contains(c))
            {
                switch (args[1])
                {
                    case "viridian":
                        txTextBoxColor = Color.FromArgb(164, 164, 255);
                        break;
                    case "vermilion":
                        txTextBoxColor = Color.FromArgb(255, 60, 60);
                        break;
                    case "vitellary":
                        txTextBoxColor = Color.FromArgb(255, 255, 134);
                        break;
                    case "verdigris":
                        txTextBoxColor = Color.FromArgb(144, 255, 144);
                        break;
                    case "violet":
                        txTextBoxColor = Color.FromArgb(255, 134, 255);
                        break;
                    case "victoria":
                        txTextBoxColor = Color.FromArgb(95, 95, 255);
                        break;
                    case "gray":
                    case "terminal":
                        txTextBoxColor = Color.FromArgb(174, 174, 174);
                        break;
                }
            }
            else
            {
                Color? clr = game.GetColor(c);
                if (clr.HasValue)
                    txTextBoxColor = clr.Value;
            }
            Number txX = getNumber(args.ElementAtOrDefault(2 + txArgOffset), game);
            Number txY = getNumber(args.ElementAtOrDefault(3 + txArgOffset), game);
            return new Command(game, (e, sender, target) =>
            {
                VTextBox tb = new VTextBox(txX.Value(e), txY.Value(e), game.FontTexture, args.Last(), txTextBoxColor);
                tb.Layer = 50;
                e.TextBoxes.Add(tb);
                game.TextBoxes.Add(tb);
                game.hudSprites.Add(tb);
            });
        }
        private static Command ChangeFontCommand(Game game, string[] args)
        {
            string fontTexture = args.ElementAtOrDefault(1);
            Texture newFont = game.TextureFromName(fontTexture);
            Action<Script.Executor, Sprite, Sprite> success = (e, sender, target) => { };
            if (newFont != null && newFont.Width / newFont.TileSizeX == 16 && newFont.Height / newFont.TileSizeY == 16)
            {
                success = (e, sender, target) => {
                    game.FontTexture = newFont;
                };
            }
            return new Command(game, success, false);
        }
        private static Command WaitCommand(Game game, string[] args)
        {
            Number frames = getNumber(args.LastOrDefault(), game);
            return new Command(game, (e, sender, target) =>
            {
                e.WaitingFrames = (int)frames.Value(e);
            }, true);
        }
        private static Command PlayerControlCommand(Game game, string[] args)
        {
            bool.TryParse(args.ElementAtOrDefault(1), out bool pc);
            return new Command(game, (e, sender, target) =>
            {
                game.PlayerControl = pc;
                if (!pc)
                    game.ActivePlayer.InputDirection = 0;
            });
        }
        private static Command MoodCommand(Game game, string[] args)
        {
            string s = (args.ElementAtOrDefault(2) ?? "").ToLower();
            bool sad = (s == "sad" || s == "1");
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1) ?? "player", game, e);
                foreach (Sprite sprite in sprites)
                {
                    if (sprite is Crewman)
                        (sprite as Crewman).Sad = sad;
                }
            });
        }
        private static Command CheckpointCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Crewman p = game.ActivePlayer;
                p.CheckpointFlipX = p.FlipX;
                p.CheckpointFlipY = p.FlipY;
                p.CheckpointX = p.CenterX;
                p.CheckpointY = p.FlipY ? p.Y : p.Bottom;
                if (p.CurrentCheckpoint != null)
                {
                    p.CurrentCheckpoint.Deactivate();
                    p.CurrentCheckpoint = null;
                }
            });
        }
        private static Command PositionCommand(Game game, string[] args)
        {
            string p = args.ElementAtOrDefault(2);
            string c = args.ElementAtOrDefault(1);

            return new Command(game, (e, sender, target) =>
            {
                if (e.TextBoxes.Count > 0)
                {
                    VTextBox tb = e.TextBoxes.Last();
                    if (c.ToLower() == "centerx" || c.ToLower() == "center")
                        tb.CenterX = Game.RESOLUTION_WIDTH / 2;
                    if (c.ToLower() == "centery" || c.ToLower() == "center")
                        tb.CenterY = Game.RESOLUTION_HEIGHT / 2;
                    if (c.ToLower() == "top")
                        tb.Y = 4;
                    else if (c.ToLower() == "bottom")
                        tb.Bottom = Game.RESOLUTION_HEIGHT - 4;
                    Crewman crewman = game.SpriteFromName(c) as Crewman;
                    if (c.ToLower() == "player") crewman = game.ActivePlayer;
                    if (crewman != null)
                    {
                        if (p == "above")
                        {
                            tb.Bottom = crewman.Y - 2 - game.CameraY;
                            tb.X = crewman.X - 16 - game.CameraX;
                        }
                        else
                        {
                            tb.Y = crewman.Bottom + 2 - game.CameraY;
                            tb.X = crewman.X - 16 - game.CameraX;
                        }
                        if (tb.Right > Game.RESOLUTION_WIDTH - 8) tb.Right = Game.RESOLUTION_WIDTH - 8;
                        if (tb.Bottom > Game.RESOLUTION_HEIGHT - 8) tb.Bottom = Game.RESOLUTION_HEIGHT - 8;
                        if (tb.X < 8) tb.X = 8;
                        if (tb.Y < 8) tb.Y = 8;
                    }
                }
            });
        }
        private static Command SpeakCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                if (e.TextBoxes.Count > 0)
                {
                    VTextBox tb = e.TextBoxes.Last();
                    tb.Appear();
                    game.WaitingForAction = true;
                    e.WaitingForAction = () =>
                    {
                        
                    };
                }
            }, true);
        }
        private static Command SpeakActiveCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                if (e.TextBoxes.Count > 0)
                {
                    VTextBox tb = e.TextBoxes.Last();
                    for (int i = e.TextBoxes.Count - 2; i >= 0; i--)
                    {
                        e.TextBoxes[i].Disappear();
                        e.TextBoxes[i].Disappeared += (textBox) =>
                        {
                            game.hudSprites.Remove(textBox);
                            game.TextBoxes.Remove(textBox);
                            e.TextBoxes.Remove(textBox);
                        };
                    }
                    tb.Appear();
                    game.WaitingForAction = true;
                    e.WaitingForAction = () =>
                    {
                        
                    };
                }

            }, true);
        }
        private static Command ShowTextCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                if (e.TextBoxes.Count > 0)
                {
                    VTextBox tb = e.TextBoxes.Last();
                    tb.Appear();
                }
            });
        }
        private static Command ReplaceCommand(Game game, string[] args)
        {
            string replace = args.ElementAtOrDefault(1) ?? "";
            Number with = getNumber(args.ElementAtOrDefault(2), game);
            return new Command(game, (e, sender, target) =>
            {
                float w = with.Value(e);
                string replaceWith = FormatNumber(w, args.ElementAtOrDefault(3) ?? "false");
                if (replace != null && replace != "")
                {
                    VTextBox tb = e.TextBoxes.Last();
                    if (tb != null)
                    {
                        string t = tb.Text.Replace(replace, replaceWith);
                        tb.Text = t;
                    }
                }
            });
        }
        private static Command NormalizeTextCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                VTextBox tb = e.TextBoxes.LastOrDefault();
                if (tb != null)
                {
                    string t = tb.Text.Replace('\n', ' ');
                    int index = 0;
                    int lastLine = 0;
                    while (true)
                    {
                        int nextSpace = t.IndexOf(' ', index);
                        int nextDash = t.IndexOf('-', index);
                        if (nextDash > -1 && nextDash < nextSpace) nextSpace = nextDash;
                        if (nextSpace == -1) break;
                        if (nextSpace - lastLine > 36)
                        {
                            if (index > lastLine)
                            {
                                t = t.Remove(index - 1, 1);
                                t = t.Insert(index - 1, "\n");
                                lastLine = index;
                            }
                            else
                            {
                                t = t.Insert(lastLine + 36, "\n");
                                index += 37;
                            }
                        }
                        else
                            index = nextSpace + 1;
                    }
                    tb.Text = t;
                }
            });
        }
        private static Command EndTextCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                for (int i = game.TextBoxes.Count - 1; i >= 0; i--)
                {
                    game.TextBoxes[i].Disappear();
                    game.TextBoxes[i].Disappeared += (textBox) =>
                    {
                        game.hudSprites.Remove(textBox);
                        game.TextBoxes.Remove(textBox);
                        e.TextBoxes.Remove(textBox);
                    };
                }
            });
        }
        private static Command PlaySoundCommand(Game game, string[] args)
        {
            SoundEffect se = game.GetSound(args.LastOrDefault());
            return new Command(game, (e, sender, target) => { se.Play(); }, false);
        }
        private static Command AddSpriteCommand(Game game, string[] args)
        {
            Number x = getNumber(args.ElementAtOrDefault(2), game);
            Number y = getNumber(args.ElementAtOrDefault(3), game);
            Sprite s = game.SpriteFromName(args.ElementAtOrDefault(1));
            return new Command(game, (e, sender, target) =>
            {
                if (s != null)
                {
                    game.AddSprite(s, x.Value(e), y.Value(e));
                }
            }, false);
        }
        private static Command ChangeAICommand(Game game, string[] args)
        {
            string aiString = args.ElementAtOrDefault(2);
            Crewman.AIStates aiState = Crewman.AIStates.Stand;
            if (aiString != null)
            {
                if (aiString.ToLower() == "stand")
                    aiState = Crewman.AIStates.Stand;
                else if (aiString.ToLower() == "follow")
                    aiState = Crewman.AIStates.Follow;
                else if (aiString.ToLower() == "face")
                    aiState = Crewman.AIStates.Face;
            }
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                Crewman crewman2 = getSprite(args.ElementAtOrDefault(3) ?? "", game, e).FirstOrDefault() as Crewman;
                foreach (Sprite sprite in sprites)
                {
                    if (sprite is Crewman)
                    {
                        if (crewman2 is null) crewman2 = game.ActivePlayer;
                        (sprite as Crewman).AIState = aiState;
                        if (aiState != Crewman.AIStates.Stand)
                            (sprite as Crewman).Target = crewman2;
                    }
                }
            }, false);
        }
        private static Command ShakeCommand(Game game, string[] args)
        {
            Number frames = getNumber(args.ElementAtOrDefault(1), game);
            Number intensity = getNumber(args.ElementAtOrDefault(2), game);
            return new Command(game, (e, sender, target) =>
            {
                game.Shake((int)frames.Value(e), (int)intensity.Value(e));
            });
        }
        private static Command FlashCommand(Game game, string[] args)
        {
            Number frames = getNumber(args.ElementAtOrDefault(1), game);
            Number r = getNumber(args.ElementAtOrDefault(2), game);
            Number g = getNumber(args.ElementAtOrDefault(3), game);
            Number b = getNumber(args.ElementAtOrDefault(4), game);
            if (args.Length < 5) r = g = b = 255;
            return new Command(game, (e, sender, target) =>
            {
                game.Flash((int)frames.Value(e), (int)r.Value(e), (int)g.Value(e), (int)b.Value(e));
            });
        }
        private static Command MusicFadeOutCommand(Game game, string[] args)
        {
            Number speed = getNumber(args.LastOrDefault(), game);
            return new Command(game, (e, sender, target) =>
            {
                if (speed.Value(e) == 0) speed = 1;
                game.CurrentSong.FadeOut(speed.Value(e));
            }, false);
        }
        private static Command MusicFadeInCommand(Game game, string[] args)
        {
            Number speed = getNumber(args.LastOrDefault(), game);
            return new Command(game, (e, sender, target) =>
            {
                if (speed.Value(e) == 0) speed = 1;
                game.CurrentSong.FadeIn(speed.Value(e));
            }, false);
        }
        private static Command PauseMusicCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.CurrentSong.Pause();
                game.CurrentSong.Silence();
            });
        }
        private static Command FreezeCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.Freeze = Game.FreezeOptions.OnlySprites;
            });
        }
        private static Command SemiFreezeCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.Freeze = Game.FreezeOptions.OnlyMovement;
            });
        }
        private static Command UnfreezeCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.Freeze = Game.FreezeOptions.Unfrozen;
            });
        }
        private static Command WalkCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1), game, e);
                foreach (Sprite sprite in sprites)
                {
                    Crewman c = sprite as Crewman;
                    if (c is object)
                    {
                        if (int.TryParse(args.ElementAtOrDefault(2) ?? "0", out int p))
                        {
                            c.InputDirection = Math.Sign(p);
                        }
                    }
                }
            });
        }
        private static Command ItemCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                foreach (Sprite sprite in sprites)
                {
                    Crewman c = sprite as Crewman;
                    if (c is object)
                    {
                        Script s = game.ScriptFromName(args.Last());
                        if (s is object)
                        {
                            c.Script = s;
                        }
                    }
                }
            });
        }
        private static Command UntilFadeCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                e.Waiting = () => game.FadeSpeed == 0;
            }, true);
        }
        private static Command IfCommand(Game game, string[] args)
        {
            Number n = getNumber(args.ElementAtOrDefault(1), game);
            bool isElseif = args[0].ToLower() == "elseif";
            string script = args.ElementAtOrDefault(2) ?? "";
            string option = (args.ElementAtOrDefault(3) ?? "").ToLower();
            return new Command(game, (e, sender, target) =>
            {
                if (isElseif && e.IfSatisfied)
                {
                    e.Continue();
                    return;
                }
                if (n.Value(e) != 0)
                {
                    e.IfSatisfied = true;
                    Script.Executor scr = game.ExecuteScript(game.ScriptFromName(script), sender, target);
                    if (scr is object)
                        for (int i = 0; i < e.CreatedSprites.Count; i++)
                        {
                            scr.CreatedSprites.Add(e.CreatedSprites.Keys[i], e.CreatedSprites.Values[i]);
                        }
                    switch (option)
                    {
                        case "wait":
                            if (scr is object)
                                scr.Finished += (sc) => { e.Continue(); };
                            break;
                        case "stop":
                            e.Stop();
                            break;
                        default:
                            e.Continue();
                            break;
                    }
                }
                else
                {
                    e.IfSatisfied = false;
                    e.Continue();
                }
            }, true);
        }
        private static Command ElseCommand(Game game, string[] args)
        {
            string script = args.ElementAtOrDefault(1) ?? "";
            string option = (args.ElementAtOrDefault(2) ?? "").ToLower();
            return new Command(game, (e, sender, target) =>
            {
                if (e.IfSatisfied)
                {
                    e.Continue();
                    return;
                }
                Script.Executor scr = null;
                scr = game.ExecuteScript(game.ScriptFromName(script), sender, target);
                if (scr is object)
                    switch (option)
                    {
                        case "wait":
                            scr.Finished += (sc) => { e.Continue(); };
                            break;
                        case "stop":
                            e.Stop();
                            break;
                        default:
                            e.Continue();
                            break;
                    }
                e.IfSatisfied = true;
            }, true);
        }
        private static Command CreateEnemyCommand(Game game, string[] args)
        {
            Number x = getNumber(args.ElementAtOrDefault(1) ?? "", game);
            Number y = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            string texture = args.ElementAtOrDefault(3) ?? "";
            string animation = args.ElementAtOrDefault(4) ?? "";
            Number xSp = getNumber(args.ElementAtOrDefault(5) ?? "", game);
            Number ySp = getNumber(args.ElementAtOrDefault(6) ?? "", game);
            string name = args.ElementAtOrDefault(7) ?? "";
            if (!bool.TryParse(args.ElementAtOrDefault(8) ?? "", out bool s))
                s = true;
            return new Command(game, (e, sender, target) =>
            {
                Texture t = game.TextureFromName(texture);
                Animation a = t?.AnimationFromName(animation);
                if (a is object)
                {
                    Enemy enemy = new Enemy(x.Value(e), y.Value(e), t, a, xSp.Value(e), ySp.Value(e), game.CurrentRoom.Color);
                    enemy.Name = name;
                    enemy.SyncAnimation(game.FrameCount);
                    if (!s)
                        enemy.Solid = Sprite.SolidState.NonSolid;
                    e.CreatedSprites.Remove(name);
                    e.CreatedSprites.Add(name, enemy);
                    game.AddSprite(enemy, enemy.X, enemy.Y);
                }
            });
        }
        private static Command SetPositionCommand(Game game, string[] args)
        {
            string sprite = args.ElementAtOrDefault(1) ?? "";
            string xAlign = args.ElementAtOrDefault(2)?.ToLower() ?? "x";
            Number x = getNumber(args.ElementAtOrDefault(3) ?? "", game);
            string yAlign = args.ElementAtOrDefault(4)?.ToLower() ?? "y";
            Number y = getNumber(args.ElementAtOrDefault(5) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                Sprite s = getSprite(sprite, game, e).FirstOrDefault();
                if (s is object)
                    switch (xAlign)
                    {
                        case "centerx":
                            s.CenterX = x.Value(e) + game.CurrentRoom.GetX;
                            break;
                        case "right":
                            s.Right = x.Value(e) + game.CurrentRoom.GetX;
                            break;
                        default:
                            s.X = x.Value(e) + game.CurrentRoom.GetX;
                            break;
                    }
                switch (yAlign)
                {
                    case "centery":
                        s.CenterY = y.Value(e) + game.CurrentRoom.GetY;
                        break;
                    case "bottom":
                        s.Bottom = y.Value(e) + game.CurrentRoom.GetY;
                        break;
                    default:
                        s.Y = y.Value(e) + game.CurrentRoom.GetY;
                        break;
                }
            });
        }
        private static Command SetBoundsCommand(Game game, string[] args)
        {
            string sprite = args.ElementAtOrDefault(1) ?? "";
            Number x = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            Number y = getNumber(args.ElementAtOrDefault(3) ?? "", game);
            Number w = getNumber(args.ElementAtOrDefault(4) ?? "", game);
            Number h = getNumber(args.ElementAtOrDefault(5) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(sprite, game, e);
                foreach (Sprite spr in sprites)
                {
                    IBoundSprite s = spr as IBoundSprite;
                    if (s is object)
                    {
                        s.Bounds = new Rectangle((int)x.Value(e) + (int)game.CurrentRoom.GetX - (int)s.InitialX, (int)y.Value(e) + (int)game.CurrentRoom.GetY - (int)s.InitialY, (int)w.Value(e), (int)h.Value(e));
                    }
                }
            });
        }
        private static Command FlipYCommand(Game game, string[] args)
        {
            string sprite = args.ElementAtOrDefault(1)?.ToLower() ?? "";
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] s = getSprite(sprite, game, e);
                foreach (Sprite spr in s)
                {
                    spr.FlipY = !spr.FlipY;
                }
            });
        }
        private static Command DeleteSpriteCommand(Game game, string[] args)
        {
            string sprite = args.ElementAtOrDefault(1) ?? "";
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] s = getSprite(sprite, game, e);
                foreach (Sprite spr in s)
                    game.RemoveSprite(spr);
            });
        }
        private static Command CreateTimerCommand(Game game, string[] args)
        {
            string script = args.ElementAtOrDefault(1) ?? "";
            Number interval = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                Script s = game.ScriptFromName(script);
                if (s is object)
                {
                    Timer timer = new Timer(s, (int)interval.Value(e), target, game);
                    game.AddSprite(timer, 0, 0);
                }
            });
        }
        private static Command ChangeMusicCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Music music = game.GetMusic(args.ElementAtOrDefault(1) ?? "");
                game.CurrentSong.FadeOut();
                game.MusicFaded = () =>
                {
                    game.CurrentSong = music;
                    music.Silence();
                    music.FadeIn();
                };
            });
        }
        private static Command ExitConditionCommand(Game game, string[] args)
        {
            Number ec = getNumber(args.ElementAtOrDefault(1) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                e.ExitCondition = () => ec.Value(e) != 0;
            });
        }
        private static Command TileColorCommand(Game game, string[] args)
        {
            Color c = game.GetColor(args.ElementAtOrDefault(1) ?? "").GetValueOrDefault(Color.White);
            return new Command(game, (e, sender, target) =>
            {
                game.SetTilesColor(c);
                if (game.BGSprites.InheritRoomColor)
                    game.BGSprites.BaseColor = c;
            });
        }
        private static Command SetCommand(Game game, string[] args)
        {
            Number n = getNumber(args.ElementAtOrDefault(1) ?? "", game);
            Number s = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                n.SetValue(s.Value(e));
            });
        }
        private static Command DoCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                e.AddMarker();
            });
        }
        private static Command WhileCommand(Game game, string[] args)
        {
            Number condition = getNumber(args.ElementAtOrDefault(1) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                if (condition.Value(e) != 0)
                    e.ReturnToMarker();
                else
                    e.RemoveMarker();
            });
        }
        private static Command GoToCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                e.GoToMarker(args.ElementAtOrDefault(1) ?? "");
            });
        }
        private static Command AddRoomCommand(Game game, string[] args)
        {
            Number rx = getNumber(args.ElementAtOrDefault(1) ?? "", game);
            Number ry = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            Number x = getNumber(args.ElementAtOrDefault(3) ?? "", game);
            Number y = getNumber(args.ElementAtOrDefault(4) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                int roomID = (int)(rx.Value(e) + ry.Value(e) * 100);
                if (game.RoomDatas.ContainsKey(roomID))
                    game.CurrentRoom.AddRoom(game.RoomDatas[roomID], game, (int)x.Value(e), (int)y.Value(e));
            });
        }
        private static Command AutoScrollCommand(Game game, string[] args)
        {
            Number x = getNumber(args.ElementAtOrDefault(1) ?? "", game);
            Number y = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            Number mx = getNumber(args.ElementAtOrDefault(3) ?? "", game);
            Number my = getNumber(args.ElementAtOrDefault(4) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                game.AutoScrollX = x.Value(e);
                game.AutoScrollY = y.Value(e);
                game.StopScrollX = mx.Value(e);
                game.StopScrollY = my.Value(e);
                game.MinScrollX = Math.Min(game.CameraX, game.StopScrollX);
                game.MaxScrollX = Math.Max(game.CameraX, game.StopScrollX);
                game.MinScrollY = Math.Min(game.CameraY, game.StopScrollY);
                game.MaxScrollY = Math.Max(game.CameraY, game.StopScrollY);
                game.AutoScroll = true;
            });
        }
        private static Command DisableAutoScrollCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.AutoScroll = false;
                game.AutoScrollX = 0;
                game.AutoScrollY = 0;
            });
        }
        private static Command SetPlayerCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Crewman c = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).FirstOrDefault() as Crewman;
                if (c is object)
                    game.SetPlayer(c);
            });
        }
        private static Command CutsceneCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) => { game.Cutscene(); });
        }
        private static Command EndCutsceneCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) => { game.EndCutscene(); });
        }
        private static Command UntilBarsCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                e.Waiting = () => game.CutsceneBars == 0;
            }, true);
        }
        private static Command ChangeAnimationCommand(Game game, string[] args)
        {
            string property = args.ElementAtOrDefault(3);
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] spr = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                foreach (Sprite s in spr)
                {
                    if (s is object && s.Texture is object)
                    {
                        Animation anim = s.Texture.AnimationFromName(args.ElementAtOrDefault(2) ?? "");
                        if (anim is object)
                        {
                            if (!string.IsNullOrEmpty(property))
                            {
                                s.SetProperty(property, anim.Name, game);
                            }
                            else
                                s.Animation = anim;
                        }
                    }
                }
            });
        }
        private static Command CreateCrewmanCommand(Game game, string[] args)
        {
            Number x = getNumber(args.ElementAtOrDefault(1) ?? "", game);
            Number y = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                Texture t = game.TextureFromName(args.ElementAtOrDefault(3) ?? "");
                string name = args.ElementAtOrDefault(4) ?? "";
                if (name == "") name = t?.Name ?? "crewman";
                Crewman c = new Crewman(x.Value(e), y.Value(e), t, game, name);
                if (!e.CreatedSprites.ContainsKey(c.Name))
                    e.CreatedSprites.Add(c.Name, c);
                game.AddSprite(c, c.X, c.Y);
            });
        }
        private static Command GoToRoomCommand(Game game, string[] args)
        {
            Number x = getNumber(args.ElementAtOrDefault(1) ?? "", game);
            Number y = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                game.LoadRoom((int)x.Value(e), (int)y.Value(e));
            });
        }
        private static Command FadeOutCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.FadeOut();
            });
        }
        private static Command FadeInCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.FadeIn();
            });
        }
        private static Command RestoreCommand(Game game, string[] args)
        {
            string spr = (args.ElementAtOrDefault(1) ?? "").ToLower();
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] s = getSprite(spr, game, e);
                foreach (Sprite sprite in s)
                {
                    if (sprite is IScriptExecutor)
                    {
                        (sprite as IScriptExecutor).Activated = false;
                    }
                    if (sprite?.ActivityZone is object)
                    {
                        sprite.ActivityZone.Activated = false;
                    }
                }
            });
        }
        private static Command SetGravityCommand(Game game, string[] args)
        {
            string spr = (args.ElementAtOrDefault(1) ?? "").ToLower();
            Number g = getNumber(args.ElementAtOrDefault(2) ?? "", game);
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(spr, game, e);
                foreach (Sprite c in sprites)
                {
                    if (c is object)
                    {
                        c.Gravity = g.Value(e) * 0.6875f;
                    }
                }
            });
        }
        private static Command BeFadeInCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.FadeIn();
                game.FadeSpeed = 255;
            });
        }
        private static Command DestroyCommand(Game game, string[] args)
        {
            Type t = null;
            switch ((args.ElementAtOrDefault(1) ?? "").ToLower())
            {
                case "gravitylines":
                    t = typeof(GravityLine);
                    break;
                case "platforms":
                    t = typeof(Platform);
                    break;
                case "warptokens":
                    t = typeof(WarpToken);
                    break;
                case "warplines":
                    t = typeof(WarpLine);
                    break;
                case "enemies":
                    t = typeof(Enemy);
                    break;
                case "scriptboxes":
                    t = typeof(ScriptBox);
                    break;
                case "sprites":
                    t = typeof(Sprite);
                    break;
                case "checkpoints":
                    t = typeof(Checkpoint);
                    break;

            }
            return new Command(game, (e, sender, target) =>
            {
                if (t is object)
                {
                    if (t != typeof(Sprite))
                        game.Destroy(t);
                    else
                    {
                        foreach (Sprite sprite in e.CreatedSprites.Values)
                        {
                            game.RemoveSprite(sprite);
                        }
                        e.CreatedSprites.Clear();
                    }
                }
            });
        }
        private static Command CreateActivityZoneCommand(Game game, string[] args)
        {
            string sn = args.ElementAtOrDefault(1) ?? "";
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] s = getSprite(sn, game, e);
                Number w = getNumber(args.ElementAtOrDefault(2) ?? "", game);
                Number h = getNumber(args.ElementAtOrDefault(3) ?? "", game);
                Script sc = game.ScriptFromName(args.ElementAtOrDefault(4) ?? "");
                Color c = game.GetColor(args.ElementAtOrDefault(5) ?? "", sender, target) ?? Color.Gray;
                string txt = args.ElementAtOrDefault(6) ?? "  Press ENTER to explode  ";
                Script sc2 = game.ScriptFromName(args.ElementAtOrDefault(7) ?? "");
                Script sc3 = game.ScriptFromName(args.ElementAtOrDefault(8) ?? "");
                foreach (Sprite sprite in s)
                {
                    VTextBox tb = new VTextBox(0, 4, game.FontTexture, txt, c);
                    tb.CenterX = Game.RESOLUTION_WIDTH / 2;
                    ActivityZone az = new ActivityZone(sprite, 0, 0, w.Value(e), h.Value(e), sc, game, tb);
                    az.EnterScript = sc2;
                    az.ExitScript = sc3;
                    game.ActivityZones.Add(az);
                    sprite.ActivityZone = az;
                }
            });
        }
        private static Command CreateSpriteCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Number x = getNumber(args.ElementAtOrDefault(1) ?? "", game);
                Number y = getNumber(args.ElementAtOrDefault(2) ?? "", game);
                Texture t = game.TextureFromName(args.ElementAtOrDefault(3) ?? "");
                if (t is object)
                {
                    Animation a = t.AnimationFromName(args.ElementAtOrDefault(4) ?? "");
                    if (a is object)
                    {
                        Sprite s = new Sprite(x.Value(e), y.Value(e), t, a);
                        s.Name = args.ElementAtOrDefault(5) ?? "sprite";
                        if (!e.CreatedSprites.ContainsKey(s.Name))
                            e.CreatedSprites.Add(s.Name, s);
                        game.AddSprite(s, s.X, s.Y);
                    }
                }
            });
        }
        private static Command FlipCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                foreach (Sprite sprite in sprites)
                {
                    Crewman c = sprite as Crewman;
                    if (c is object)
                    {
                        c.FlipOrJump();
                    }
                }
            });
        }
        private static Command WaitUntilCommand(Game game, string[] args)
        {
            Command cm = null;
            cm = new Command(game, (e, sender, target) =>
            {
                Number c = getNumber(args.ElementAtOrDefault(1) ?? "", game);
                cm.Wait = c.Value(e) == 0;
                if (cm.Wait)
                e.Waiting = () => c.Value(e) != 0 || c.GetValue is null;
            });
            return cm;
        }
        private static Command CreatePlatformCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Number x = getNumber(args.ElementAtOrDefault(1) ?? "0", game);
                Number y = getNumber(args.ElementAtOrDefault(2) ?? "0", game);
                Number l = getNumber(args.ElementAtOrDefault(6) ?? "4", game);
                Number xv = getNumber(args.ElementAtOrDefault(7) ?? "0", game);
                Number yv = getNumber(args.ElementAtOrDefault(8) ?? "0", game);
                Number c = getNumber(args.ElementAtOrDefault(9) ?? "0", game);
                bool.TryParse(args.ElementAtOrDefault(10) ?? "false", out bool d);
                Texture t = game.TextureFromName(args.ElementAtOrDefault(3) ?? "platforms");
                if (t is object)
                {
                    Animation a = t.AnimationFromName(args.ElementAtOrDefault(4) ?? "platform1");
                    Animation b = t.AnimationFromName(args.ElementAtOrDefault(11) ?? "disappear");
                    if (a is object)
                    {
                        Platform p = new Platform(x.Value(e), y.Value(e), t, a, xv.Value(e), yv.Value(e), c.Value(e), d, b, (int)l.Value(e));
                        string name = args.ElementAtOrDefault(5) ?? "platform";
                        p.Name = name;
                        Color clr = game.CurrentRoom.Color;
                        int r = clr.R + (255 - clr.R) / 2;
                        int g = clr.G + (255 - clr.G) / 2;
                        int bl = clr.B + (255 - clr.B) / 2;
                        p.Color = Color.FromArgb(r, g, bl);
                        if (!e.CreatedSprites.ContainsKey(name))
                            e.CreatedSprites.Add(name, p);
                        game.AddSprite(p, p.X, p.Y);
                    }
                }
            });
        }
        private static Command SetColorCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] s = getSprite(args.ElementAtOrDefault(1), game, e);
                Color? c = game.GetColor(args.ElementAtOrDefault(2), sender, target);
                if (c.HasValue)
                    foreach (Sprite sprite in s)
                        sprite.Color = c.Value;
            });
        }
        private static Command CreateWarpToken(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Number x = getNumber(args.ElementAtOrDefault(1) ?? "0", game);
                Number y = getNumber(args.ElementAtOrDefault(2) ?? "0", game);
                Texture t = game.TextureFromName(args.ElementAtOrDefault(3) ?? "sprites32");
                if (t is object)
                {
                    Animation a = t.AnimationFromName(args.ElementAtOrDefault(4) ?? "WarpToken");
                    if (a is object)
                    {
                        Number rx = getNumber(args.ElementAtOrDefault(5) ?? "0", game);
                        Number ry = getNumber(args.ElementAtOrDefault(6) ?? "0", game);
                        Number ox = getNumber(args.ElementAtOrDefault(7) ?? "0", game);
                        Number oy = getNumber(args.ElementAtOrDefault(8) ?? "0", game);
                        Number f = getNumber(args.ElementAtOrDefault(9) ?? "3", game);
                        string name = args.ElementAtOrDefault(10) ?? "warptoken";
                        int roomX = (int)rx.Value(e);
                        int roomY = (int)ry.Value(e);
                        WarpToken wt = new WarpToken(x.Value(e), y.Value(e), t, a, ox.Value(e) + roomX * Room.ROOM_WIDTH, oy.Value(e) + roomY * Room.ROOM_HEIGHT, roomX, roomY, game, (WarpToken.FlipSettings)(int)f.Value(e));
                        wt.Name = name;
                        if (!e.CreatedSprites.ContainsKey(name))
                        {
                            e.CreatedSprites.Add(name, wt);
                        }
                        game.AddSprite(wt, wt.X, wt.Y);
                    }
                }
            });
        }
        private static Command ScrollBoundsCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Number lx = getNumber(args.ElementAtOrDefault(1) ?? "", game);
                Number ly = getNumber(args.ElementAtOrDefault(2) ?? "", game);
                Number hx = getNumber(args.ElementAtOrDefault(3) ?? "", game);
                Number hy = getNumber(args.ElementAtOrDefault(4) ?? "", game);
                game.MaxScrollX = hx.Value(e);
                game.MaxScrollY = hy.Value(e);
                game.MinScrollX = lx.Value(e);
                game.MinScrollY = ly.Value(e);
            });
        }
        private static Command DoubleJumpCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                foreach (Sprite sprite in sprites)
                {
                    Crewman c = sprite as Crewman;
                    if (c is object)
                    {
                        Number j = getNumber(args.ElementAtOrDefault(2) ?? "1", game);
                        c.MaxJumps = (int)j.Value(e);
                    }
                }
            });
        }
        private static Command KillCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1) ?? "player", game, e);
                foreach (Sprite sprite in sprites)
                {
                    (sprite as Crewman)?.Die();
                }
            });
        }
        private static Command RandomCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Number n = getNumber(args.ElementAtOrDefault(1) ?? "", game);
                if (n.GetValue is object) return;
                Number min = getNumber(args.ElementAtOrDefault(2) ?? "", game);
                Number max = getNumber(args.ElementAtOrDefault(3) ?? "", game);
                Random r = new Random(DateTime.Now.Millisecond + (int)n.Value(e));
                n.SetValue(r.Next((int)min.Value(e), (int)max.Value(e) + 1));
            });
        }
        private static Command DeathScriptCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Script s = game.ScriptFromName(args.ElementAtOrDefault(1) ?? "");
                game.OnPlayerDeath = s;
            });
        }
        private static Command RespawnScriptCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Script s = game.ScriptFromName(args.ElementAtOrDefault(1) ?? "");
                game.OnPlayerRespawn = s;
            });
        }
        private static Command HudTextCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                string name = args.ElementAtOrDefault(1) ?? "";
                string text = args.LastOrDefault() ?? "";
                Number x = getNumber(args.ElementAtOrDefault(2) ?? "", game);
                Number y = getNumber(args.ElementAtOrDefault(3) ?? "", game);
                Color? c = game.GetColor(args.ElementAtOrDefault(4) ?? "White", sender, target);
                if (!bool.TryParse(args.ElementAtOrDefault(6), out bool box))
                    box = false;
                game.HudText(name, text, new PointF(x.Value(e), y.Value(e)), c.GetValueOrDefault(Color.White), box);
            });
        }
        private static Command HudReplaceCommand(Game game, string[] args)
        {
            string name = args.ElementAtOrDefault(1) ?? "";
            string format = args.ElementAtOrDefault(4) ?? "0";
            string replace = args.ElementAtOrDefault(2) ?? "";
            Number with = getNumber(args.ElementAtOrDefault(3), game);
            return new Command(game, (e, sender, target) =>
            {
                float w = with.Value(e);
                string replaceWith = FormatNumber(with.Value(e), format);
                if (replace != null && replace != "")
                {
                    game.HudReplace(name, replace, replaceWith);
                }
            });
        }
        private static Command HudSizeCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                string name = args.ElementAtOrDefault(1) ?? "";
                Number size = getNumber(args.ElementAtOrDefault(2) ?? "1", game);
                game.HudSize(name, size.Value(e));
            });
        }
        private static Command HudRemovCommand(Game  game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                string name = args.ElementAtOrDefault(1) ?? "";
                game.HudRemove(name);
            });
        }
        private static Command SetNumberCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] s = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                foreach (Sprite sprite in s)
                {
                    SortedList<string, SpriteProperty> properties = sprite.Properties;
                    if (properties.ContainsKey(args.ElementAtOrDefault(2) ?? ""))
                    {
                        SpriteProperty sp = properties[args.ElementAtOrDefault(2) ?? ""];
                        Number n = getNumber(args.ElementAtOrDefault(3) ?? "", game);
                        if (sp.Type == SpriteProperty.Types.Float)
                        {
                            sp.SetValue(n.Value(e), game);
                        }
                        else if (sp.Type == SpriteProperty.Types.Int)
                        {
                            sp.SetValue((int)n.Value(e), game);
                        }
                        else if (sp.Type == SpriteProperty.Types.String)
                        {
                            sp.SetValue(n.Value(e).ToString(), game);
                        }
                    }
                }
            });
        }
        private static Command SetBoolCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] s = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                foreach (Sprite sprite in s)
                {
                    SortedList<string, SpriteProperty> properties = sprite.Properties;
                    if (properties.ContainsKey(args.ElementAtOrDefault(2) ?? ""))
                    {
                        SpriteProperty sp = properties[args.ElementAtOrDefault(2) ?? ""];
                        bool.TryParse(args.ElementAtOrDefault(3) ?? "", out bool b);
                        if (sp.Type == SpriteProperty.Types.Bool)
                        {
                            sp.SetValue(b, game);
                        }
                        else if (sp.Type == SpriteProperty.Types.String)
                        {
                            sp.SetValue(b.ToString(), game);
                        }
                    }
                }
            });
        }
        private static Command SetTagCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] s = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                foreach (Sprite sprite in s)
                {
                    if (sprite is object)
                    {
                        Number v = getNumber(args.ElementAtOrDefault(3) ?? "", game);
                        string name = args.ElementAtOrDefault(2) ?? "";
                        if (!sprite.Tags.ContainsKey(name))
                            sprite.Tags.Add(name, v.Value(e));
                        else
                            sprite.Tags[name] = v.Value(e);
                    }
                }
            });
        }
        private static Command RespawnCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1) ?? "player", game, e);
                foreach (Sprite sprite in sprites)
                {
                    Crewman c = sprite as Crewman;
                    if (c is object)
                    {
                        c.Respawn();
                    }
                }
            });
        }
        private static Command SetSpeedCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite[] sprites = getSprite(args.ElementAtOrDefault(1) ?? "", game, e);
                Number xSpeed = getNumber(args.ElementAtOrDefault(2) ?? "", game);
                Number ySpeed = getNumber(args.ElementAtOrDefault(3) ?? "", game);
                float xs = xSpeed.Value(e);
                float ys = ySpeed.Value(e);
                bool relative = string.IsNullOrEmpty(args.ElementAtOrDefault(3));
                foreach (Sprite spr in sprites)
                {
                    IBoundSprite sprite = spr as IBoundSprite;
                    if (spr is null) continue;
                    if (relative)
                    {
                        double sp = xs;
                        double an = Math.Atan2(sprite.YVel, sprite.XVel);
                        sprite.XVel = (float)(sp * Math.Cos(an));
                        sprite.YVel = (float)(sp * Math.Sin(an));
                    }
                    else
                    {
                        sprite.XVel = xs;
                        sprite.YVel = ys;
                    }
                }
            });
        }
        private static Command TrinketCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                int id = (int)getNumber(args.ElementAtOrDefault(1) ?? "0", game).Value(e);
                if (!game.CollectedTrinkets.Contains(id))
                {
                    game.CollectedTrinkets.Add(id);
                    Crewman c = getSprite(args.ElementAtOrDefault(2) ?? "", game, e).FirstOrDefault() as Crewman;
                    if (c is object)
                    {
                        if (game.LoseTrinkets)
                            c.PendingTrinkets.Add(new Trinket(-20, -20, null, null, null, game, id));
                        else
                            c.HeldTrinkets.Add(id);
                    }
                }
            });
        }
    }
}
