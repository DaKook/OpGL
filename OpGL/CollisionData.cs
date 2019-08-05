using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class CollisionData
    {
        public bool IsColliding;
        public bool Vertical;
        public float Distance;
        public Drawable CollidedWith;

        public CollisionData(bool isColliding, bool vertical = true, float distance = 0f, Drawable collidedWith = null)
        {
            IsColliding = isColliding;
            Vertical = vertical;
            Distance = distance;
            CollidedWith = collidedWith;
        }
    }
}
