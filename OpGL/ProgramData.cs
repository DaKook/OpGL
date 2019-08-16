using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace OpGL
{
    public class ProgramData
    {
        public uint ID;
        public int ModelLocation;
        public int TexLocation;
        public int ColorLocation;
        public int MasterColorLocation;

        public ProgramData(uint id)
        {
            ID = id;
            ModelLocation = Gl.GetUniformLocation(id, "model");
            TexLocation = Gl.GetUniformLocation(id, "texMatrix");
            ColorLocation = Gl.GetUniformLocation(id, "color");
            MasterColorLocation = Gl.GetUniformLocation(id, "masterColor");
        }
    }
}
