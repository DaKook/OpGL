using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace V7
{
    public class PassedKeyEvent
    {
        public bool Shift { get; }
        public bool Control { get; }
        public bool Alt { get; }
        public Keys Key { get; }
        public bool Pass { get; set; }
        public PassedKeyEvent(KeyboardKeyEventArgs e)
        {
            Key = e.Key;
            Shift = e.Shift;
            Control = e.Control;
            Alt = e.Alt;
        }
    }
}
