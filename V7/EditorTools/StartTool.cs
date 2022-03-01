using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class StartTool : EditorTool
    {
        public override string DefaultName => "Start Point";

        public override string DefaultDescription => "Set the starting point of the level.\n" +
            "Use LEFT-CLICK to set the start point, and RIGHT-CLICK to navigate to the starting room.\n" +
            "Hold Z to start flipped, and hold X to start facing left.";

        public override string DefaultKey => "P";

        public override Keys DefaultKeybind => Keys.P;

        public StartTool(LevelEditor parent) : base(parent)
        {

        }

        public override void Process()
        {
            base.Process();
            size = new Size((int)Math.Ceiling(Owner.ActivePlayer.Width / 8f), (int)Math.Ceiling(Owner.ActivePlayer.Height / 8));
            position = centerOn(mouse, size * 8);
            color = Color.Blue;
            if (left)
            {
                if (!Parent.Sprites.Contains(Parent.Player))
                {
                    Parent.Sprites.Add(Parent.Player);
                }
                Parent.Player.Visible = true;
                Parent.Player.CenterX = CenterX + CameraX;
                Parent.Player.Bottom = Bottom + CameraY;
                Parent.Player.FlipX = key(Keys.X);
                Parent.Player.FlipY = key(Keys.Z);
                if (key(Keys.Z)) Parent.Player.Gravity = -Math.Abs(Parent.Player.Gravity);
                else Parent.Player.Gravity = Math.Abs(Parent.Player.Gravity);
                Owner.StartX = (int)Parent.Player.X;
                Owner.StartY = (int)Parent.Player.Y;
                Owner.StartRoomX = Owner.CurrentRoom.X;
                Owner.StartRoomY = Owner.CurrentRoom.Y;
                //defaultPlayer = Player.Name;
            }
        }

        public override JObject Save()
        {
            throw new NotImplementedException();
        }
    }
}
