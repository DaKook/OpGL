using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public interface IPlatform
    {
        List<Sprite> OnTop { get; set; }
        float XVel { get; set; }
        float YVel { get; set; }
        float Conveyor { get; set; }
        bool SingleDirection { get; set; }
        void Disappear();
    }
}
