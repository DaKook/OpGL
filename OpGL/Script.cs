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

        public Command[] Commands;
        public Script(Command[] commands)
        {
            Commands = commands;
        }

        public Script ExecuteFromBeginning()
        {
            currentLocation = 0;
            Continue();
            return this;
        }

        public bool Continue()
        {
            if (currentLocation < Commands.Length)
                Commands[currentLocation++].Execute();
            while (currentLocation < Commands.Length && (currentLocation == 0 || !Commands[currentLocation - 1].Wait))
            {
                Commands[currentLocation++].Execute();
            }
            return currentLocation >= Commands.Length;
        }
    }
}
