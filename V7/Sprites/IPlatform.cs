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
        float XVelocity { get; set; }
        float YVelocity { get; set; }
        float Conveyor { get; set; }
        bool SingleDirection { get; set; }
        bool Sticky { get; set; }
        void Disappear();
    }
}
