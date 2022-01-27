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
        public Checkpoint(float x, float y, Texture texture, Animation deactivated, Animation activated = null, bool xFlip = false, bool yFlip = false) : base(x, y, texture, deactivated)
        {
            DeactevatedAnimation = deactivated;
            ActivatedAnimation = activated == null ? deactivated : activated;
            Animation = deactivated;
            Solid = SolidState.Entity;
            Immovable = true;
            flipX = xFlip;
            flipY = yFlip;
            Layer = -1;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (crewman.CurrentCheckpoint != this || crewman.PendingTrinkets.Count > 0)
            {
                if (crewman.CurrentCheckpoint != null)
                {
                    crewman.CurrentCheckpoint.Deactivate();
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
                Activate();
            }
        }

        public void Deactivate()
        {
            Activated = false;
            ResetAnimation();
            Animation = DeactevatedAnimation;
        }

        public void Activate(bool playSound = true)
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
                ret["Type"].GetValue = () => "Checkpoint";
                return ret;
            }
        }
    }
}
