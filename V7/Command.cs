using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using System.Globalization;
using OpenTK.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Newtonsoft.Json.Linq;

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

        private static SpriteVariable getSprite(string s, Game game, Script.Executor e)
        {
            if (string.IsNullOrEmpty(s)) return new SpriteVariable("", null);
            Sprite ret = game.SpriteFromName(s);
            if (ret is null)
            {
                if (game.Vars.TryGetValue(s, out Variable v) && v.TryConvert(Variable.VarTypes.Sprite, out v, e))
                    return v as SpriteVariable;
                if ((v = Variable.DoMath(s, e))?.TryConvert(Variable.VarTypes.Sprite, out v, e) ?? false)
                    return v as SpriteVariable;
            }
            return new SpriteVariable(s, ret);
        }

        public static SortedSet<string> Pointers = new SortedSet<string>() { "trinkets", "totaltrinkets", "roomx", "roomy", "camerax", "cameray", "input", "action", "music" };
        public static SortedSet<string> SpritePointers = new SortedSet<string>() { "x", "y", "centerx", "centery", "right", "bottom", "width", "height", "trinkets", "gravity", "direction", "input", "xvelocity", "yvelocity", "texture", "animation" };
        static SortedSet<char> operators = new SortedSet<char>() { '+', '-', '*', '/', '=', '!', '&', '|', '>', '<' };

        public static DecimalVariable GetNumber(string s, Game game, Script.Executor e, List<DecimalVariable> newNumbers = null)
        {
            {
                if (string.IsNullOrEmpty(s)) return new DecimalVariable("", 0);
                Variable var = Variable.DoMath(s, e);
                if (var?.TryConvert(Variable.VarTypes.Decimal, out Variable ret, e) ?? false)
                    return ret as DecimalVariable;
            }
            return new DecimalVariable("", 0f);
        }
        private static TextureVariable getTexture(string s, Game game, Script.Executor e)
        {
            if (string.IsNullOrEmpty(s)) return new TextureVariable("", null);
            Texture ret = game.TextureFromName(s);
            if (ret is null)
            {
                if (game.Vars.TryGetValue(s, out Variable v) && v.TryConvert(Variable.VarTypes.Texture, out v, e))
                    return v as TextureVariable;
                if (Variable.DoMath(s, e)?.TryConvert(Variable.VarTypes.Texture, out v, e) ?? false)
                    return v as TextureVariable;
            }
            return new TextureVariable(s, ret);
        }
        private static AnimationVariable getAnimation(string s, Game game, Texture t, Script.Executor e)
        {
            if (string.IsNullOrEmpty(s)) return new AnimationVariable("", null);
            Animation ret = t.AnimationFromName(s);
            if (ret is null)
            {
                if (game.Vars.TryGetValue(s, out Variable v) && v.TryConvert(Variable.VarTypes.Animation, out v, e))
                    return v as AnimationVariable;
                if (Variable.DoMath(s, e)?.TryConvert(Variable.VarTypes.Animation, out v, e) ?? false)
                    return v as AnimationVariable;
            }
            return new AnimationVariable(s, ret);
        }
        private static SoundVariable getSound(string s, Game game, Script.Executor e)
        {
            if (string.IsNullOrEmpty(s)) return null;
            SoundEffect ret = game.GetSound(s);
            if (ret is null)
            {
                if (game.Vars.TryGetValue(s, out Variable v) && v.TryConvert(Variable.VarTypes.Sound, out v, e))
                    return v as SoundVariable;
                if (Variable.DoMath(s, e).TryConvert(Variable.VarTypes.Sound, out v, e))
                    return v as SoundVariable;
            }
            return new SoundVariable(s, ret);
        }
        private static Music getMusic(string s, Game game, Script.Executor e)
        {
            if (string.IsNullOrEmpty(s)) return Music.Empty;
            Music ret = game.GetMusic(s);
            if (ret is null)
            {
                int n = (int)GetNumber(s, game, e).Value;
                if (n > -1 && n < game.Textures.Count)
                    ret = game.Songs.Values[n];
            }
            return ret;
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
            { "arg", new Syntax(null, new ArgTypes[] { ArgTypes.None, ArgTypes.ArgType, ArgTypes.None }) },
            { "say", new Syntax(SayCommand, new ArgTypes[] { ArgTypes.Int, ArgTypes.Color }, 0) },
            { "text", new Syntax(TextCommand, new ArgTypes[] { ArgTypes.Color, ArgTypes.Number, ArgTypes.Number, ArgTypes.Int }, 3) },
            { "changefont", new Syntax(ChangeFontCommand, new ArgTypes[] { ArgTypes.Texture }) },
            { "delay", new Syntax(WaitCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "playercontrol", new Syntax(PlayerControlCommand, new ArgTypes[] { ArgTypes.Bool }) },
            { "mood", new Syntax(MoodCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Mood }) },
            { "checkpoint", new Syntax(CheckpointCommand, new ArgTypes[] { }) },
            { "position", new Syntax(PositionCommand, new ArgTypes[] { ArgTypes.Position1, ArgTypes.Position2 }) },
            { "centertext", new Syntax(CenterTextCommand, new ArgTypes[] { }) },
            { "speak", new Syntax(SpeakCommand, new ArgTypes[] { }) },
            { "speak_active", new Syntax(SpeakActiveCommand, new ArgTypes[] { }) },
            { "showtext", new Syntax(ShowTextCommand, new ArgTypes[] { }) },
            { "endtext", new Syntax(EndTextCommand, new ArgTypes[] { }) },
            { "squeak", new Syntax(SqueakCommand, new ArgTypes[] { ArgTypes.Squeak }) },
            { "playef", new Syntax(PlaySoundCommand, new ArgTypes[] { ArgTypes.Sound }) },
            { "playsound", new Syntax(PlaySoundCommand, new ArgTypes[] { ArgTypes.Sound }) },
            { "addsprite", new Syntax(AddSpriteCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number, ArgTypes.Number }) },
            { "changeai", new Syntax(ChangeAICommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.AI, ArgTypes.Sprite }) },
            { "shake", new Syntax(ShakeCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number }) },
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
            { "iftouching", new Syntax(IfTouchingCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Sprite, ArgTypes.Script, ArgTypes.If }) },
            { "createenemy", new Syntax(CreateEnemyCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Texture, ArgTypes.Animation, ArgTypes.Number, ArgTypes.Number, ArgTypes.SpriteName, ArgTypes.Bool }) },
            { "setposition", new Syntax(SetPositionCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.PosX, ArgTypes.Number, ArgTypes.PosY, ArgTypes.Number }) },
            { "movesprite", new Syntax(MoveSpriteCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number, ArgTypes.Number }) },
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
            { "hudtext", new Syntax(HudTextCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.Number, ArgTypes.Number, ArgTypes.Color, ArgTypes.Int }, 4) },
            { "hudsprite", new Syntax(HudSpriteCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.Number, ArgTypes.Number, ArgTypes.Color, ArgTypes.Texture, ArgTypes.Animation }) },
            { "hudreplace", new Syntax(HudReplaceCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.None, ArgTypes.Number, ArgTypes.NumberFormat }) },
            { "hudsize", new Syntax(HudSizeCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.Number }) },
            { "hudremove", new Syntax(HudRemovCommand, new ArgTypes[] { ArgTypes.None }) },
            { "setnumber", new Syntax(SetNumberCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Property, ArgTypes.Number }) },
            { "setbool", new Syntax(SetBoolCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Property, ArgTypes.Bool }) },
            { "settag", new Syntax(SetTagCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.None, ArgTypes.Number }) },
            { "respawn", new Syntax(RespawnCommand, new ArgTypes[] { ArgTypes.Sprite }) },
            { "setspeed", new Syntax(SetSpeedCommand, new ArgTypes[] { ArgTypes.Sprite, ArgTypes.Number, ArgTypes.Number }) },
            { "trinket", new Syntax(TrinketCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Sprite }) },
            { "showmap", new Syntax(ShowMapCommand, new ArgTypes[] { ArgTypes.Bool, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "hidemap", new Syntax(HideMapCommand, new ArgTypes[] { }) },
            { "getroomtag", new Syntax(GetRoomTagCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.None }) },
            { "mapimage", new Syntax(MapImageCommand, new ArgTypes[] { ArgTypes.Texture }) },
            { "startloading", new Syntax(StartLoading, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number }) },
            { "mapanimation", new Syntax(MapAnimation, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Bool, ArgTypes.Bool }) },
            { "capturesprites", new Syntax(CaptureSpritesCommand, new ArgTypes[] { ArgTypes.SpriteName }) },
            { "addmenuitem", new Syntax(AddMenuItemCommand, new ArgTypes[] { ArgTypes.None, ArgTypes.Script, ArgTypes.Number, ArgTypes.Number }) },
            { "clearmenuitems", new Syntax(ClearMenuItemsCommand, new ArgTypes[] { }) },
            { "pausescript", new Syntax(PauseScriptCommand, new ArgTypes[] { ArgTypes.Script }) },
            { "unpuase", new Syntax(UnpauseCommand, new ArgTypes[] { }) },
            { "groundtool", new Syntax(TileToolCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "backgroundtool", new Syntax(TileToolCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "spikestool", new Syntax(TileToolCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "saveroom", new Syntax(SaveRoomCommand, new ArgTypes[] { }) },
            { "addlight", new Syntax(AddLightCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "setlight", new Syntax(SetLightCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "lightlevel", new Syntax(LightLevelCommand, new ArgTypes[] { ArgTypes.Number }) },
            { "vector", new Syntax(VectorCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) },
            { "scalar", new Syntax(ScalarCommand, new ArgTypes[] { ArgTypes.Number, ArgTypes.Number, ArgTypes.Number, ArgTypes.Number }) }
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
            public int linesIndex;
            public Syntax(cmd cmd, ArgTypes[] arg, int lines = -1)
            {
                command = cmd;
                args = arg;
                linesIndex = lines;
            }
        }

        public enum ArgTypes { None, ArgType, Command, Identifier, Property, Int, Sprite, Texture, Animation, Sound, Color, Bool, Number, Mood, Position1, Position2, AI, Squeak, Script, If, SpriteName, PosX, PosY, Music, NumberFormat, Marker, Type }
        public static readonly SortedDictionary<string, ArgTypes> CustomArgTypes = new SortedDictionary<string, ArgTypes>()
        {
            { "number", ArgTypes.Number },
            { "texture", ArgTypes.Texture },
            { "animation", ArgTypes.Animation },
            { "sound", ArgTypes.Sound },
            { "music", ArgTypes.Music },
            { "sprite", ArgTypes.Sprite }
        };
        public static readonly SortedSet<ArgTypes> NumberSub = new SortedSet<ArgTypes>() { ArgTypes.Animation, ArgTypes.Bool, ArgTypes.Color, ArgTypes.Mood, ArgTypes.Music, ArgTypes.Sound, ArgTypes.Texture };

        public static string[] CommandNames
        {
            get
            {
                List<string> ret = cmdTypes.Keys.ToList();
                ret.Sort();
                return ret.ToArray();
            }
        }

        private static SortedList<string, int> argNames;
        public static Command[] ParseScript2(Game game, string script, Script scr)
        {
            argNames = new SortedList<string, int>();
            scr.ClearArgs();
            string[] lines = script.Replace(Environment.NewLine, "\n").Split(new char[] { '\n' });
            List<Command> commands = new List<Command>();
            int i = 0;
            char[] dividers = new char[] { '(', '=' };
            while (i < lines.Length)
            {
                string line = lines[i].TrimStart();
                if (line.StartsWith("#"))
                {
                    i++;
                    continue;
                }
                else if (line.StartsWith(">"))
                {
                    line = line.Substring(1);
                    scr.AddMarker(line, commands.Count);
                    i++;
                    continue;
                }
                else if (line == "{")
                {
                    scr.OpenBraces(commands.Count);
                    i++;
                    continue;
                }
                else if (line == "}")
                {
                    scr.CloseBraces(commands.Count);
                    i++;
                    continue;
                }
                int dividerIndex = line.IndexOfAny(dividers);
                if (dividerIndex == 0)
                {
                    continue;
                }
                else if (dividerIndex == -1)
                {
                    line += "()";
                    dividerIndex = line.Length - 2;
                }
                char divider = line[dividerIndex];
                Command command;
                if (divider == '=')
                {
                    //if (operators.Contains(line[--dividerIndex]))
                    //{
                    //    op = line[dividerIndex - 1] + "=";
                    //}
                    command = new Command(game, (e, sender, target) =>
                    {
                        string leftSide = line[..dividerIndex];
                        leftSide = leftSide.TrimEnd();
                        Variable var1 = GetFullVariable(leftSide, game, e);
                        string rightSide = line.Substring(dividerIndex + 1);
                        rightSide = rightSide.Trim();
                        Variable var2 = Variable.DoMath(rightSide, e);
                        if (var1.VarType == var2.VarType || var2.TryConvert(var1.VarType, out var2, e))
                        {
                            var1.Set(var2);
                        }
                    });
                    commands.Add(command);
                }
                else if (divider == '(')
                {
                    string leftSide = line[..dividerIndex];
                    leftSide = leftSide.TrimEnd();
                    int rsIndex = line.LastIndexOf(')');
                    if (rsIndex == -1)
                    {
                        rsIndex = line.Length;
                    }
                    if (rsIndex - dividerIndex - 1 < 0) continue;
                    string rightSide = line.Substring(dividerIndex + 1, rsIndex - dividerIndex - 1);
                    if (cmdTypes.TryGetValue(leftSide, out Syntax cmdSyntax))
                    {
                        List<string> argsList = new List<string>();
                        argsList.Add(leftSide);
                        argsList.AddRange(rightSide.Split(','));
                        if (cmdSyntax.linesIndex > -1)
                        {
                            if (!int.TryParse(argsList.ElementAtOrDefault(cmdSyntax.linesIndex + 1), out int linesCount) || linesCount == 0) continue;
                            string text = lines[i + 1];
                            for (int j = 1; j < linesCount && j + i + 1 < lines.Length; j++)
                            {
                                text += "\n" + lines[j + i + 1];
                            }
                            argsList.Add(text);
                        }
                        commands.Add(cmdSyntax.command(game, argsList.ToArray()));
                    }
                }
                i++;
            }
            return commands.ToArray();
        }

        private static Variable GetFullVariable(string s, Game game, Script.Executor e)
        {
            Variable var;
            int ind = s.IndexOf(':');
            string arg;
            bool done;
            if (done = ind == -1) ind = s.Length;
            arg = s.Substring(0, ind);
            var = Variable.GetVariable(arg, e);
            while (!done)
            {
                s = s.Substring(ind + 1);
                if (done = (ind = s.IndexOf(':')) == -1)
                    ind = s.Length;
                arg = s.Substring(0, ind);
                var = var.GetProperty(arg, e);
            }
            return var;
        }

        public static Command[] ParseScript(Game game, string script, Script scr)
        {
            return ParseScript2(game, script, scr);
            argNames = new SortedList<string, int>();
            scr.ClearArgs();
            string[] lines = script.Replace(Environment.NewLine, "\n").Split(new char[] { '\n' });
            List<Command> commands = new List<Command>();
            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i++];
                line.TrimStart(' ');
                List<string> argsList = line.Split(',').ToList();
                if (argsList[0].Contains('('))
                {
                    string cm = argsList[0].Substring(0, argsList[0].IndexOf('('));
                    argsList.Insert(0, cm);
                    argsList[1] = argsList[1].Substring(cm.Length + 1);
                    if (argsList.Last().EndsWith(")"))
                    {
                        argsList[argsList.Count - 1] = argsList[argsList.Count - 1].Substring(0, argsList[argsList.Count - 1].Length - 1);
                    }
                }
                if (line.StartsWith("#"))
                {
                    continue;
                }
                else if (line.StartsWith(">"))
                {
                    line = line.Substring(1);
                    scr.AddMarker(line, commands.Count);
                    continue;
                }
                else if (line == "{")
                {
                    scr.OpenBraces(commands.Count);
                    continue;
                }
                else if (line == "}")
                {
                    scr.CloseBraces(commands.Count);
                    continue;
                }
                else if (line.StartsWith("arg"))
                {
                    string[] argArgs = line.Split(',', '(', ')');
                    if (argArgs.Length > 3)
                    {
                        string name = argArgs[1];
                        if (CustomArgTypes.TryGetValue(argArgs[2], out ArgTypes type))
                        {
                            float.TryParse(argArgs[3], out float f);
                            argNames.Add(name, scr.ArgCount);
                            scr.AddArg(name, type, f);
                        }
                    }
                    continue;
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
                Script customCommand;
                if (cmdTypes.ContainsKey(args[0]))
                {
                    commands.Add(cmdTypes[args[0]].command(game, args));
                }
                else if ((customCommand = game.ScriptFromName(args[0])) is object)
                {
                    commands.Add(CustomCommand(game, customCommand, args));
                }
            }
            return commands.ToArray();
        }

        private static Command SayCommand(Game game, string[] args)
        {

            return new Command(game, (e, sender, target) =>
            {
                Color sayTextBoxColor = Color.Gray;
                Crewman sayCrewman = getSprite(args.ElementAtOrDefault(2) ?? "", game, e).Value as Crewman;
                SoundEffect squeak = null;
                if (sayCrewman is object)
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
                VTextBox sayTextBox = new VTextBox(0, 0, game.FontTexture, args.Last(), sayTextBoxColor);
                sayTextBox.ParseStyles();
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
                        txTextBoxColor = Color.FromArgb(255, 164, 164, 255);
                        break;
                    case "vermilion":
                        txTextBoxColor = Color.FromArgb(255, 255, 60, 60);
                        break;
                    case "vitellary":
                        txTextBoxColor = Color.FromArgb(255, 255, 255, 134);
                        break;
                    case "verdigris":
                        txTextBoxColor = Color.FromArgb(255, 144, 255, 144);
                        break;
                    case "violet":
                        txTextBoxColor = Color.FromArgb(255, 255, 134, 255);
                        break;
                    case "victoria":
                        txTextBoxColor = Color.FromArgb(255, 95, 95, 255);
                        break;
                    case "gray":
                    case "terminal":
                        txTextBoxColor = Color.FromArgb(255, 174, 174, 174);
                        break;
                }
            }
            else
            {
                Color? clr = game.GetColor(c);
                if (clr.HasValue)
                    txTextBoxColor = clr.Value;
            }
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable txX = GetNumber(args.ElementAtOrDefault(2 + txArgOffset), game, e);
                DecimalVariable txY = GetNumber(args.ElementAtOrDefault(3 + txArgOffset), game, e);
                VTextBox tb = new VTextBox(txX.Value, txY.Value, game.FontTexture, args.Last(), txTextBoxColor) { Layer = 50 };
                tb.ParseStyles();
                e.TextBoxes.Add(tb);
                game.TextBoxes.Add(tb);
                game.hudSprites.Add(tb);
            });
        }
        private static Command ChangeFontCommand(Game game, string[] args)
        {
            string fontTexture = args.ElementAtOrDefault(1);
            FontTexture newFont = game.TextureFromName(fontTexture) as FontTexture;
            Action<Script.Executor, Sprite, Sprite> success = (e, sender, target) => { };
            if (newFont != null && (newFont.Width / newFont.TileSizeX) % 16 == 0)
            {
                success = (e, sender, target) => {
                    game.FontTexture = newFont;
                };
            }
            return new Command(game, success, false);
        }
        private static Command WaitCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable frames = GetNumber(args.LastOrDefault(), game, e);
                e.WaitingFrames = (int)frames.Value;
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
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "player", game, e).Value;
                if (sprite is Crewman)
                    (sprite as Crewman).Sad = sad;
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
            return new Command(game, (e, sender, target) =>
            {
                Variable with = Variable.DoMath(args.ElementAtOrDefault(2), e);
                string replaceWith;
                if (with is IntegerVariable && with.TryConvert(Variable.VarTypes.Decimal, out Variable dv, e))
                {
                    float w = (dv as DecimalVariable).Value;
                    replaceWith = FormatNumber(w, args.ElementAtOrDefault(3) ?? "false");
                }
                else
                {
                    replaceWith = with.ToString();
                }
                if (replace != null && replace != "")
                {
                    VTextBox tb = e.TextBoxes.Last();
                    if (tb != null)
                    {
                        tb.UnparseStyles();
                        string t = tb.Text.Replace(replace, replaceWith);
                        tb.Text = t;
                        tb.ParseStyles();
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
                    tb.UnparseStyles();
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
                    tb.ParseStyles();
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
            return new Command(game, (e, sender, target) => 
            {
                getSound(args.LastOrDefault() ?? "", game, e).Value?.Play();
            });
        }
        private static Command AddSpriteCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite s = getSprite(args.ElementAtOrDefault(1), game, e).Value;
                if (s != null)
                {
                    DecimalVariable x = GetNumber(args.ElementAtOrDefault(2), game, e);
                    DecimalVariable y = GetNumber(args.ElementAtOrDefault(3), game, e);
                    game.AddSprite(s, x.Value, y.Value);
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
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                Crewman crewman2 = getSprite(args.ElementAtOrDefault(3) ?? "", game, e).Value as Crewman;
                if (sprite is Crewman)
                {
                    if (crewman2 is null) crewman2 = game.ActivePlayer;
                    (sprite as Crewman).AIState = aiState;
                    if (aiState != Crewman.AIStates.Stand)
                        (sprite as Crewman).Target = crewman2;
                }
            }, false);
        }
        private static Command ShakeCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable frames = GetNumber(args.ElementAtOrDefault(1), game, e);
                DecimalVariable intensity = GetNumber(args.ElementAtOrDefault(2) ?? "2", game, e);
                game.Shake((int)frames.Value, (int)intensity.Value);
            });
        }
        private static Command FlashCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable frames = GetNumber(args.ElementAtOrDefault(1), game, e);
                DecimalVariable r = GetNumber(args.ElementAtOrDefault(2), game, e);
                DecimalVariable g = GetNumber(args.ElementAtOrDefault(3), game, e);
                DecimalVariable b = GetNumber(args.ElementAtOrDefault(4), game, e);
                if (args.Length < 5) r = g = b = 255;
                game.Flash((int)frames.Value, (int)r.Value, (int)g.Value, (int)b.Value);
            });
        }
        private static Command MusicFadeOutCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable speed = GetNumber(args.LastOrDefault() ?? "0", game, e);
                if (speed.Value == 0) speed = 1;
                game.CurrentSong.FadeOut(speed.Value);
            }, false);
        }
        private static Command MusicFadeInCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable speed = GetNumber(args.LastOrDefault() ?? "0", game, e);
                if (speed.Value == 0) speed = 1;
                game.CurrentSong.FadeIn(speed.Value);
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
                Sprite sprite = getSprite(args.ElementAtOrDefault(1), game, e).Value;
                Crewman c = sprite as Crewman;
                if (c is object)
                {
                    if (int.TryParse(args.ElementAtOrDefault(2) ?? "0", out int p))
                    {
                        c.InputDirection = Math.Sign(p);
                    }
                }
            });
        }
        private static Command ItemCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                Crewman c = sprite as Crewman;
                if (c is object)
                {
                    Script s = game.ScriptFromName(args.Last());
                    if (s is object)
                    {
                        c.Script = s;
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
            bool isElseif = args[0].ToLower() == "elseif";
            string script = args.ElementAtOrDefault(2) ?? "";
            string option = (args.ElementAtOrDefault(3) ?? "").ToLower();
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable n = GetNumber(args.ElementAtOrDefault(1), game, e);
                if (isElseif && e.IfSatisfied)
                {
                    if (args.Length == 2)
                        e.SkipAhead();
                    e.Continue();
                    return;
                }
                if (n.Value != 0)
                {
                    e.IfSatisfied = true;
                    Script.Executor scr = game.ExecuteScript(game.ScriptFromName(script), sender, target, new DecimalVariable[] { }, false, e.Locals);
                    switch (option)
                    {
                        case "wait":
                            if (scr is object)
                                scr.Finished += (sc) => { e.Continue(); };
                            break;
                        case "stop":
                            e.Stop();
                            break;
                        case "goto":
                            e.GoToMarker(args.ElementAtOrDefault(4));
                            break;
                        default:
                            e.Continue();
                            break;
                    }
                }
                else
                {
                    e.IfSatisfied = false;
                    if (args.Length == 2)
                        e.SkipAhead();
                    else
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
                    if ((args.ElementAtOrDefault(1) ?? "") == "")
                        e.SkipAhead();
                    e.Continue();
                    return;
                }
                Script.Executor scr = null;
                scr = game.ExecuteScript(game.ScriptFromName(script), sender, target, new DecimalVariable[] { });
                if (scr is object)
                {
                    for (int i = 0; i < e.Locals.Count; i++)
                    {
                        scr.Locals.Add(e.Locals.Keys[i], e.Locals.Values[i]);
                    }
                    switch (option)
                    {
                        case "wait":
                            scr.Finished += (sc) => { e.Continue(); };
                            break;
                        case "stop":
                            e.Stop();
                            break;
                        case "goto":
                            e.GoToMarker(args.ElementAtOrDefault(4));
                            break;
                        default:
                            e.Continue();
                            break;
                    }
                }
                else
                    switch (option)
                    {
                        case "stop":
                            e.Stop();
                            break;
                        case "goto":
                            e.GoToMarker(args.ElementAtOrDefault(4));
                            break;
                        default:
                            e.Continue();
                            break;
                    }
                e.IfSatisfied = true;
            }, true);
        }
        private static Command IfTouchingCommand(Game game, string[] args)
        {
            string script = args.ElementAtOrDefault(3) ?? "";
            string option = (args.ElementAtOrDefault(4) ?? "").ToLower();
            return new Command(game, (e, sender, target) =>
            {
                Sprite s1 = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                Sprite s2 = getSprite(args.ElementAtOrDefault(2) ?? "", game, e).Value;
                bool yes = s1.IsOverlapping(s2) is object;
                if (yes)
                {
                    e.IfSatisfied = true;
                    Script.Executor scr = game.ExecuteScript(game.ScriptFromName(script), sender, target, new DecimalVariable[] { }, false, e.Locals);
                    switch (option)
                    {
                        case "wait":
                            if (scr is object)
                                scr.Finished += (sc) => { e.Continue(); };
                            break;
                        case "stop":
                            e.Stop();
                            break;
                        case "goto":
                            e.GoToMarker(args.ElementAtOrDefault(5));
                            break;
                        default:
                            e.Continue();
                            break;
                    }
                }
                else
                {
                    e.IfSatisfied = false;
                    if (args.Length == 3)
                        e.SkipAhead();
                    else
                        e.Continue();
                }
            });
        }
        private static Command CreateEnemyCommand(Game game, string[] args)
        {
            string texture = args.ElementAtOrDefault(3) ?? "";
            string animation = args.ElementAtOrDefault(4) ?? "";
            string name = args.ElementAtOrDefault(7) ?? "";
            if (!bool.TryParse(args.ElementAtOrDefault(8) ?? "", out bool s))
                s = true;
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable xSp = GetNumber(args.ElementAtOrDefault(5) ?? "", game, e);
                DecimalVariable ySp = GetNumber(args.ElementAtOrDefault(6) ?? "", game, e);
                Texture t = getTexture(texture, game, e).Value;
                Animation a = getAnimation(animation, game, t, e).Value;
                if (a is object)
                {
                    Enemy enemy = new Enemy(x.Value, y.Value, t, a, xSp.Value, ySp.Value, game.CurrentRoom.Color) { Name = name };
                    enemy.SyncAnimation(game.FrameCount);
                    if (!s)
                        enemy.Solid = Sprite.SolidState.NonSolid;
                    e.Locals.Remove(name);
                    e.Locals.Add(name, new SpriteVariable(name, enemy));
                    game.AddSprite(enemy, enemy.X, enemy.Y);
                }
            });
        }
        private static Command SetPositionCommand(Game game, string[] args)
        {
            string sprite = args.ElementAtOrDefault(1) ?? "";
            string xAlign = args.ElementAtOrDefault(2)?.ToLower() ?? "x";
            string yAlign = args.ElementAtOrDefault(4)?.ToLower() ?? "y";
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(5) ?? "", game, e);
                Sprite s = getSprite(sprite, game, e).Value;
                if (s is object)
                    switch (xAlign)
                    {
                        case "centerx":
                            s.CenterX = x.Value + game.CurrentRoom.GetX;
                            break;
                        case "right":
                            s.Right = x.Value + game.CurrentRoom.GetX;
                            break;
                        default:
                            s.X = x.Value + game.CurrentRoom.GetX;
                            break;
                    }
                switch (yAlign)
                {
                    case "centery":
                        s.CenterY = y.Value + game.CurrentRoom.GetY;
                        break;
                    case "bottom":
                        s.Bottom = y.Value + game.CurrentRoom.GetY;
                        break;
                    default:
                        s.Y = y.Value + game.CurrentRoom.GetY;
                        break;
                }
            });
        }
        private static Command MoveSpriteCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                sprite.X += x.Value;
                sprite.Y += y.Value;
            });
        }
        private static Command SetBoundsCommand(Game game, string[] args)
        {
            string spr = args.ElementAtOrDefault(1) ?? "";
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                DecimalVariable w = GetNumber(args.ElementAtOrDefault(4) ?? "", game, e);
                DecimalVariable h = GetNumber(args.ElementAtOrDefault(5) ?? "", game, e);
                Sprite sprite = getSprite(spr, game, e).Value;
                IBoundSprite s = sprite as IBoundSprite;
                if (s is object)
                {
                    s.Bounds = new Rectangle((int)x.Value + (int)game.CurrentRoom.GetX - (int)s.InitialX, (int)y.Value + (int)game.CurrentRoom.GetY - (int)s.InitialY, (int)w.Value, (int)h.Value);
                }
            });
        }
        private static Command FlipYCommand(Game game, string[] args)
        {
            string sprite = args.ElementAtOrDefault(1)?.ToLower() ?? "";
            return new Command(game, (e, sender, target) =>
            {
                Sprite s = getSprite(sprite, game, e).Value;
                s.FlipY = !s.FlipY;
            });
        }
        private static Command DeleteSpriteCommand(Game game, string[] args)
        {
            string sprite = args.ElementAtOrDefault(1) ?? "";
            return new Command(game, (e, sender, target) =>
            {
                Sprite s = getSprite(sprite, game, e).Value;
                game.RemoveSprite(s);
            });
        }
        private static Command CreateTimerCommand(Game game, string[] args)
        {
            string script = args.ElementAtOrDefault(1) ?? "";
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable interval = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                Script s = game.ScriptFromName(script);
                if (s is object)
                {
                    Timer timer = new Timer(s, (int)interval.Value, target, game);
                    game.AddSprite(timer, 0, 0);
                }
            });
        }
        private static Command ChangeMusicCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Music music = getMusic(args.ElementAtOrDefault(1) ?? "", game, e);
                bool.TryParse(args.ElementAtOrDefault(3) ?? "true", out bool fadeout);
                bool.TryParse(args.ElementAtOrDefault(4) ?? "true", out bool fadein);
                float.TryParse(args.ElementAtOrDefault(2) ?? "80", out float volume);
                if (music != game.CurrentSong)
                {
                    if (fadeout)
                    {
                        game.CurrentSong.FadeOut();
                        game.MusicFaded = () =>
                        {
                            game.CurrentSong.Stop();
                            game.CurrentSong = music;
                            music.Rewind();
                            music.Silence();
                            if (fadein)
                                music.FadeIn();
                            else
                                music.Play();
                        };
                    }
                    else
                    {
                        game.CurrentSong.Stop();
                        game.CurrentSong = music;
                        music.Rewind();
                        if (fadein)
                            music.FadeIn();
                        else
                            music.Play();
                    }
                }
                if (!music.IsNull)
                {
                    music.Volume = volume;
                }
            });
        }
        private static Command ExitConditionCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable ec = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                e.ExitCondition = () => ec.Value != 0;
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
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable n = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable s = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                n.Value =  s.Value;
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
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable condition = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                if (condition.Value != 0)
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
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable rx = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable ry = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(4) ?? "", game, e);
                int roomID = (int)(rx.Value + ry.Value * 100);
                if (game.RoomDatas.ContainsKey(roomID))
                    game.CurrentRoom.AddRoom(game.RoomDatas[roomID], game, (int)x.Value, (int)y.Value);
            });
        }
        private static Command AutoScrollCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable mx = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                DecimalVariable my = GetNumber(args.ElementAtOrDefault(4) ?? "", game, e);
                game.AutoScrollX = x.Value;
                game.AutoScrollY = y.Value;
                game.StopScrollX = mx.Value;
                game.StopScrollY = my.Value;
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
                Crewman c = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value as Crewman;
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
                Sprite s = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                if (s is object && s.Texture is object)
                {
                    Animation anim = getAnimation(args.ElementAtOrDefault(2) ?? "", game, s.Texture, e).Value;
                    if (anim is object)
                    {
                        if (!string.IsNullOrEmpty(property))
                        {
                            s.ResetAnimation();
                            s.SetProperty(property, anim.Name, game);
                        }
                        else
                        {
                            s.ResetAnimation();
                            s.Animation = anim;
                        }
                    }
                }
            });
        }
        private static Command CreateCrewmanCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                CrewmanTexture t = getTexture(args.ElementAtOrDefault(3) ?? "", game, e).Value as CrewmanTexture;
                string name = args.ElementAtOrDefault(4) ?? "";
                if (name == "") name = t?.Name ?? "crewman";
                Crewman c = new Crewman(x.Value, y.Value, t, game, name);
                if (!e.Locals.ContainsKey(c.Name))
                    e.Locals.Add(c.Name, new SpriteVariable(c.Name, c));
                game.AddSprite(c, c.X, c.Y);
            });
        }
        private static Command GoToRoomCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                game.LoadRoom((int)x.Value, (int)y.Value);
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
                Sprite sprite = getSprite(spr, game, e).Value;
                if (sprite is IScriptExecutor)
                {
                    (sprite as IScriptExecutor).Activated = false;
                }
                if (sprite?.ActivityZone is object)
                {
                    sprite.ActivityZone.Activated = false;
                }
            });
        }
        private static Command SetGravityCommand(Game game, string[] args)
        {
            string spr = (args.ElementAtOrDefault(1) ?? "").ToLower();
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable g = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                Sprite sprite = getSprite(spr, game, e).Value;
                if (sprite is object)
                {
                    sprite.Gravity = g.Value * 0.6875f;
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
                        foreach (SpriteVariable sprites in e.Locals.Values)
                        {
                            //foreach (var sprite in sprites)
                            {
                                game.RemoveSprite(sprites.Value);
                            }
                        }
                        e.Locals.Clear();
                    }
                }
            });
        }
        private static Command CreateActivityZoneCommand(Game game, string[] args)
        {
            string sn = args.ElementAtOrDefault(1) ?? "";
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(sn, game, e).Value;
                DecimalVariable w = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable h = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                Script sc = game.ScriptFromName(args.ElementAtOrDefault(4) ?? "");
                Color c = game.GetColor(args.ElementAtOrDefault(5) ?? "", sender, target) ?? Color.Gray;
                string txt = args.ElementAtOrDefault(6) ?? "  Press ENTER to explode  ";
                Script sc2 = game.ScriptFromName(args.ElementAtOrDefault(7) ?? "");
                Script sc3 = game.ScriptFromName(args.ElementAtOrDefault(8) ?? "");
                VTextBox tb = new VTextBox(0, 4, game.FontTexture, txt, c) { CenterX = Game.RESOLUTION_WIDTH / 2 };
                ActivityZone az = new ActivityZone(sprite, 0, 0, w.Value, h.Value, sc, game, tb)
                {
                    EnterScript = sc2,
                    ExitScript = sc3
                };
                game.ActivityZones.Add(az);
                sprite.ActivityZone = az;
            });
        }
        private static Command CreateSpriteCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                Texture t = getTexture(args.ElementAtOrDefault(3) ?? "", game, e).Value;
                if (t is object)
                {
                    Animation a = getAnimation(args.ElementAtOrDefault(4) ?? "", game, t, e).Value;
                    if (a is object)
                    {
                        Sprite s = new Sprite(x.Value, y.Value, t, a)
                        {
                            Name = args.ElementAtOrDefault(5) ?? "sprite"
                        };
                        if (!e.Locals.ContainsKey(s.Name))
                            e.Locals.Add(s.Name, new SpriteVariable(s.Name, s));
                        game.AddSprite(s, s.X, s.Y);
                    }
                }
            });
        }
        private static Command FlipCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                Crewman c = sprite as Crewman;
                if (c is object)
                {
                    c.FlipOrJump();
                }
            });
        }
        private static Command WaitUntilCommand(Game game, string[] args)
        {
            Command cm = null;
            cm = new Command(game, (e, sender, target) =>
            {
                DecimalVariable c = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                cm.Wait = c.Value == 0;
                if (cm.Wait)
                    e.Waiting = () => c.Value != 0 || c.GetValue is null;
            });
            return cm;
        }
        private static Command CreatePlatformCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "0", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "0", game, e);
                DecimalVariable l = GetNumber(args.ElementAtOrDefault(6) ?? "4", game, e);
                DecimalVariable xv = GetNumber(args.ElementAtOrDefault(7) ?? "0", game, e);
                DecimalVariable yv = GetNumber(args.ElementAtOrDefault(8) ?? "0", game, e);
                DecimalVariable c = GetNumber(args.ElementAtOrDefault(9) ?? "0", game, e);
                bool.TryParse(args.ElementAtOrDefault(10) ?? "false", out bool d);
                Texture t = getTexture(args.ElementAtOrDefault(3) ?? "platforms", game, e).Value;
                if (t is object)
                {
                    Animation a = getAnimation(args.ElementAtOrDefault(4) ?? "platform1", game, t, e).Value;
                    Animation b = getAnimation(args.ElementAtOrDefault(11) ?? "disappear", game, t, e).Value;
                    if (a is object)
                    {
                        Platform p = new Platform(x.Value, y.Value, t, a, xv.Value, yv.Value, c.Value, d, b, (int)l.Value);
                        string name = args.ElementAtOrDefault(5) ?? "platform";
                        p.Name = name;
                        Color clr = game.CurrentRoom.Color;
                        int r = clr.R + (255 - clr.R) / 2;
                        int g = clr.G + (255 - clr.G) / 2;
                        int bl = clr.B + (255 - clr.B) / 2;
                        p.Color = Color.FromArgb(255, r, g, bl);
                        if (!e.Locals.ContainsKey(name))
                            e.Locals.Add(name, new SpriteVariable(name, p));
                        game.AddSprite(p, p.X, p.Y);
                    }
                }
            });
        }
        private static Command SetColorCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1), game, e).Value;
                Color? c = game.GetColor(args.ElementAtOrDefault(2), sender, target);
                if (c.HasValue)
                    sprite.Color = c.Value;
            });
        }
        private static Command CreateWarpToken(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "0", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "0", game, e);
                Texture t = getTexture(args.ElementAtOrDefault(3) ?? "sprites32", game, e).Value;
                if (t is object)
                {
                    Animation a = getAnimation(args.ElementAtOrDefault(4) ?? "WarpToken", game, t, e).Value;
                    if (a is object)
                    {
                        DecimalVariable rx = GetNumber(args.ElementAtOrDefault(5) ?? "0", game, e);
                        DecimalVariable ry = GetNumber(args.ElementAtOrDefault(6) ?? "0", game, e);
                        DecimalVariable ox = GetNumber(args.ElementAtOrDefault(7) ?? "0", game, e);
                        DecimalVariable oy = GetNumber(args.ElementAtOrDefault(8) ?? "0", game, e);
                        DecimalVariable f = GetNumber(args.ElementAtOrDefault(9) ?? "3", game, e);
                        string name = args.ElementAtOrDefault(10) ?? "warptoken";
                        int roomX = (int)rx.Value;
                        int roomY = (int)ry.Value;
                        WarpToken wt = new WarpToken(x.Value, y.Value, t, a, ox.Value + roomX * Room.ROOM_WIDTH, oy.Value + roomY * Room.ROOM_HEIGHT, roomX, roomY, game, (WarpToken.FlipSettings)(int)f.Value)
                        {
                            Name = name
                        };
                        if (!e.Locals.ContainsKey(name))
                        {
                            e.Locals.Add(name, new SpriteVariable(name, wt));
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
                DecimalVariable lx = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable ly = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable hx = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                DecimalVariable hy = GetNumber(args.ElementAtOrDefault(4) ?? "", game, e);
                game.MaxScrollX = hx.Value;
                game.MaxScrollY = hy.Value;
                game.MinScrollX = lx.Value;
                game.MinScrollY = ly.Value;
            });
        }
        private static Command DoubleJumpCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                Crewman c = sprite as Crewman;
                if (c is object)
                {
                    DecimalVariable j = GetNumber(args.ElementAtOrDefault(2) ?? "1", game, e);
                    c.MaxJumps = (int)j.Value;
                }
            });
        }
        private static Command KillCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "player", game, e).Value;
                (sprite as Crewman)?.Die();
            });
        }
        private static Command RandomCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable n = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                if (n.GetValue is object) return;
                DecimalVariable min = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable max = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                Random r = new Random(DateTime.Now.Millisecond + (int)n.Value);
                n.SetValue(r.Next((int)min.Value, (int)max.Value + 1));
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
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                Color? c = game.GetColor(args.ElementAtOrDefault(4) ?? "White", sender, target);
                if (!bool.TryParse(args.ElementAtOrDefault(6), out bool box))
                    box = false;
                game.HudText(name, text, new PointF(x.Value, y.Value), c.GetValueOrDefault(Color.White), box);
            });
        }
        private static Command HudSpriteCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                string name = args.ElementAtOrDefault(1) ?? "sprite";
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(2) ?? "0", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(3) ?? "0", game, e);
                Color? c = game.GetColor(args.ElementAtOrDefault(4) ?? "", sender, target);
                Texture t = getTexture(args.ElementAtOrDefault(5) ?? "", game, e).Value;
                Animation a = getAnimation(args.ElementAtOrDefault(6) ?? "", game, t, e).Value;
                game.HudSprite(name, new PointF(x.Value, y.Value), c, a, t);
            });
        }
        private static Command HudReplaceCommand(Game game, string[] args)
        {
            string name = args.ElementAtOrDefault(1) ?? "";
            string format = args.ElementAtOrDefault(4) ?? "0";
            string replace = args.ElementAtOrDefault(2) ?? "";
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable with = GetNumber(args.ElementAtOrDefault(3), game, e);
                float w = with.Value;
                string replaceWith = FormatNumber(with.Value, format);
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
                DecimalVariable size = GetNumber(args.ElementAtOrDefault(2) ?? "1", game, e);
                game.HudSize(name, size.Value);
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
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                SortedList<string, SpriteProperty> properties = sprite.Properties;
                if (properties.ContainsKey(args.ElementAtOrDefault(2) ?? ""))
                {
                    SpriteProperty sp = properties[args.ElementAtOrDefault(2) ?? ""];
                    DecimalVariable n = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                    if (sp.Type == SpriteProperty.Types.Float)
                    {
                        sp.SetValue(n.Value, game);
                    }
                    else if (sp.Type == SpriteProperty.Types.Int)
                    {
                        sp.SetValue((int)n.Value, game);
                    }
                    else if (sp.Type == SpriteProperty.Types.String)
                    {
                        sp.SetValue(n.Value.ToString(), game);
                    }
                }
            });
        }
        private static Command SetBoolCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
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
            });
        }
        private static Command SetTagCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                if (sprite is object)
                {
                    DecimalVariable v = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                    string name = args.ElementAtOrDefault(2) ?? "";
                    if (!sprite.Tags.ContainsKey(name))
                        sprite.Tags.Add(name, v.Value);
                    else
                        sprite.Tags[name] = v.Value;
                }
            });
        }
        private static Command RespawnCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite sprite = getSprite(args.ElementAtOrDefault(1) ?? "player", game, e).Value;
                Crewman c = sprite as Crewman;
                if (c is object)
                {
                    c.Respawn();
                }
            });
        }
        private static Command SetSpeedCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Sprite spr = getSprite(args.ElementAtOrDefault(1) ?? "", game, e).Value;
                DecimalVariable xSpeed = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                DecimalVariable ySpeed = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                float xs = xSpeed.Value;
                float ys = ySpeed.Value;
                bool relative = string.IsNullOrEmpty(args.ElementAtOrDefault(3));
                IBoundSprite sprite = spr as IBoundSprite;
                if (spr is null) return;
                if (relative)
                {
                    double sp = xs;
                    double an = Math.Atan2(sprite.YVelocity, sprite.XVelocity);
                    sprite.XVelocity = (float)(sp * Math.Cos(an));
                    sprite.YVelocity = (float)(sp * Math.Sin(an));
                }
                else
                {
                    sprite.XVelocity = xs;
                    sprite.YVelocity = ys;
                }
            });
        }
        private static Command TrinketCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                int id = (int)GetNumber(args.ElementAtOrDefault(1) ?? "0", game, e).Value;
                if (!game.CollectedTrinkets.Contains(id))
                {
                    game.CollectedTrinkets.Add(id);
                    Crewman c = getSprite(args.ElementAtOrDefault(2) ?? "", game, e).Value as Crewman;
                    if (c is object)
                    {
                        if (game.LoseTrinkets)
                            c.PendingTrinkets.Add(new Trinket(-20, -20, null, null, null, game));
                        else
                            c.HeldTrinkets.Add(id);
                    }
                }
            });
        }
        private static Command ShowMapCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                bool.TryParse(args.ElementAtOrDefault(1) ?? "true", out bool white);
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(2) ?? "0", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(3) ?? "0", game, e);
                DecimalVariable w = GetNumber(args.ElementAtOrDefault(4) ?? "320", game, e);
                DecimalVariable h = GetNumber(args.ElementAtOrDefault(5) ?? "240", game, e);
                DecimalVariable mx = GetNumber(args.ElementAtOrDefault(6) ?? "-1", game, e);
                DecimalVariable my = GetNumber(args.ElementAtOrDefault(7) ?? "-1", game, e);
                DecimalVariable mw = GetNumber(args.ElementAtOrDefault(8) ?? "-1", game, e);
                DecimalVariable mh = GetNumber(args.ElementAtOrDefault(9) ?? "-1", game, e);
                MapLayer m = game.ShowMap(x.Value, y.Value, w.Value, h.Value, (int)mx.Value, (int)my.Value, (int)mw.Value, (int)mh.Value, white, false);
                m.EnableSelect = false;
            });
        }
        private static Command HideMapCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.HideMap();
            });
        }
        private static Command GetRoomTagCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable n = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                if (n.GetValue is null)
                {
                    DecimalVariable x = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                    DecimalVariable y = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                    int xv = (int)x.Value;
                    int yv = (int)y.Value;
                    var o = game.RoomDatas[xv + yv * 100];
                    var tags = (Newtonsoft.Json.Linq.JObject)o["Tags"];
                    float t = 0;
                    if (tags is object)
                    {
                        t = (float)(tags[args.ElementAtOrDefault(4) ?? ""] ?? 0);
                    }
                    n.SetValue(t);
                }
            });
        }
        private static Command MapImageCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Texture t = getTexture(args.ElementAtOrDefault(1), game, e).Value;
                game.MapTexture = t;
            });
        }
        private static Command StartLoading(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                int id = (int)x.Value + (int)y.Value * 100;
                if (game.RoomGroups.TryGetValue(id, out RoomGroup g))
                {
                    if (!g.Loaded)
                        game.StartLoading(g);
                }
            });
        }
        private static Command MapAnimation(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                float x = GetNumber(args.ElementAtOrDefault(1) ?? "0", game, e).Value;
                float y = GetNumber(args.ElementAtOrDefault(2) ?? "0", game, e).Value;
                bool.TryParse(args.ElementAtOrDefault(3) ?? "false", out bool random);
                bool.TryParse(args.ElementAtOrDefault(4) ?? "false", out bool fade);
                Game.MapAnimations anim = Game.MapAnimations.None;
                if (x > 0)
                    anim |= Game.MapAnimations.Left;
                else if (x < 0)
                    anim |= Game.MapAnimations.Right;
                if (y > 0)
                    anim |= Game.MapAnimations.Up;
                else if (y < 0)
                    anim |= Game.MapAnimations.Down;
                if (random)
                    anim |= Game.MapAnimations.Random;
                if (fade)
                    anim |= Game.MapAnimations.Fade;
                game.MapAnimation = anim;
            });
        }
        private static Command CaptureSpritesCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                if (args.Length <= 2) return;
                string name = args.ElementAtOrDefault(1) ?? "capture";
                List<Sprite> sprites = new List<Sprite>();
                if (e.Locals.TryGetValue(name, out Variable already))
                {
                    sprites.Add(((SpriteVariable)already).Value);
                    e.Locals.Remove(name);
                }
                List<Predicate<Sprite>> conditions = new List<Predicate<Sprite>>();
                bool single = false;
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i].ToLower().StartsWith("type:"))
                    {
                        string type = args[i].Substring(5).ToLower();
                        if (Types.ContainsKey(type))
                        {
                            conditions.Add((s) => s.GetType() == Types[type]);
                        }
                    }
                    else if (args[i].ToLower().StartsWith("touching:"))
                    {
                        //string touch = args[i].Substring(9);
                        //Sprite sp = getSprite(touch, game, e).Value;
                        //conditions.Add((s) => (sp is object && sp.Length > 0) ? sp.Any((t) => s.IsOverlapping(t) is object) : false);
                    }
                    else if (args[i].ToLower().StartsWith("on:"))
                    {
                        //string on = args[i].Substring(3);
                        //Sprite[] sp = getSprite(on, game, e);
                        //List<Sprite> p = sp is object ? sp.ToList().FindAll((t) => t is Platform) : new List<Sprite>();
                        //if (sp is object & sp.Length > 0)
                        //{
                        //    conditions.Add((s) => {
                        //        if (p.Count == 0) return false;
                        //        return p.Any((t) => t is IPlatform & (t as IPlatform).OnTop.Contains(s));
                        //    });
                        //}
                    }
                    else if (args[i].ToLower() == "single")
                    {
                        single = true;
                    }
                }
                foreach (Sprite sprite in game.CurrentRoom.Objects.ToProcess)
                {
                    if (conditions.All((c) => c(sprite)))
                    {
                        sprites.Add(sprite);
                        if (single) break;
                    }
                }
                e.Locals.Add(name, new SpriteVariable(name, sprites[0]));
            });
        }
        private static Command CenterTextCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                VTextBox tb = e.TextBoxes.LastOrDefault();
                if (tb is object)
                {
                    tb.AlignToCenter();
                }
            });
        }
        private static Command AddMenuItemCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                string name = args.ElementAtOrDefault(1) ?? "Menu Item";
                Script s = game.ScriptFromName(args.ElementAtOrDefault(2) ?? "");
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(3) ?? "", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(4) ?? "", game, e);
                VMenuItem mi = new VMenuItem(name, () =>
                {
                    game.ExecuteScript(s, sender, target, new DecimalVariable[] { }, true);
                });
                game.MenuItems.Add(mi);
            });
        }
        private static Command ClearMenuItemsCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.MenuItems.Clear();
            });
        }
        private static Command PauseScriptCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                Script s = game.ScriptFromName(args.ElementAtOrDefault(1) ?? "");
                game.PauseScript = s;
            });
        }
        private static Command UnpauseCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                game.Freeze = Game.FreezeOptions.Unfrozen;
                //game.PauseScripts.Remove(e);
                //game.CurrentScripts.Add(e);
            });
        }
        private static Command TileToolCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x1 = GetNumber(args.ElementAtOrDefault(1) ?? "0", game, e);
                DecimalVariable y1 = GetNumber(args.ElementAtOrDefault(2) ?? "0", game, e);
                DecimalVariable x2 = null, y2 = null;
                if (args.Length > 4)
                {
                    x2 = GetNumber(args.ElementAtOrDefault(3) ?? "0", game, e);
                    y2 = GetNumber(args.ElementAtOrDefault(4) ?? "0", game, e);
                }
                List<PointF> points = new List<PointF>();
                int x = (int)x1.Value / 8 * 8;
                int y = (int)y1.Value / 8 * 8;
                if (x2 is object)
                {
                    int w = (int)x2.Value / 8 * 8;
                    int h = (int)y2.Value / 8 * 8;
                    for (int i = y; i <= h; i += 8)
                    {
                        for (int j = x; j <= w; j += 8)
                        {
                            points.Add(new PointF(j + game.CurrentRoom.GetX, i + game.CurrentRoom.GetY));
                        }
                    }
                }
                else
                {
                    points.Add(new PointF(x, y));
                }
                LevelEditor.Tools tool;
                AutoTileSettings at;
                char pre;
                bool lc = (args.ElementAtOrDefault(5) ?? "true").ToLower() == "true";
                DecimalVariable layer = GetNumber(args.ElementAtOrDefault(6) ?? "-2", game, e);
                switch (args[0].ToLower())
                {
                    case "backgroundtool":
                        tool = LevelEditor.Tools.Background;
                        at = game.CurrentRoom.Background;
                        pre = 'b';
                        break;
                    case "spikestool":
                        tool = LevelEditor.Tools.Spikes;
                        at = game.CurrentRoom.Spikes;
                        pre = 's';
                        break;
                    default:
                        tool = LevelEditor.Tools.Ground;
                        at = game.CurrentRoom.Ground;
                        pre = 'g';
                        break;
                }
                game.AutoTilesToolMulti(points, lc, tool, at, (int)layer.Value, game.CurrentRoom.TileTexture, pre);
                
            });
        }
        private static Command SaveRoomCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                if (!(game.CurrentRoom is RoomGroup))
                {
                    JObject save = game.CurrentRoom.Save(game);
                    JObject replace = game.RoomDatas[game.FocusedRoom];
                    replace["Tiles"] = save["Tiles"];
                }
            });
        }
        private static Command AddLightCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(1) ?? "0", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(2) ?? "0", game, e);
                DecimalVariable r = GetNumber(args.ElementAtOrDefault(3) ?? "0", game, e);
                game.AddLight(x.Value + game.CurrentRoom.GetX, y.Value + game.CurrentRoom.GetY, r.Value);
            });
        }
        private static Command SetLightCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable i = GetNumber(args.ElementAtOrDefault(1) ?? "0", game, e);
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(2) ?? "0", game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(3) ?? "0", game, e);
                DecimalVariable r = GetNumber(args.ElementAtOrDefault(4) ?? "0", game, e);
                game.SetLight((int)i.Value, x.Value + game.CurrentRoom.GetX, y.Value + game.CurrentRoom.GetY, r.Value);
            });
        }
        private static Command LightLevelCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable l = GetNumber(args.ElementAtOrDefault(1) ?? "100", game, e);
                game.MainLight = l.Value / 100f;
            });
        }
        private static Command VectorCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable var1 = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable var2 = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                if (var1.GetValue is object || var2.GetValue is object) return;
                DecimalVariable x = GetNumber(args.ElementAtOrDefault(3) ?? (args.ElementAtOrDefault(1) ?? ""), game, e);
                DecimalVariable y = GetNumber(args.ElementAtOrDefault(4) ?? (args.ElementAtOrDefault(2) ?? ""), game, e);
                float xv = x.Value;
                float yv = y.Value;
                double direction = Math.Atan2(yv, xv);
                float degrees = (float)(direction * 360 / (2 * Math.PI));
                float magnitude = (float)Math.Sqrt(xv * xv + yv * yv);
                var1.SetValue(magnitude);
                var2.SetValue(degrees);
            });
        }
        private static Command ScalarCommand(Game game, string[] args)
        {
            return new Command(game, (e, sender, target) =>
            {
                DecimalVariable var1 = GetNumber(args.ElementAtOrDefault(1) ?? "", game, e);
                DecimalVariable var2 = GetNumber(args.ElementAtOrDefault(2) ?? "", game, e);
                if (var1.GetValue is object || var2.GetValue is object) return;
                DecimalVariable m = GetNumber(args.ElementAtOrDefault(3) ?? (args.ElementAtOrDefault(1) ?? ""), game, e);
                DecimalVariable d = GetNumber(args.ElementAtOrDefault(4) ?? (args.ElementAtOrDefault(2) ?? ""), game, e);
                float mv = m.Value;
                float dv = d.Value;
                double direction = dv * Math.PI / 180;
                var1.SetValue((float)Math.Cos(direction) * mv);
                var2.SetValue((float)Math.Sin(direction) * mv);
            });
        }
        private static Command CustomCommand(Game game, Script script, string[] args)
        {
            Command command = null;
            command = new Command(game, (e, sender, target) =>
            {
                DecimalVariable[] numbers = new DecimalVariable[args.Length - 1];
                Texture texture = null;
                for (int i = 0; i < script.ArgCount; i++)
                {
                    ArgTypes type = script.GetArgType(i);
                    if (args.Length > i + 1)
                    {
                        switch (type)
                        {
                            case ArgTypes.Sprite:
                                Sprite sprite = getSprite(args[i + 1], game, e).Value;
                                numbers[i] = 0f;
                                break;
                            case ArgTypes.Texture:
                                texture = getTexture(args[i + 1], game, e).Value;
                                numbers[i] = Math.Max(game.Textures.IndexOfKey(texture.Name), 0);
                                break;
                            case ArgTypes.Animation:
                                if (texture is object)
                                {
                                    Animation a = getAnimation(args[i + 1], game, texture, e).Value;
                                    numbers[i] = Math.Max(texture.Animations.IndexOfKey(a.Name), 0);
                                }
                                else
                                    numbers[i] = 0;
                                break;
                            case ArgTypes.Sound:
                                float sound = game.Sounds.IndexOfKey(args[i + 1]);
                                if (sound == -1)
                                    sound = GetNumber(args[i + 1], game, e).Value;
                                numbers[i] = Math.Max(sound, 0);
                                break;
                            //case ArgTypes.Color:
                            //    break;
                            //case ArgTypes.Bool:
                            //    break;
                            case ArgTypes.Number:
                                numbers[i] = GetNumber(args[i + 1], game, e);
                                break;
                            //case ArgTypes.Mood:
                            //    break;
                            case ArgTypes.Music:
                                float music = game.Songs.IndexOfKey(args[i + 1]);
                                if (music == -1)
                                    music = GetNumber(args[i + 1], game, e).Value;
                                numbers[i] = Math.Max(music, 0);
                                break;
                            default:
                                break;
                        }
                    }
                }
                Script.Executor exec = game.ExecuteScript(script, sender, target, numbers);
                if (!exec.IsFinished)
                {
                    command.Wait = true;
                    game.CurrentScripts.Add(exec);
                    exec.Finished += (_) =>
                    {
                        e.Continue();
                    };
                }
            });
            return command;
        }
    }
}
