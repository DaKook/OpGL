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

        private static string[] presetcolors = new string[] { "cyan", "red", "yellow", "green", "purple", "blue", "gray", "terminal" };
        private delegate Command cmd(Game game, string[] args);
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
            { "endtext", EndTextCommand }
       };

        public static Script ParseScript(Game game, string script, string name = "")
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
                    commands.Add(cmdTypes[args[0]](game, args));
            }
            return new Script(commands.ToArray(), name, script);
        }

        private static Command SayCommand(Game game, string[] args)
        {
            Color sayTextBoxColor = Color.Gray;
            Crewman sayCrewman = game.SpriteFromName(args.ElementAtOrDefault(2)) as Crewman;
            if (sayCrewman != null)
            {
                sayTextBoxColor = sayCrewman.TextBoxColor;
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
                        break;
                    case "red":
                        sayTextBoxColor = Color.FromArgb(255, 60, 60);
                        break;
                    case "yellow":
                        sayTextBoxColor = Color.FromArgb(255, 255, 134);
                        break;
                    case "green":
                        sayTextBoxColor = Color.FromArgb(144, 255, 144);
                        break;
                    case "purple":
                        sayTextBoxColor = Color.FromArgb(255, 134, 255);
                        break;
                    case "blue":
                        sayTextBoxColor = Color.FromArgb(95, 95, 255);
                        break;
                    case "gray":
                    case "terminal":
                        sayTextBoxColor = Color.FromArgb(174, 174, 174);
                        break;
                }
            }

            return new Command(game, () =>
            {
                if (sayCrewman == null && args.ElementAtOrDefault(2).ToLower() == "player")
                {
                    sayCrewman = game.ActivePlayer;
                    sayTextBoxColor = game.ActivePlayer.TextBoxColor;
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
                sayTextBox.Appear();
                game.WaitingForAction = () =>
                {
                    sayTextBox.Disappear();
                    sayTextBox.Disappeared += (textBox) => game.hudSprites.Remove(textBox);
                    game.CurrentScript.Continue();
                };
            }, true);
        }
        private static Command TextCommand(Game game, string[] args)
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
            int.TryParse(args.ElementAtOrDefault(2 + txArgOffset), out int txX);
            int.TryParse(args.ElementAtOrDefault(3 + txArgOffset), out int txY);
            return new Command(game, () =>
            {
                VTextBox tb = new VTextBox(txX, txY, game.FontTexture, args.Last(), txTextBoxColor);
                game.TextBoxes.Add(tb);
                game.hudSprites.Add(tb);
            });
        }
        private static Command ChangeFontCommand(Game game, string[] args)
        {
            string fontTexture = args.ElementAtOrDefault(1);
            Texture newFont = game.TextureFromName(fontTexture);
            Action success = () => { };
            if (newFont != null && newFont.Width / newFont.TileSize == 16 && newFont.Height / newFont.TileSize == 16)
            {
                success = () => {
                    game.FontTexture = newFont;
                };
            }
            return new Command(game, success, false);
        }
        private static Command WaitCommand(Game game, string[] args)
        {
            int.TryParse(args.ElementAtOrDefault(1), out int frames);
            return new Command(game, () =>
            {
                game.DelayFrames = frames;
            }, true);
        }
        private static Command PlayerControlCommand(Game game, string[] args)
        {
            bool.TryParse(args.ElementAtOrDefault(1), out bool pc);
            return new Command(game, () =>
            {
                game.PlayerControl = pc;
            });
        }
        private static Command MoodCommand(Game game, string[] args)
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
        private static Command CheckpointCommand(Game game, string[] args)
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
        private static Command PositionCommand(Game game, string[] args)
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
        private static Command SpeakCommand(Game game, string[] args)
        {
            return new Command(game, () =>
            {
                if (game.TextBoxes.Count > 0)
                {
                    VTextBox tb = game.TextBoxes.Last();
                    tb.Appear();
                    game.WaitingForAction = () =>
                    {
                        game.CurrentScript.Continue();
                    };
                }
            }, true);
        }
        private static Command SpeakActiveCommand(Game game, string[] args)
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
                    game.WaitingForAction = () =>
                    {
                        game.CurrentScript.Continue();
                    };
                }

            }, true);
        }
        private static Command EndTextCommand(Game game, string[] args)
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

    }
}
