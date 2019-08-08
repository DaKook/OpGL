using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Command
    {
        public Action Action;
        public bool Wait;

        public Command(Action action, bool wait = false)
        {
            Action = action;
            Wait = wait;
        }

        public void Execute()
        {
            Action();
        }
    }
}
