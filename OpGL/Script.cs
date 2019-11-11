using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Script
    {
        private int currentLocation = 0;


        public delegate void FinishedDelegate(Script script);
        public event FinishedDelegate Finished;

        public bool IsFinished { get; private set; }

        public string Name;
        public string Contents;

        public int WaitingFrames;
        public Action WaitingForAction = null;

        public List<VTextBox> TextBoxes = new List<VTextBox>();

        public Command[] Commands;
        public Script(Command[] commands, string name = "", string contents = "")
        {
            Commands = commands;
            Name = name;
            Contents = contents;
        }

        public static Script Empty => new Script(new Command[] { }, "", "");

        public Script ExecuteFromBeginning()
        {
            IsFinished = false;
            currentLocation = 0;
            WaitingForAction = null;
            WaitingFrames = 0;
            Continue();
            return this;
        }

        public void Continue()
        {
            if (currentLocation < Commands.Length)
                Commands[currentLocation++].Execute();
            else
                currentLocation++;
            while (currentLocation < Commands.Length && (currentLocation == 0 || !Commands[currentLocation - 1].Wait))
            {
                Commands[currentLocation++].Execute();
            }
            if (currentLocation >= Commands.Length && (currentLocation > Commands.Length || !Commands[currentLocation - 1].Wait))
            {
                Finished?.Invoke(this);
                IsFinished = true;
                currentLocation = 0;
                WaitingForAction = null;
                WaitingFrames = 0;
            }
            return ;
        }

        public void Process()
        {
            if (WaitingFrames > 0)
            {
                if (--WaitingFrames <= 0)
                {
                    Continue();
                }
            }
        }
    }
}
