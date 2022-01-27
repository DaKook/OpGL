using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL;

namespace V7
{
    class GLProgram
    {
        private static string CheckCompileError(int shader)
        {
            string errorMsg;
            GL.GetShaderInfoLog(shader, 512, out int _, out errorMsg);
            if (errorMsg.Length == 0)
                return null;
            else
                return errorMsg;
        }
        private static string CheckLinkError(int program)
        {
            string errorMsg;
            GL.GetProgramInfoLog(program, 512, out int _, out errorMsg);
            if (errorMsg.Length == 0)
                return null;
            else
                return errorMsg;
        }

        // returns the ID of the loaded program, or uint.max if failed
        public static int Load(string pathToVertexShader, string pathToFragmentShader)
        {
            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, File.ReadAllText(pathToVertexShader));
            GL.CompileShader(vs);
            string errorMsg = CheckCompileError(vs);
            if (errorMsg != null)
            {
                Console.WriteLine("Vertex shader compile error: " + errorMsg);
                return int.MaxValue;
            }

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, File.ReadAllText(pathToFragmentShader));
            GL.CompileShader(fs);
            errorMsg = CheckCompileError(fs);
            if (errorMsg != null)
            {
                Console.WriteLine("Fragment shader compile error: " + errorMsg);
                return int.MaxValue;
            }

            int program = GL.CreateProgram();
            GL.AttachShader(program, vs);
            GL.AttachShader(program, fs);
            GL.LinkProgram(program);
            errorMsg = CheckLinkError(program);
            if (errorMsg != null)
            {
                Console.WriteLine("Program link error: " + errorMsg);
                return int.MaxValue;
            }

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
            return program;
        }
    }
}
