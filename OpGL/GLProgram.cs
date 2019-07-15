using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenGL;

namespace OpGL
{
    class GLProgram
    {
        private static string CheckCompileError(uint shader)
        {
            StringBuilder errorMsg = new StringBuilder(512);
            Gl.GetProgramInfoLog(shader, 512, out int len, errorMsg);
            if (errorMsg.Length == 0)
                return null;
            else
                return errorMsg.ToString();
        }
        private static string CheckLinkError(uint program)
        {
            StringBuilder errorMsg = new StringBuilder(512);
            Gl.GetProgramInfoLog(program, 512, out int len, errorMsg);
            if (errorMsg.Length == 0)
                return null;
            else
                return errorMsg.ToString();
        }

        // returns the ID of the loaded program, or uint.max if failed
        public static uint Load(string pathToVertexShader, string pathToFragmentShader)
        {
            uint vs = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vs, new string[] { File.ReadAllText(pathToVertexShader) });
            Gl.CompileShader(vs);
            string errorMsg = CheckCompileError(vs);
            if (errorMsg != null)
            {
                Console.WriteLine("Vertex shader compile error: " + errorMsg);
                return uint.MaxValue;
            }

            uint fs = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fs, new string[] { File.ReadAllText(pathToFragmentShader) });
            Gl.CompileShader(fs);
            errorMsg = CheckCompileError(fs);
            if (errorMsg != null)
            {
                Console.WriteLine("Fragment shader compile error: " + errorMsg);
                return uint.MaxValue;
            }

            uint program = Gl.CreateProgram();
            Gl.AttachShader(program, vs);
            Gl.AttachShader(program, fs);
            Gl.LinkProgram(program);
            errorMsg = CheckLinkError(program);
            if (errorMsg != null)
            {
                Console.WriteLine("Program link error: " + errorMsg);
                return uint.MaxValue;
            }

            Gl.DeleteShader(vs);
            Gl.DeleteShader(fs);
            return program;
        }
    }
}
