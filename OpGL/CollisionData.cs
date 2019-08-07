using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class CollisionData
    {
        public bool Vertical;
        public float Distance;
        public Drawable CollidedWith;

        public CollisionData(bool vertical, float distance, Drawable collidedWith)
        {
            Vertical = vertical;
            Distance = distance;
            CollidedWith = collidedWith;
        }
    }
}
