using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class CollisionData
    {
        public bool Vertical;
        public double Distance;
        public Sprite CollidedWith;

        public CollisionData(bool vertical, double distance, Sprite collidedWith)
        {
            Vertical = vertical;
            Distance = Math.Round(distance, 4);
            CollidedWith = collidedWith;
        }
    }
}
