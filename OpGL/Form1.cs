using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenGL;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    public partial class Form1 : Form
    {
        Game game;

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
            game.StopGame();
        }

        // This timer is used in order to take code out of the Form's Load handler
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer1.Dispose();

            game = new Game(glControl);
            game.QuitGame += (sender2, e2) => { Application.Exit(); };
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
