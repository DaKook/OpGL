using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace OpGL
{
    public class Command
    {
        public Game game;
        private Action action;
        public bool Wait;

        public Command(Game game, Action action, bool wait = false)
        {
            this.game = game;
            this.action = action;
            Wait = wait;
        }

        public void Execute()
        {
            action();
        }

        private static Number getNumber(string s, Game game)
        {
            if (s.StartsWith("?"))
            {
                s = s.Substring(1);
                string[] ss = s.Split(':');
                if (ss.Length == 2)
                {
                    Sprite sprite = game.SpriteFromName(ss[0]);
                    if (sprite != null)
                    {
                        switch (ss[1].ToLower())
                        {
                            case "x":
                                return new Number(s, sprite, Number.SourceTypes.X);
                            case "y":
                                return new Number(s, sprite, Number.SourceTypes.Y);
                            case "checkpointx":
                                return new Number(s, sprite, Number.SourceTypes.CheckX);
                            case "checkpointy":
                                return new Number(s, sprite, Number.SourceTypes.CheckY);
                        }
                    }
                }
            }
            if (game.Vars.ContainsKey(s))
                return game.Vars[s];
            else
            {
                Number.TryParse(s, out Number ret);
                return ret;
            }
        }

        private static string[] presetcolors = new string[] { "cyan", "red", "yellow", "green", "purple", "blue", "gray", "terminal" };
        private delegate Command cmd(Game game, string[] args, Script script);
        private static Dictionary<string, cmd> cmdTypes = new Dictionary<string, cmd> {
            { "say", SayCommand },
            { "text", TextCommand },
            { "changefont", ChangeFontCommand },
            { "delay", WaitCommand },
            { "playercontrol", PlayerControlCommand },
            { "mood", MoodCommand },
            { "checkpoint", CheckpointCommand },
            { "position", PositionCommand },
            { "speak", SpeakCommand },
            { "speak_active", SpeakActiveCommand },
            { "endtext", EndTextCommand },
            { "squeak", SqueakCommand },
            { "playef", PlaySoundCommand },
            { "playsound", PlaySoundCommand },
            { "addsprite", AddSpriteCommand },
            { "changeai", ChangeAICommand }
       };

        public static Command[] ParseScript(Game game, string script, Script parent)
        {
            string[] lines = script.Replace(Environment.NewLine, "\n").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<Command> commands = new List<Command>();
            int i = 0;
            while (i < lines.Length)
            {
                string[] args = lines[i++].Split(new char[] { ',', '(', ')' });
                args[0] = args[0].ToLower();

                // multi-line commands
                int txLines = 0;
                if (args[0] == "say")
                {
                    if (!int.TryParse(args.ElementAtOrDefault(1), out txLines)) continue;
                }
                else if (args[0] == "text")
                    if (!int.TryParse(args.LastOrDefault(), out txLines)) continue;
                if (txLines > 0)
                {
                    Array.Resize(ref args, args.Length + 1);
                    args[args.Length - 1] = string.Join("\n", lines, i, txLines);
                    i += txLines;
                }

                if (cmdTypes.ContainsKey(args[0]))
                    commands.Add(cmdTypes[args[0]](game, args, parent));
            }
            return commands.ToArray();
        }

        private static Command SayCommand(Game game, string[] args, Script script)
        {
            Color sayTextBoxColor = Color.Gray;
            Crewman sayCrewman = game.SpriteFromName(args.ElementAtOrDefault(2)) as Crewman;
            SoundEffect squeak = null;
            if (sayCrewman != null)
            {
                sayTextBoxColor = sayCrewman.TextBoxColor;
                squeak = sayCrewman.Squeak;
            }
            else if (args.Length == 6)
            {
                int.TryParse(args[2], out int r);
                int.TryParse(args[3], out int g);
                int.TryParse(args[4], out int b);
                sayTextBoxColor = Color.FromArgb(r, g, b);
            }
            else if (presetcolors.Contains(args.ElementAtOrDefault(2)))
            {
                switch (args[2])
                {
                    case "cyan":
                        sayTextBoxColor = Color.FromArgb(164, 164, 255);
                        squeak = game.GetSound("crew1");
                        break;
                    case "red":
                        sayTextBoxColor = Color.FromArgb(255, 60, 60);
                        squeak = game.GetSound("crew6");
                        break;
                    case "yellow":
                        sayTextBoxColor = Color.FromArgb(255, 255, 134);
                        squeak = game.GetSound("crew4");
                        break;
                    case "green":
                        sayTextBoxColor = Color.FromArgb(144, 255, 144);
                        squeak = game.GetSound("crew2");
                        break;
                    case "purple":
                        sayTextBoxColor = Color.FromArgb(255, 134, 255);
                        squeak = game.GetSound("crew5");
                        break;
                    case "blue":
                        sayTextBoxColor = Color.FromArgb(95, 95, 255);
                        squeak = game.GetSound("crew3");
                        break;
                    case "gray":
                    case "terminal":
                        sayTextBoxColor = Color.FromArgb(174, 174, 174);
                        squeak = game.GetSound("blip2");
                        break;
                }
            }

            return new Command(game, () =>
            {
                if (sayCrewman == null && args.ElementAtOrDefault(2).ToLower() == "player")
                {
                    sayCrewman = game.ActivePlayer;
                    sayTextBoxColor = game.ActivePlayer.TextBoxColor;
                    squeak = game.ActivePlayer.Squeak;
                }
                VTextBox sayTextBox = new VTextBox(0, 0, game.FontTexture, args.Last(), sayTextBoxColor);
                if (sayCrewman != null)
                {
                    sayTextBox.Bottom = sayCrewman.Y - 2;
                    sayTextBox.X = sayCrewman.X - 16;
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
                game.hudSprites.Add(sayTextBox);
                squeak?.Play();
                sayTextBox.Appear();
                game.WaitingForAction = true;
                script.WaitingForAction = () =>
                {
                    sayTextBox.Disappear();
                    sayTextBox.Disappeared += (textBox) => game.hudSprites.Remove(textBox);
                };
            }, true);
        }
        private static Command SqueakCommand(Game game, string[] args, Script script)
        {
            SoundEffect squeak = null;
            switch (args.ElementAtOrDefault(1))
            {
                case "cyan":
                    squeak = game.GetSound("crew1");
                    break;
                case "red":
                    squeak = game.GetSound("crew6");
                    break;
                case "yellow":
                    squeak = game.GetSound("crew4");
                    break;
                case "green":
                    squeak = game.GetSound("crew2");
                    break;
                case "purple":
                    squeak = game.GetSound("crew5");
                    break;
                case "blue":
                    squeak = game.GetSound("crew3");
                    break;
                case "gray":
                case "terminal":
                    squeak = game.GetSound("blip2");
                    break;
            }
            return new Command(game, () => {
                if (args.ElementAtOrDefault(1).ToLower() == "player")
                    squeak = game.ActivePlayer.Squeak;
                else if (args.ElementAtOrDefault(1).ToLower() == "cry")
                    squeak = Crewman.Cry;
                squeak?.Play();
            }, false);
        }
        private static Command TextCommand(Game game, string[] args, Script script)
        {
            Color txTextBoxColor = Color.Gray;
            Crewman sayCrewman = game.SpriteFromName(args.ElementAtOrDefault(1)) as Crewman;
            int txArgOffset = 0;
            if (sayCrewman != null)
            {
                txTextBoxColor = sayCrewman.TextBoxColor;
            }
            else if (args.Length == 8)
            {
                txArgOffset = 2;
                int.TryParse(args[2], out int r);
                int.TryParse(args[3], out int g);
                int.TryParse(args[4], out int b);
                txTextBoxColor = Color.FromArgb(r, g, b);
            }
            else if (presetcolors.Contains(args.ElementAtOrDefault(1)))
            {
                switch (args[1])
                {
                    case "cyan":
                        txTextBoxColor = Color.FromArgb(164, 164, 255);
                        break;
                    case "red":
                        txTextBoxColor = Color.FromArgb(255, 60, 60);
                        break;
                    case "yellow":
                        txTextBoxColor = Color.FromArgb(255, 255, 134);
                        break;
                    case "green":
                        txTextBoxColor = Color.FromArgb(144, 255, 144);
                        break;
                    case "purple":
                        txTextBoxColor = Color.FromArgb(255, 134, 255);
                        break;
                    case "blue":
                        txTextBoxColor = Color.FromArgb(95, 95, 255);
                        break;
                    case "gray":
                    case "terminal":
                        txTextBoxColor = Color.FromArgb(174, 174, 174);
                        break;
                }
            }
            Number txX = getNumber(args.ElementAtOrDefault(2 + txArgOffset), game);
            Number txY = getNumber(args.ElementAtOrDefault(3 + txArgOffset), game);
            return new Command(game, () =>
            {
                VTextBox tb = new VTextBox(txX, txY, game.FontTexture, args.Last(), txTextBoxColor);
                game.TextBoxes.Add(tb);
                game.hudSprites.Add(tb);
            });
        }
        private static Command ChangeFontCommand(Game game, string[] args, Script script)
        {
            string fontTexture = args.ElementAtOrDefault(1);
            Texture newFont = game.TextureFromName(fontTexture);
            Action success = () => { };
            if (newFont != null && newFont.Width / newFont.TileSizeX == 16 && newFont.Height / newFont.TileSizeY == 16)
            {
                success = () => {
                    game.FontTexture = newFont;
                };
            }
            return new Command(game, success, false);
        }
        private static Command WaitCommand(Game game, string[] args, Script script)
        {
            Number frames = getNumber(args.LastOrDefault(), game);
            return new Command(game, () =>
            {
                script.WaitingFrames = (int)frames;
            }, true);
        }
        private static Command PlayerControlCommand(Game game, string[] args, Script script)
        {
            bool.TryParse(args.ElementAtOrDefault(1), out bool pc);
            return new Command(game, () =>
            {
                game.PlayerControl = pc;
            });
        }
        private static Command MoodCommand(Game game, string[] args, Script script)
        {
            Crewman crewman = game.SpriteFromName(args[1]) as Crewman;
            if (crewman == null) crewman = game.ActivePlayer;
            bool sad = (args[2].ToLower() == "sad" || args[2] == "1");
            return new Command(game, () =>
            {
                if (crewman == null && args.ElementAtOrDefault(1).ToLower() == "player") crewman = game.ActivePlayer;
                if (crewman != null)
                    crewman.Sad = sad;
            });
        }
        private static Command CheckpointCommand(Game game, string[] args, Script script)
        {
            return new Command(game, () =>
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
        private static Command PositionCommand(Game game, string[] args, Script script)
        {
            string p = args.ElementAtOrDefault(2);
            string c = args.ElementAtOrDefault(1);

            return new Command(game, () =>
            {
                if (game.TextBoxes.Count > 0)
                {
                    VTextBox tb = game.TextBoxes.Last();
                    if (c.ToLower() == "centerx" || c.ToLower() == "center")
                        tb.CenterX = Game.RESOLUTION_WIDTH / 2;
                    if (c.ToLower() == "centery" || c.ToLower() == "center")
                        tb.CenterY = Game.RESOLUTION_HEIGHT / 2;
                    Crewman crewman = game.SpriteFromName(c) as Crewman;
                    if (c.ToLower() == "player") crewman = game.ActivePlayer;
                    if (crewman != null)
                    {
                        if (p == "above")
                        {
                            tb.Bottom = crewman.Y - 2;
                            tb.X = crewman.X - 16;
                        }
                        else
                        {
                            tb.Y = crewman.Bottom + 2;
                            tb.X = crewman.X - 16;
                        }
                    }
                }
            });
        }
        private static Command SpeakCommand(Game game, string[] args, Script script)
        {
            return new Command(game, () =>
            {
                if (game.TextBoxes.Count > 0)
                {
                    VTextBox tb = game.TextBoxes.Last();
                    tb.Appear();
                    game.WaitingForAction = true;
                    script.WaitingForAction = () =>
                    {
                        
                    };
                }
            }, true);
        }
        private static Command SpeakActiveCommand(Game game, string[] args, Script script)
        {
            return new Command(game, () =>
            {
                if (game.TextBoxes.Count > 0)
                {
                    VTextBox tb = game.TextBoxes.Last();
                    for (int i = game.TextBoxes.Count - 2; i >= 0; i--)
                    {
                        game.TextBoxes[i].Disappear();
                        game.TextBoxes[i].Disappeared += (textBox) => game.hudSprites.Remove(textBox);
                    }
                    tb.Appear();
                    game.WaitingForAction = true;
                    script.WaitingForAction = () =>
                    {
                        
                    };
                }

            }, true);
        }
        private static Command EndTextCommand(Game game, string[] args, Script script)
        {
            return new Command(game, () =>
            {
                for (int i = game.TextBoxes.Count - 1; i >= 0; i--)
                {
                    game.TextBoxes[i].Disappear();
                    game.TextBoxes[i].Disappeared += (textBox) => game.hudSprites.Remove(textBox);
                }
            });
        }
        private static Command PlaySoundCommand(Game game, string[] args, Script script)
        {
            SoundEffect se = game.GetSound(args.LastOrDefault());
            return new Command(game, () => { se.Play(); }, false);
        }
        private static Command AddSpriteCommand(Game game, string[] args, Script script)
        {
            Number x = getNumber(args.ElementAtOrDefault(2), game);
            Number y = getNumber(args.ElementAtOrDefault(3), game);
            Sprite s = game.SpriteFromName(args.ElementAtOrDefault(1));
            return new Command(game, () =>
            {
                if (s != null)
                {
                    game.AddSprite(s, x, y);
                }
            }, false);
        }
        private static Command ChangeAICommand(Game game, string[] args, Script script)
        {
            Crewman crewman1 = game.SpriteFromName(args.ElementAtOrDefault(1)) as Crewman;
            Crewman crewman2 = game.SpriteFromName(args.ElementAtOrDefault(3)) as Crewman;
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
            return new Command(game, () =>
            {
                if (crewman1 != null)
                {
                    if (crewman2 == null) crewman2 = game.ActivePlayer;
                    crewman1.AIState = aiState;
                    if (aiState != Crewman.AIStates.Stand)
                        crewman1.Target = crewman2;
                }
            }, false);
        }

    }
}
