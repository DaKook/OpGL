using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class Checkpoint : Drawable
    {
        public Animation ActivatedAnimation = null;
        public Animation DeactevatedAnimation = null;
        public bool Activated;
        public Checkpoint(float x, float y, Texture texture, Animation deactivated, Animation activated = null, bool xFlip = false, bool yFlip = false) : base(x, y, texture, deactivated)
        {
            DeactevatedAnimation = deactivated;
            ActivatedAnimation = activated;
            Solid = SolidState.Entity;
            Immovable = true;
            flipX = xFlip;
            flipY = yFlip;
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
                crewman.CheckpointX = X + (Width - crewman.Width) / 2;
                crewman.CheckpointY = Y + (flipY ? 0 : (Height - crewman.Height));
                Activate();
            }
        }

        public void Deactivate()
        {
            Activated = false;
            ResetAnimation();
            Animation = DeactevatedAnimation;
        }

        public void Activate()
        {
            Activated = true;
            ResetAnimation();
            Animation = ActivatedAnimation;
        }
    }
}
