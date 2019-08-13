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

        public string Name;
        public string Contents;

        public Command[] Commands;
        public Script(Command[] commands, string name = "", string contents = "")
        {
            Commands = commands;
            Name = name;
            Contents = contents;
        }

        public Script ExecuteFromBeginning()
        {
            currentLocation = 0;
            Continue();
            return this;
        }

        public void Continue()
        {
            if (currentLocation < Commands.Length)
                Commands[currentLocation++].Execute();
            while (currentLocation < Commands.Length && (currentLocation == 0 || !Commands[currentLocation - 1].Wait))
            {
                Commands[currentLocation++].Execute();
            }
            if (Finished != null && currentLocation >= Commands.Length) Finished(this);
            return ;
        }
    }
}
