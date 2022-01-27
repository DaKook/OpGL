using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace V7
{
    public class WarpToken : Sprite
    {
        public enum FlipSettings { KeepFlip, ReverseFlip, Flip, Unflip }
        public FlipSettings Settings;
        public float OutX;
        public float OutY;
        public int OutRoomX;
        public int OutRoomY;
        public WarpTokenOutput OutputSprite;
        private Game game;
        public static SoundEffect WarpSound;

        public struct WarpData
        {
            public Point InRoom;
            public Point OutRoom;
            public PointF In;
            public PointF Out;
            public WarpData(WarpToken warp, int roomX, int roomY)
            {
                InRoom = new Point(roomX, roomY);
                OutRoom = new Point(warp.OutRoomX, warp.OutRoomY);
                In = new PointF(warp.X, warp.Y);
                Out = new PointF(warp.OutX, warp.OutY);
            }
            public WarpData(JToken warp, int roomX, int roomY)
            {
                InRoom = new Point(roomX, roomY);
                OutRoom = new Point((int)(warp["OutRoomX"] ?? 0), (int)(warp["OutRoomY"] ?? 0));
                In = new PointF((float)(warp["X"] ?? 0f), (float)(warp["Y"] ?? 0f));
                Out = new PointF((float)(warp["OutX"] ?? 0f), (float)(warp["OutY"] ?? 0f));
            }
        }

        public WarpToken(float x, float y, Texture texture, Animation animation, float outX, float outY, int roomX, int roomY, Game owner, FlipSettings flip = FlipSettings.Unflip) : base(x, y, texture, animation)
        {
            Settings = flip;
            OutX = outX;
            OutY = outY;
            OutRoomX = roomX;
            OutRoomY = roomY;
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
            if (crewman.IsPlayer)
                game.LoadRoom(OutRoomX, OutRoomY);
            game.Flash(10);
            game.Shake(40);
            WarpSound?.Play();
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "WarpToken");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Animation", Animation.Name);
        //    ret.Add("OutX", OutX);
        //    ret.Add("OutY", OutY);
        //    ret.Add("Flip", (int)Settings);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("OutX", new SpriteProperty("OutX", () => OutX, (t, g) => OutX = (float)t, 0f, SpriteProperty.Types.Float, "The X position the warp token warps to.", false));
                ret.Add("OutY", new SpriteProperty("OutY", () => OutY, (t, g) => OutY = (float)t, 0f, SpriteProperty.Types.Float, "The Y position the warp token warps to.", false));
                ret.Add("OutRoomX", new SpriteProperty("OutRoomX", () => OutRoomX, (t, g) => OutRoomX = (int)t, 0f, SpriteProperty.Types.Float, "The room X the warp token warps to.", false));
                ret.Add("OutRoomY", new SpriteProperty("OutRoomY", () => OutRoomY, (t, g) => OutRoomY = (int)t, 0f, SpriteProperty.Types.Int, "The room Y the warp token warps to.", false));
                ret.Add("Flip", new SpriteProperty("Flip", () => (int)Settings, (t, g) => Settings = (FlipSettings)(int)t, 3, SpriteProperty.Types.Int, "0 = Keep flip, 1 = Reverse flip, 2 = Flipped, 3 = Unflipped."));
                ret["Type"].GetValue = () => "WarpToken";
                return ret;
            }
        }
    }
}
