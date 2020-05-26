using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace OpGL
{
    class PushSprite : Sprite, IPlatform
    {
        public override Pushability Pushability => Pushability.PushSprite;
        //bool wpR;
        //bool wpL;
        //bool wpU;
        //bool wpD;
        public float YVel { get; set; } = 0;
        public float XVel { get; set; } = 0;
        public bool OnGround = false;
        public float Acceleration = 0.5f;

        public List<Sprite> OnTop { get; set; } = new List<Sprite>();
        public float Conveyor { get; set; }
        public bool SingleDirection { get; set; }

        public void Disappear()
        {
            //Do nothing
        }

        public PushSprite(float x, float y, Texture texture, Animation animation) : base(x, y, texture, animation)
        {
            Solid = SolidState.Ground;
            Gravity = 0.6875f;
            Pushable = true;
        }

        public override void Process()
        {
            base.Process();
            YVel += Gravity;
            if (YVel > Crewman.TerminalVelocity) YVel = Crewman.TerminalVelocity;
            else if (YVel < -Crewman.TerminalVelocity) YVel = -Crewman.TerminalVelocity;
            if (XVel > 0)
            {
                XVel -= Acceleration;
                if (XVel < 0) XVel = 0;
            }
            else if (XVel < 0)
            {
                XVel += Acceleration;
                if (XVel > 0) XVel = 0;
            }
            Move(XVel, YVel);
            if (!OnGround)
            {
                if (onPlatform != null)
                {
                    XVel += onPlatform.XVel + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                    YVel += onPlatform.YVel;
                    onPlatform.OnTop.Remove(this);
                    onPlatform = null;
                }
            }
            OnGround = false;
        }

        public override void Move(double x, double y)
        {
            base.Move(x, y);
            foreach (Sprite sprite in OnTop)
            {
                sprite.Move(x, y);
            }
        }

        public override void CollideY(double distance, Sprite collision)
        {
            DY -= distance;
            foreach (Sprite sprite in OnTop)
            {
                if (sprite != collision)
                    sprite.Move(0, -distance);
            }
            if (Math.Sign(distance) == Math.Sign(Gravity))
            {
                OnGround = true;
                //Check if landing on a platform
                if (collision is IPlatform && onPlatform != collision)
                {
                    if (onPlatform != null)
                    {
                        XVel += onPlatform.XVel + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                        YVel += onPlatform.YVel;
                        onPlatform.OnTop.Remove(this);
                    }
                    onPlatform = collision as IPlatform;
                    onPlatform.OnTop.Add(this);
                }
                else if (onPlatform != null && !(collision is IPlatform))
                {
                    XVel += onPlatform.XVel + onPlatform.Conveyor * (Gravity < 0 && !onPlatform.SingleDirection ? -1 : 1);
                    YVel += onPlatform.YVel;
                    onPlatform.OnTop.Remove(this);
                    onPlatform = null;
                }
            }
            if (Math.Sign(distance) == Math.Sign(YVel))
            {
                YVel = 0;
            }
        }

        public override void CollideX(double distance, Sprite collision)
        {
            Move((float)Math.Round(-distance, 4), 0);
            if (Math.Sign(distance) == Math.Sign(XVel))
            {
                XVel = 0;
            }
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

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "Push");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Animation", Animation.Name);
        //    if (Color != Color.White)
        //        ret.Add("Color", Color.ToArgb());
        //    if (Gravity != 0.6875f)
        //        ret.Add("Gravity", Gravity);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret["Gravity"].CanSet = true;
                ret["Type"].GetValue = () => "Push";
                return ret;
            }
        }

        public override JObject Save(Game game, bool isUniversal = false)
        {
            JObject ret = base.Save(game, isUniversal);
            if (OnTop.Count > 0)
            {
                JArray ot = new JArray();
                foreach (Sprite sprite in OnTop)
                {
                    ot.Add(sprite.Save(game, false));
                }
                ret.Add("Attached", ot);
            }
            return ret;
        }
    }
}
