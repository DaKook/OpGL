using System;
using System.Windows.Forms;
using OpenTK;

namespace V7
{
    public partial class Form1 : Form
    {
        //Game game;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void GlControl_MouseMove(object sender, MouseEventArgs e)
        {
            
        }

        private void GlControl_MouseLeave(object sender, EventArgs e)
        {
            
        }

        private void GlControl_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //game.StopGame();
        }

        // This timer is used in order to take code out of the Form's Load handler
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer1.Dispose();

            GameWindow gw = new GameWindow();
            gw.Title = "VVVVVVV";
            gw.Run(60, 60);

            //game.QuitGame += (sender2, e2) => { Application.Exit(); };
        }

        private void GlControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
