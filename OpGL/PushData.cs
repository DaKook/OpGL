using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public struct PushData
    {
        public Pushability Up;
        public Pushability Down;
        public Pushability Right;
        public Pushability Left;
        public PushData(Pushability up, Pushability down, Pushability left, Pushability right)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
        }

        public PushData(Pushability all)
        {
            Up = Down = Left = Right = all;
        }

        public Pushability GetPushability(CollisionData cd)
        {
            if (cd.Vertical)
            {
                if (cd.Distance > 0)
                    return Down;
                else
                    return Up;
            }
            else
            {
                if (cd.Distance > 0)
                    return Right;
                else
                    return Left;
            }
        }
        public Pushability GetOppositePushability(CollisionData cd)
        {
            if (cd.Vertical)
            {
                if (cd.Distance > 0)
                    return Up;
                else
                    return Down;
            }
            else
            {
                if (cd.Distance > 0)
                    return Left;
                else
                    return Right;
            }
        }
        public void SetPushability(CollisionData cd, Pushability val)
        {
            if (cd.Vertical)
            {
                if (cd.Distance > 0)
                    Down = val;
                else
                    Up = val;
            }
            else
            {
                if (cd.Distance > 0)
                    Right = val;
                else
                    Left = val;
            }
        }
        public void SetOppositePushability(CollisionData cd, Pushability val)
        {
            if (cd.Vertical)
            {
                if (cd.Distance > 0)
                    Up = val;
                else
                    Down = val;
            }
            else
            {
                if (cd.Distance > 0)
                    Left = val;
                else
                    Right = val;
            }
        }
    }
}
