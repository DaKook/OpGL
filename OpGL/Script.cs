using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Script
    {
        public class Executor
        {
            private int currentLocation;
            private Script script;
            public bool IfSatisfied = false;
            public SortedList<string, Sprite> CreatedSprites = new SortedList<string, Sprite>();
            public Func<bool> ExitCondition;

            public Executor(Script script)
            {
                this.script = script;
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
                if (script.markers.TryGetValue(markerName, out int p))
                {
                    currentLocation = p;
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

            public Executor ExecuteFromBeginning(Sprite sender, Sprite target)
            {
                IsFinished = false;
                currentLocation = 0;
                WaitingForAction = null;
                WaitingFrames = 0;
                Sender = sender;
                Target = target;
                Continue(sender, target);
                return this;
            }

            public void Continue()
            {
                Continue(Sender, Target);
            }

            public void Continue(Sprite sender, Sprite target)
            {
                if (ExitCondition is object && ExitCondition())
                    Stop();
                if (IsFinished) return;
                if (currentLocation < script.Commands.Length)
                    script.Commands[currentLocation++].Execute(this, sender, target);
                else
                    currentLocation++;
                while (currentLocation < script.Commands.Length && (currentLocation == 0 || !script.Commands[currentLocation - 1].Wait) && !IsFinished)
                {
                    script.Commands[currentLocation++].Execute(this, sender, target);
                }
                if (currentLocation >= script.Commands.Length && (currentLocation > script.Commands.Length || !script.Commands[currentLocation - 1].Wait))
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
                CreatedSprites.Clear();
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
        private SortedList<string, int> markers = new SortedList<string, int>();

        public void AddMarker(string markerName, int position)
        {
            markers.Add(markerName, position);
        }

        public Command[] Commands;
        public Script(Command[] commands, string name = "", string contents = "")
        {
            Commands = commands;
            Name = name;
            Contents = contents;
        }

        public static Script Empty => new Script(new Command[] { }, "", "");
    }
}
