using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
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
            Solid = SolidState.Entity;
            Immovable = true;
            flipX = xFlip;
            flipY = yFlip;
            Layer = -1;
        }

        public override void HandleCrewmanCollision(Crewman crewman)
        {
            if (!Activated)
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
            if (!Activated)
            {
                if (playSound)
                    ActivateSound?.Play();
                Activated = true;
                ResetAnimation();
                Animation = ActivatedAnimation;
            }
        }

        public override JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "Checkpoint");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            ret.Add("Deactivated", DeactevatedAnimation.Name);
            ret.Add("Activated", ActivatedAnimation.Name);
            ret.Add("FlipX", flipX);
            ret.Add("FlipY", flipY);
            return ret;
        }
    }
}
