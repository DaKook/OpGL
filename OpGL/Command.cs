using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Command
    {
        private Action action;
        public bool Wait;

        public Command(Action action, bool wait = false)
        {
            this.action = action;
            Wait = wait;
        }

        public void Execute()
        {
            action();
        }
    }
}
