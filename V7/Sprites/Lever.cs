using System;
using System.Collections.Generic;
using System.Text;

namespace V7
{
    public class Lever : Sprite, IActivityZone
    {
        public bool On;
        public DecimalVariable LoadCheck;
        public Script Script { get; set; }
        public Animation OnAnimation;
        public Animation OffAnimation;
        public Game Owner;
        public static VTextBox TextBox;
        private VTextBox ivTextBox;
        VTextBox IActivityZone.TextBox { get => ivTextBox ?? TextBox; set => ivTextBox = value; }
        Sprite IActivityZone.Sprite { get => this; set { } }
        public bool Activated { get; set; }

        public Lever(float x, float y, Texture texture, Animation offAnimation, Animation onAnimation, Script script, bool flipX, bool flipY, Game owner, bool on = false) : base(x, y, texture, offAnimation)
        {
            animFrame = Animation.LoopStart;
            OffAnimation = offAnimation;
            OnAnimation = onAnimation;
            Script = script;
            FlipX = flipX;
            FlipY = flipY;
            Owner = owner;
            On = on;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (Activated) return;
            Owner.SetActivityZone(this);
            TextBox.Appear();
            if (!Owner.hudSprites.Contains(TextBox))
                Owner.hudSprites.Add(TextBox);
        }

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("On", new SpriteProperty("On", () => On, (t, g) => On = (bool)t, false, SpriteProperty.Types.Bool, "Whether the lever is on or off."));
                ret.Add("Script", new SpriteProperty("Script", () => Script?.Name ?? "", (t, g) => Script = g.ScriptFromName((string)t), "", SpriteProperty.Types.Script, "The script executed by this lever."));
                ret.Add("OnAnimation", new SpriteProperty("OnAnimation", () => OnAnimation.Name, (t, g) => OnAnimation = Texture.AnimationFromName((string)t), "LeverOn", SpriteProperty.Types.Animation, "The animation displayed when turned on."));
                ret.Add("OffAnimation", new SpriteProperty("OffAnimation", () => OffAnimation.Name, (t, g) => OffAnimation = Texture.AnimationFromName((string)t), "LeverOff", SpriteProperty.Types.Animation, "The animation displayed when turned off."));
                ret.Add("LoadCheck", new SpriteProperty("LoadCheck", () => LoadCheck?.Name ?? "", (t, g) => LoadCheck = Command.GetNumber((string)t, Owner, new Script.Executor(Script.Empty, null) { Sender = this, Target = null }), "", SpriteProperty.Types.String, "The number to decide whether this lever is on when loading."));
                ret["Type"].GetValue = () => "Lever";
                return ret;
            }
        }
    }
}
