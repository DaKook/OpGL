using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace OpGL
{
    public class TestGame
    {
        GameWindow gameWindow;
        public TestGame(GameWindow window)
        {
            gameWindow = window;
            window.RenderFrame += Window_RenderFrame;
        }

        private void Window_RenderFrame(object sender, FrameEventArgs e)
        {
            
        }
    }
}
