using Newtonsoft.Json.Linq;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public abstract class EditorTool
    {
        public LevelEditor Parent;
        public Game Owner => Parent.Owner;
        public string Prompt;
        public bool PromptImportant;
        public Texture PreviewTexture;
        public Point PreviewPoint;
        protected bool isLeftDown;
        protected bool wasLeftDown;
        protected bool isRightDown;
        protected bool wasRightDown;
        protected bool isMiddleDown;
        protected bool wasMiddleDown;
        public abstract string DefaultName { get; }
        public string Name;
        public abstract string DefaultDescription { get; }
        public string Description;
        public abstract string DefaultKey { get; }
        public string Key;
        public abstract Keys DefaultKeybind { get; }
        public Keys Keybind;
        protected bool left => isLeftDown && !wasLeftDown;
        protected bool right => isRightDown && !wasRightDown;
        protected bool middle => isMiddleDown && !wasMiddleDown;

        protected Point mouse => new Point(Parent.MouseX, Parent.MouseY);
        protected bool key(Keys k) => Parent.Owner.IsKeyHeld(k);
        protected bool shift => key(Keys.LeftShift) || key(Keys.RightShift);
        protected bool ctrl => key(Keys.LeftControl) || key(Keys.RightControl);
        protected bool alt => key(Keys.LeftAlt) || key(Keys.RightAlt);
        public bool TakeInput { get; protected set; }

        public SpriteCollection Sprites;

        protected Point centerOn(Point point, Size size)
        {
            return new Point((int)Math.Round((point.X - size.Width / 2f) / 8f) * 8, (int)Math.Round((point.Y - size.Height / 2f) / 8f) * 8);
        }

        public EditorTool(LevelEditor parent)
        {
            Parent = parent;
            Name = DefaultName;
            Description = DefaultDescription;
            Key = DefaultKey;
            Keybind = DefaultKeybind;
        }

        public Point Position => position;
        public Size Size => size;
        public Color Color => color;
        // Position and Size
        protected Point position;
        protected Size size;
        protected Color color;

        public float CenterX => position.X + size.Width * 4f;
        public float CenterY => position.Y + size.Height * 4f;
        public float Right => position.X + size.Width * 8f;
        public float Bottom => position.Y + size.Height * 8f;

        public float CameraX => Owner.CameraX;
        public float CameraY => Owner.CameraY;

        public virtual void Process()
        {
            wasLeftDown = isLeftDown;
            isLeftDown = Parent.LeftMouse;
            wasRightDown = isRightDown;
            isRightDown = Parent.RightMouse;
            wasMiddleDown = isMiddleDown;
            isMiddleDown = Parent.MiddleMouse;
        }
        public virtual void HandleKey(PassedKeyEvent e)
        {

        }
        public abstract JObject Save();
        public static EditorTool Load(JObject loadFrom, LevelEditor parent)
        {
            return null;
        }

        protected JObject getBaseSave(string type)
        {
            JObject ret = new JObject();
            ret.Add("Name", Name);
            ret.Add("Type", type);
            return ret;
        }
    }
}
