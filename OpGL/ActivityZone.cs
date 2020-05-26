using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class ActivityZone : RectangleSprite, IActivityZone
    {
        public Sprite Sprite { get; set; }
        public float OffsetX;
        public float OffsetY;
        public VTextBox TextBox { get; set; }
        public Script Script { get; set; }
        public Script EnterScript;
        public Script ExitScript;
        public bool IsOverPlayer = false;
        private bool _activated;
        public bool Activated { get => _activated; set
            {
                _activated = value;
                IsOverPlayer = false;
            }
        }
        public Game Owner;

        public ActivityZone(Sprite sprite, float offsetX, float offsetY, float width, float height, Script script, Game owner, VTextBox textBox) : base(sprite.CenterX - width / 2 + offsetX, sprite.CenterY - height / 2 + offsetY, width, height)
        {
            Sprite = sprite;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Script = script;
            Visible = false;
            Owner = owner;
            TextBox = textBox;
        }

        public static ActivityZone Load(JToken loadFrom, Sprite sprite, Game game)
        {
            Script s = game.ScriptFromName((string)loadFrom["Script"] ?? "");
            Script n = game.ScriptFromName((string)loadFrom["Enter"] ?? "");
            Script x = game.ScriptFromName((string)loadFrom["Exit"] ?? "");
            float w = (float)(loadFrom["Width"] ?? 100f);
            float h = (float)(loadFrom["Height"] ?? 100f);
            float xo = (float)(loadFrom["OffsetX"] ?? 0f);
            float yo = (float)(loadFrom["OffsetY"] ?? 0f);
            int c = (int)(loadFrom["Color"] ?? System.Drawing.Color.Gray.ToArgb());
            string t = (string)loadFrom["Text"] ?? "  Press ENTER to explode  ";
            VTextBox tb = new VTextBox(0, 4, game.FontTexture, t, System.Drawing.Color.FromArgb(c));
            tb.CenterX = Game.RESOLUTION_WIDTH / 2;
            ActivityZone ret = new ActivityZone(sprite, xo, yo, w, h, s, game, tb);
            ret.EnterScript = n;
            ret.ExitScript = x;
            return ret;
        }

        public override void Process()
        {
            CenterX = Sprite.CenterX + OffsetX;
            CenterY = Sprite.CenterY + OffsetY;
            if (IsOverlapping(Owner.ActivePlayer) && !IsOverPlayer && !Activated)
            {
                IsOverPlayer = true;
                Owner.ExecuteScript(EnterScript, Sprite, Owner.ActivePlayer);
                Owner.SetActivityZone(this);
                TextBox.Appear();
                if (!Owner.hudSprites.Contains(TextBox))
                    Owner.hudSprites.Add(TextBox);
            }
            else if (!IsOverlapping(Owner.ActivePlayer) && IsOverPlayer)
            {
                IsOverPlayer = false;
                Owner.ExecuteScript(ExitScript, Sprite, Owner.ActivePlayer);
                TextBox.Disappear();
                Owner.SetActivityZone(null);
            }
        }
    }
}
