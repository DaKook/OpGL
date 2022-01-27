using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace V7
{
    public class InstancedSprite : Sprite
    {
        public override void Dispose()
        {
            GL.DeleteBuffers(1, ref ibo);
            GL.DeleteVertexArrays(1, new int[] { VAO });
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

        public override int VAO { get; set; }

        // Just the render call and any set-up StringDrawable requires but a regular Drawable doesn't.
        public override void UnsafeDraw()
        {
            if (updateBuffer || updated != Texture.Updated)
                UpdateBuffer();

            GL.DrawArraysInstanced(PrimitiveType.TriangleFan, 0, 4, instances);

            if (MultiplePositions)
            {
                PointF lastOffset = new PointF(0, 0);
                foreach (PointF offset in Offsets)
                {
                    LocMatrix = Matrix4.CreateTranslation((offset.X - lastOffset.X) * (flipX ? -1 : 1), (offset.Y - lastOffset.Y) * (flipY ? -1 : 1), 0) * LocMatrix;
                    GL.UniformMatrix4(Texture.Program.ModelLocation, false, ref LocMatrix);
                    GL.DrawArraysInstanced(PrimitiveType.TriangleFan, 0, 4, instances);
                    lastOffset = offset;
                }
            }
        }

        protected void UpdateBuffer()
        {
            if (firstRender)
            {
                firstRender = false;

                int vao = GL.GenVertexArray();
                VAO = vao;
                GL.BindVertexArray(VAO);

                GL.BindBuffer(BufferTarget.ArrayBuffer, Texture.baseVBO);
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)0);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                GL.EnableVertexAttribArray(0);
                GL.EnableVertexAttribArray(1);

                GL.CreateBuffers(1, out ibo);
                GL.BindBuffer(BufferTarget.ArrayBuffer, ibo);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)0);
                GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
                GL.EnableVertexAttribArray(2);
                GL.EnableVertexAttribArray(3);
                GL.VertexAttribDivisor(2, 1);
                GL.VertexAttribDivisor(3, 1);
            }
            updated = Texture.Updated;

            GL.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ArrayBuffer, bufferData.Length * sizeof(float), bufferData, BufferUsageHint.StaticDraw);
            updateBuffer = false;
        }
    }
}
