using System;
using System.Collections.Generic;
using System.Text;

namespace V7
{
    public interface ISolidObject
    {
        Tile.TileStates State { get; set; }
    }
}
