using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class Checkpoint : Sprite
    {
        public Animation ActivatedAnimation = null;
        public Animation DeactevatedAnimation = null;
        public bool Activated;
        public static SoundEffect ActivateSound;
        public List<Crewman> Crewmen;
        public Script ActivatedEvent;
        private SpriteProperty evActivate => new SpriteProperty("ActivateEvent", () => ActivatedEvent?.Name ?? "", (t, g) => ActivatedEvent = g.ScriptFromName((string)t), "", SpriteProperty.Types.Script, "The script to run when the checkpoint is activated.");
        Game game;
        public override bool AlwaysCollide => true;
        public Checkpoint(float x, float y, Game owner, Texture texture, Animation deactivated, Animation activated = null, bool xFlip = false, bool yFlip = false) : base(x, y, texture, deactivated)
        {
            DeactevatedAnimation = deactivated;
            ActivatedAnimation = activated == null ? deactivated : activated;
            Animation = deactivated;
            Solid = SolidState.Entity;
            Immovable = true;
            flipX = xFlip;
            flipY = yFlip;
            Layer = -1;
            ColorModifier = new AnimatedColor(new List<System.Drawing.Color>(), 0, 0, 0, 1, false);
            Crewmen = new List<Crewman>();
            game = owner;
        }

        public void RemoveCrewman(Crewman crewman)
        {
            int index = Crewmen.IndexOf(crewman);
            if (index > -1)
            {
                Crewmen.RemoveAt(index);
                ColorModifier.BaseColors.RemoveRange(index * 16, 16);
            }
            if (Crewmen.Count == 0)
                Deactivate();
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (crewman.CurrentCheckpoint != this || crewman.PendingTrinkets.Count > 0)
            {
                if (crewman.CurrentCheckpoint is object)
                {
                    crewman.CurrentCheckpoint.RemoveCrewman(crewman);
                }
                crewman.CurrentCheckpoint = this;
                crewman.CheckpointFlipX = flipX;
                crewman.CheckpointFlipY = flipY;
                crewman.CheckpointX = CenterX;
                crewman.CheckpointY = flipY ? Y : Bottom;
                if (crewman.PendingTrinkets.Count > 0)
                {
                    foreach (Trinket tr in crewman.PendingTrinkets)
                    {
                        crewman.HeldTrinkets.Add(tr.ID);
                    }
                    crewman.PendingTrinkets.Clear();
                }
                Activate(true, crewman);
                RegisterCrewman(crewman);
            }
        }

        public void RegisterCrewman(Crewman crewman)
        {
            Crewmen.Add(crewman);
            for (int i = 0; i < 8; i++)
            {
                ColorModifier.BaseColors.Add(crewman.TextBoxColor);
            }
            for (int i = 0; i < 8; i++)
            {
                ColorModifier.BaseColors.Add(System.Drawing.Color.White);
            }
        }

        public void Deactivate()
        {
            Activated = false;
            ResetAnimation();
            Animation = DeactevatedAnimation;
        }

        public void Activate(bool playSound = true, Crewman activator = null)
        {
            if (playSound)
                ActivateSound?.Play();
            if (!Activated)
            {
                Activated = true;
                ResetAnimation();
                Animation = ActivatedAnimation;
                if (!playSound)
                {
                    animFrame = Animation.LoopStart;
                }
                if (ActivatedEvent is object && activator is object)
                    game.ExecuteScript(ActivatedEvent, this, activator, new DecimalVariable[] { });
            }
        }

        //public override JObject Save()
        //{
        //    JObject ret = new JObject();
        //    ret.Add("Type", "Checkpoint");
        //    ret.Add("X", X);
        //    ret.Add("Y", Y);
        //    ret.Add("Texture", Texture.Name);
        //    ret.Add("Deactivated", DeactevatedAnimation.Name);
        //    ret.Add("Activated", ActivatedAnimation.Name);
        //    ret.Add("FlipX", flipX);
        //    ret.Add("FlipY", flipY);
        //    return ret;
        //}

        public override SortedList<string, SpriteProperty> Events
        {
            get
            {
                SortedList<string, SpriteProperty> ret = new SortedList<string, SpriteProperty>();
                ret.Add("Activate", evActivate);
                return ret;
            }
        }

        public override SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = base.Properties;
                ret.Remove("Solid");
                ret.Add("ActivatedAnimation", new SpriteProperty("ActivatedAnimation", () => ActivatedAnimation.Name, (t, g) => ActivatedAnimation = Texture.AnimationFromName((string)t), "Activated", SpriteProperty.Types.Animation, "The animation displayed when activated."));
                ret["Animation"].DefaultValue = "Deactivated";
                ret["Animation"].SetValue = (t, g) => {
                    DeactevatedAnimation = Texture.AnimationFromName((string)t);
                    Animation = DeactevatedAnimation;
                };
                ret.Add("ActivateEvent", evActivate);
                ret["Type"].GetValue = () => "Checkpoint";
                return ret;
            }
        }
    }
}
