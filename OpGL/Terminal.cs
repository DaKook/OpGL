using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class Terminal : Sprite, IActivityZone
    {
        public static VTextBox TextBox;
        public Animation DeactivatedAnimation;
        public Animation ActivatedAnimation;
        public Script Script { get; set; }
        Sprite IActivityZone.Sprite { get => this; set { } }
        VTextBox IActivityZone.TextBox { get => TextBox; set => TextBox = value; }
        public bool Repeat;
        public bool Activated { get; set; }
        public static SoundEffect ActivateSound;
        public Game Owner;
        public Terminal(float x, float y, Texture texture, Animation deactivated, Animation activated, Script script, bool repeat, Game owner) : base(x, y, texture, deactivated)
        {
            DeactivatedAnimation = deactivated;
            ActivatedAnimation = activated;
            Script = script;
            Repeat = repeat;
            Activated = false;
            Immovable = true;
            Owner = owner;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (Activated) return;
            if (Animation == DeactivatedAnimation)
            {
                Animation = ActivatedAnimation;
                ActivateSound?.Play();
            }
            Owner.SetActivityZone(this);
            TextBox.Appear();
            if (!Owner.hudSprites.Contains(TextBox))
                Owner.hudSprites.Add(TextBox);
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "Terminal");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Deactivated", DeactivatedAnimation.Name);
        //    ret.Add("Activated", ActivatedAnimation.Name);
        //    ret.Add("Script", Script.Name);
        //    ret.Add("Repeat", Repeat);
        //    ret.Add("FlipX", flipX);
        //    ret.Add("FlipY", flipY);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Add("Repeat", new SpriteProperty("Repeat", () => Repeat, (t, g) => Repeat = (bool)t, false, SpriteProperty.Types.Bool, "Whether the terminal can be used multiple times without reloading the room or not."));
                ret.Add("Script", new SpriteProperty("Script", () => Script?.Name ?? "", (t, g) => Script = g.ScriptFromName((string)t), "", SpriteProperty.Types.Script, "The script executed by this terminal."));
                ret.Add("ActivatedAnimation", new SpriteProperty("ActivatedAnimation", () => ActivatedAnimation.Name, (t, g) => ActivatedAnimation = Texture.AnimationFromName((string)t), "Activated", SpriteProperty.Types.Animation, "The animation displayed when activated."));
                ret["Animation"].DefaultValue = "Deactivated";
                ret["Animation"].SetValue = (t, g) => {
                    DeactivatedAnimation = Texture.AnimationFromName((string)t);
                    Animation = DeactivatedAnimation;
                };
                ret["Type"].GetValue = () => "Terminal";
                return ret;
            }
        }
    }
}
