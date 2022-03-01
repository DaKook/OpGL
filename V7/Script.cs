using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class Script
    {
        public class Executor
        {
            private int currentLocation;
            public Script Script { get; private set; }
            public bool IfSatisfied = false;
            public SortedList<string, Variable> Locals = new SortedList<string, Variable>();
            public Func<bool> ExitCondition;
            DecimalVariable[] Args;
            public Game Game;

            public DecimalVariable GetArg(int index)
            {
                if (index < 0 || index >= Args.Length)
                    return 0;
                else
                    return Args[index];
            }

            public Executor(Script script, Game game, DecimalVariable[] args = null)
            {
                Script = script;
                if (args is object)
                {
                    Args = args;
                }
                Game = game;
            }

            public delegate void FinishedDelegate(Executor script);
            public event FinishedDelegate Finished;

            public bool IsFinished { get; private set; }

            public int WaitingFrames;
            public Action WaitingForAction = null;
            public Func<bool> Waiting = null;
            public List<VTextBox> TextBoxes = new List<VTextBox>();
            private Stack<int> currentStack = new Stack<int>();

            public Sprite Sender;
            public Sprite Target;

            public void AddMarker()
            {
                currentStack.Push(currentLocation);
            }

            public void GoToMarker(string markerName)
            {
                if (Script.markers.TryGetValue(markerName, out int p))
                {
                    currentLocation = p;
                    skipped = true;
                }
            }

            public void ReturnToMarker()
            {
                currentLocation = currentStack.Pop();
                currentStack.Push(currentLocation);
            }

            public void RemoveMarker()
            {
                currentStack.Pop();
            }

            public void SkipAhead()
            {
                if (Script.braces.TryGetValue(currentLocation, out int l))
                {
                    currentLocation += l;
                    skipped = true;
                }
                else
                {
                    currentLocation += 1;
                    skipped = true;
                }
            }

            public Executor ExecuteFromBeginning(Sprite sender, Sprite target)
            {
                IsFinished = false;
                currentLocation = 0;
                WaitingForAction = null;
                WaitingFrames = 0;
                Sender = sender;
                Target = target;
                skipped = false;
                Continue(sender, target);
                return this;
            }

            public void Continue()
            {
                Continue(Sender, Target);
            }

            private bool skipped = false;
            public void Continue(Sprite sender, Sprite target)
            {
                if (ExitCondition is object && ExitCondition())
                    Stop();
                if (IsFinished) return;
                if (currentLocation < Script.Commands.Length)
                    Script.Commands[currentLocation++].Execute(this, sender, target);
                else
                    currentLocation++;
                skipped = false;
                while (currentLocation < Script.Commands.Length && (currentLocation == 0 || !Script.Commands[currentLocation - 1].Wait || skipped) && !IsFinished)
                {
                    skipped = false;
                    Script.Commands[currentLocation++].Execute(this, sender, target);
                }
                skipped = false;
                if (currentLocation >= Script.Commands.Length && (currentLocation > Script.Commands.Length || !Script.Commands[currentLocation - 1].Wait))
                {
                    Finished?.Invoke(this);
                    IsFinished = true;
                    currentLocation = 0;
                    WaitingForAction = null;
                    WaitingFrames = 0;
                }
                return;
            }

            public void Stop()
            {
                Finished?.Invoke(this);
                IsFinished = true;
                currentLocation = 0;
                WaitingForAction = null;
                WaitingFrames = 0;
                Locals.Clear();
                foreach (VTextBox tb in TextBoxes)
                {
                    tb.Disappear();
                }
            }

            public void Process()
            {
                if (ExitCondition is object && ExitCondition())
                    Stop();
                if (WaitingFrames > 0)
                {
                    if (--WaitingFrames <= 0)
                    {
                        Continue();
                    }
                }
                else if (Waiting is object && Waiting())
                {
                    Waiting = null;
                    Continue();
                }
            }
        }


        public string Name;
        public string Contents;

        // Script settings
        public int CyclePersistence = 500;

        private SortedList<string, int> markers = new SortedList<string, int>();
        private SortedList<int, int> braces = new SortedList<int, int>();
        private Stack<int> openedBraces = new Stack<int>();

        public void OpenBraces(int position)
        {
            if (openedBraces.Count == 0 || openedBraces.Peek() != position)
                openedBraces.Push(position);
        }

        public void CloseBraces(int position)
        {
            if (openedBraces.Count > 0)
            {
                int i = openedBraces.Pop();
                braces.Add(i, position - i);
            }
        }

        public void AddMarker(string markerName, int position)
        {
            if (!markers.ContainsKey(markerName))
                markers.Add(markerName, position);
        }

        public void ClearMarkers()
        {
            markers.Clear();
            braces.Clear();
            openedBraces.Clear();
        }

        private List<Command.ArgTypes> argTypes = new List<Command.ArgTypes>();
        private SortedList<string, int> names = new SortedList<string, int>();
        private List<float> defaults = new List<float>();

        public void ClearArgs()
        {
            argTypes.Clear();
            names.Clear();
            defaults.Clear();
        }

        public void AddArg(string name, Command.ArgTypes type, float defaultValue)
        {
            names.Add(name, names.Count);
            argTypes.Add(type);
            defaults.Add(defaultValue);
        }

        public string GetArgName(int index)
        {
            if (index < 0 || index > names.Count)
                return "";
            else
                return names.Keys[index];
        }

        public int GetArgIndex(string name)
        {
            if (names.TryGetValue(name, out int ret))
                return ret;
            else
                return -1;
        }

        public Command.ArgTypes GetArgType(int index)
        {
            if (index < 0 || index > argTypes.Count)
                return Command.ArgTypes.None;
            else
                return argTypes[index];
        }

        public float GetArgDefault(int index)
        {
            if (index < 0 || index > defaults.Count)
                return 0;
            else
                return defaults[index];
        }

        public int ArgCount => argTypes.Count;

        public Command[] Commands;
        public Script(Command[] commands, string name = "", string contents = "")
        {
            Commands = commands;
            Name = name;
            Contents = contents;
        }

        public override string ToString()
        {
            return Name;
        }

        public static Script Empty => new Script(new Command[] { }, "", "");
    }
}
