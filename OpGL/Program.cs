using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace V7
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            OpenTK.GameWindow gameWindow = new OpenTK.GameWindow();
            gameWindow.Title = "VVVVVVV";
            gameWindow.ClientSize = new System.Drawing.Size(320, 240);
            Game game = new Game(gameWindow);
            gameWindow.Closed += (sender, e) => { game.StopGame(); };
            game.StartGame();
            gameWindow.Run(60, 60);
        }
    }
}
