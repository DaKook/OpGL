using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpGL
{
    public class InstancedSprite : Sprite
    {
        public override void Dispose()
        {
            Gl.DeleteBuffers(ibo);
            Gl.DeleteVertexArrays(VAO);
        }

        public InstancedSprite(float x, float y, Texture texture) : base(x, y, texture, 0, 0)
        {

        }

        public InstancedSprite(float x, float y, Texture texture, Animation animation) : base(x, y, texture, animation)
        {

        }

        protected int instances = 0;

        protected float[] bufferData;
        protected uint ibo;
        protected bool updateBuffer = true;
        protected bool firstRender = true;
        private int updated = 0;

        public override uint VAO { get; set; }

        // Just the render call and any set-up StringDrawable requires but a regular Drawable doesn't.
        public override void UnsafeDraw()
        {
            if (updateBuffer || updated != Texture.Updated)
                UpdateBuffer();

            Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, instances);

            if (MultiplePositions)
            {
                PointF lastOffset = new PointF(0, 0);
                foreach (PointF offset in Offsets)
                {
                    LocMatrix.Translate((offset.X - lastOffset.X) * (flipX ? -1 : 1), (offset.Y - lastOffset.Y) * (flipY ? -1 : 1), 0);
                    Gl.UniformMatrix4f(Texture.Program.ModelLocation, 1, false, LocMatrix);
                    Gl.DrawArraysInstanced(PrimitiveType.Quads, 0, 4, instances);
                    lastOffset = offset;
                }
            }
        }

        protected void UpdateBuffer()
        {
            if (firstRender)
            {
                firstRender = false;

                VAO = Gl.CreateVertexArray();
                Gl.BindVertexArray(VAO);

                Gl.BindBuffer(BufferTarget.ArrayBuffer, Texture.baseVBO);
                Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
                Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);

                ibo = Gl.CreateBuffer();
                Gl.BindBuffer(BufferTarget.ArrayBuffer, ibo);
                Gl.VertexAttribPointer(2, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
                Gl.VertexAttribPointer(3, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                Gl.EnableVertexAttribArray(2);
                Gl.EnableVertexAttribArray(3);
                Gl.VertexAttribDivisor(2, 1);
                Gl.VertexAttribDivisor(3, 1);
            }
            updated = Texture.Updated;

            Gl.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)bufferData.Length * sizeof(float), bufferData, BufferUsage.StaticDraw);
            updateBuffer = false;
        }
    }
}
