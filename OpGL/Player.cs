using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    class Player : Crewman
    {
        public Player(float x, float y, Texture texture, string name = "", Animation stand = null, Animation walk = null, Animation fall = null, Animation jump = null, Animation die = null) : base(x, y, texture, name, stand, walk, fall, jump, die)
        {

        }
    }
}
