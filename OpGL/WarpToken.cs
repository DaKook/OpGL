using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    class WarpToken : Sprite
    {
        public enum FlipSettings { KeepFlip, ReverseFlip, Flip, Unflip }
        public FlipSettings Settings;
        public float OutX;
        public float OutY;
        private Game game;
        public static SoundEffect WarpSound;
        public WarpToken(float x, float y, Texture texture, Animation animation, float outX, float outY, Game owner, FlipSettings flip = FlipSettings.Unflip) : base(x, y, texture, animation)
        {
            Settings = flip;
            OutX = outX;
            OutY = outY;
            game = owner;
            Immovable = true;
            Solid = SolidState.Entity;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            crewman.CenterX = OutX + Width / 2;
            crewman.PreviousX = crewman.X;
            switch (Settings)
            {
                case FlipSettings.ReverseFlip:
                    crewman.Gravity *= -1;
                    break;
                case FlipSettings.Flip:
                    crewman.Gravity = -Math.Abs(crewman.Gravity);
                    break;
                case FlipSettings.Unflip:
                    crewman.Gravity = Math.Abs(crewman.Gravity);
                    break;
            }
            if (crewman.Gravity < 0) crewman.Y = OutY;
            else crewman.Bottom = OutY + Height;
            crewman.PreviousY = crewman.Y;
            crewman.YVelocity = 0;
            game.Flash(10);
            game.Shake(40);
            WarpSound?.Play();
        }

        public override JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "WarpToken");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            ret.Add("Animation", Animation.Name);
            ret.Add("OutX", OutX);
            ret.Add("OutY", OutY);
            ret.Add("Flip", (int)Settings);
            return ret;
        }
    }
}
