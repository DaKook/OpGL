using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace V7
{
    public class VMenuItem
    {
        public string Text;
        public Action Action;
        public PointF Offset;
        public string Description;
        public VMenuItem(string text, Action action, string description = "")
        {
            Text = text;
            Action = action;
            Description = description;
        }
    }
}
