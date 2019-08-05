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
        public Drawable Between;
        public float BetweenDistance;

        public CollisionData(bool isColliding, bool vertical = true, float distance = 0f, Drawable collidedWith = null, Drawable between = null, float betweenDistance = 0)
        {
            IsColliding = isColliding;
            Vertical = vertical;
            Distance = distance;
            CollidedWith = collidedWith;
            Between = between;
            BetweenDistance = betweenDistance;
        }
    }
}
