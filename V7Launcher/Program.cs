using System;
using V7;

namespace V7Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Opening V7...");
            Game game = new Game();
            game.Log += (m) => Console.WriteLine(m);
            game.StartGame();
            Music.Close();
            if (game.Exception is object)
            {
                Console.WriteLine("Exception thrown:");
                Console.WriteLine(game.Exception.Message);
                Console.WriteLine(game.Exception.StackTrace);
                Console.WriteLine("Press enter to close...");
                Console.ReadLine();
            }
        }
    }
}
