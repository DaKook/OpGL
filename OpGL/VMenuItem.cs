﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class VMenuItem
    {
        public string Text;
        public Action Action;
        public VMenuItem(string text, Action action)
        {
            Text = text;
            Action = action;
        }
    }
}
