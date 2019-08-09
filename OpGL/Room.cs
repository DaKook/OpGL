using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Room
    {
        public List<Drawable> Objects = new List<Drawable>();

        public IEnumerable<Drawable> LoadRoom()
        {
            //TODO: Clone each drawable (or at least each non-static drawable) to be loaded to the game.
            return Objects;
        }
    }
}
