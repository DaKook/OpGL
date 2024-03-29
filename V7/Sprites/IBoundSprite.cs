﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    interface IBoundSprite
    {
        Rectangle Bounds { get; set; }
        float XVelocity { get; set; }
        float YVelocity { get; set; }
        float X { get; set; }
        float Y { get; set; }
        float InitialX { get; set; }
        float InitialY { get; set; }
    }
}
