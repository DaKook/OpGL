using OpenTK;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace V7
{
    public abstract class SpritesLayer : IDisposable
    {
        public Game Owner { get; protected set; }
        public virtual bool UsesExtraHud => false;
        public virtual void DrawExtraHud(Matrix4 baseCamera, int viewMatrixLocation)
        {

        }

        public delegate void ExitDelegate(SpritesLayer sender);
        public event ExitDelegate Exit;

        public delegate void FinishDelegate(SpritesLayer sender);
        public event FinishDelegate Finish;

        public bool Finished { get; protected set; }

        /// <summary>
        /// Range is 0-1, 0 doesn't darken other layers, 1 hides other layers completely.
        /// </summary>
        public float Darken;

        /// <summary>
        /// When true, layers under this layer will not process.
        /// </summary>
        public bool FreezeBelow = false;

        public bool ProcessAnyway { get; protected set; }

        public bool YieldInput { get; protected set; }

        /// <summary>
        /// Renders the layer of sprites.
        /// </summary>
        /// <param name="baseCamera">The base camera matrix</param>
        /// <param name="viewMatrixLocation">The location of the view matrix in the program</param>
        public abstract void Render(Matrix4 baseCamera, int viewMatrixLocation);
        public abstract void Process();
        public abstract void HandleClick(MouseButtonEventArgs e);
        public abstract void HandleKey(PassedKeyEvent e, bool typing);
        public abstract void HandleWheel(int e);
        protected void FinishLayer()
        {
            Finished = true;
            Finish?.Invoke(this);
        }
        protected void ExitLayer()
        {
            Exit?.Invoke(this);
        }
        public abstract void Dispose();
    }
}
