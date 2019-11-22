using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace OpGL
{
    class PushSprite : Sprite
    {
        //bool wpR;
        //bool wpL;
        //bool wpU;
        //bool wpD;
        float YVelocity = 0;
        float XVelocity = 0;
        bool OnGround = false;
        public PushSprite(float x, float y, Texture texture, Animation animation) : base(x, y, texture, animation)
        {
            Solid = SolidState.Ground;
            Gravity = 0.6875f;
            Pushable = true;
        }

        public override void Process()
        {
            base.Process();
            YVelocity += Gravity;
            if (YVelocity > Crewman.TerminalVelocity) YVelocity = Crewman.TerminalVelocity;
            else if (YVelocity < -Crewman.TerminalVelocity) YVelocity = -Crewman.TerminalVelocity;
            Y += YVelocity;
            //wpL = wpR = wpU = wpD = false;
        }

        //public override void CollideY(float distance, Sprite collision)
        //{
        //    if (distance < 0 && wpU && wpD)
        //        collision.CollideY(-distance, this);
        //    else if (distance > 0 && wpU && wpD)
        //        collision.CollideY(-distance, this);
        //    else
        //    {
        //        base.CollideY(distance, collision);
        //        YVelocity = 0;
        //        if (distance < 0) wpU = true;
        //        else if (distance > 0) wpD = true;
        //    }
        //}

        //public override void CollideX(float distance, Sprite collision)
        //{
        //    if (distance < 0 && wpL)
        //        collision.CollideX(-distance, this);
        //    else if (distance > 0 && wpR)
        //        collision.CollideX(-distance, this);
        //    else
        //    {
        //        base.CollideX(distance, collision);
        //        if (distance < 0) wpL = true;
        //        else if (distance > 0) wpR = true;
        //    }
        //}

        public override JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "Push");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            ret.Add("Animation", Animation.Name);
            if (Color != Color.White)
                ret.Add("Color", Color.ToArgb());
            if (Gravity != 0.6875f)
                ret.Add("Gravity", Gravity);
            return ret;
        }
    }
}
