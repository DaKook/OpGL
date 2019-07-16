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
using SkiaSharp;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    public partial class Form1 : Form
    {
        bool init = false;
        List<Texture> Textures = new List<Texture>();
        List<Drawable> tiles = new List<Drawable>();

        Game game;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!game.IsPlaying)
            {
                //if (e.KeyCode == Keys.W)
                //{
                //    currentY -= 1;
                //    updatePreview();
                //}
                //else if (e.KeyCode == Keys.A)
                //{
                //    currentX -= 1;
                //    updatePreview();
                //}
                //else if (e.KeyCode == Keys.S)
                //{
                //    currentY += 1;
                //    updatePreview();
                //}
                //else if (e.KeyCode == Keys.D)
                //{
                //    currentX += 1;
                //    updatePreview();
                //}
                //else if (e.KeyCode == Keys.Q)
                //{
                //    currentTexture -= 1;
                //    if (currentTexture < 0)
                //        currentTexture = Textures.Count - 1;
                //    updatePreview();
                //}
                //else if (e.KeyCode == Keys.E)
                //{
                //    currentTexture += 1;
                //    if (currentTexture > Textures.Count - 1)
                //        currentTexture = 0;
                //    updatePreview();
                //}
                //else
                if (e.KeyCode == Keys.Escape)
                {
                    game.StartGame();
                }
            }
            else
            {
                if (e.KeyCode == Keys.Escape)
                {
                    game.StopGame();
                }
            }
        }

        //private void updatePreview()
        //{
        //    int px;
        //    int py;
        //    if (previewTile == null)
        //        px = py = 0;
        //    else
        //    {
        //        px = (int)previewTile.X;
        //        py = (int)previewTile.Y;
        //    }
        //    previewTile = new Drawable(px, py, Textures[currentTexture], currentX, currentY);
        //    if (!game.IsPlaying)
        //        glControl.Invalidate();
        //}

        private void GlControl_MouseMove(object sender, MouseEventArgs e)
        {
            //if (init)
            //{
            //    int x = (int)((e.X - previewTile.Texture.TileSize / 2) / previewTile.Texture.TileSize);
            //    int y = (int)((e.Y - previewTile.Texture.TileSize / 2) / previewTile.Texture.TileSize);
            //    x /= 2;
            //    y /= 2;
            //    x *= (int)previewTile.Texture.TileSize;
            //    y *= (int)previewTile.Texture.TileSize;
            //    previewTile.X = x;
            //    previewTile.Y = y;
            //    previewTile.Visible = true;

            //    if (!game.IsPlaying)
            //        glControl.Invalidate();
            //}
        }

        private void GlControl_MouseLeave(object sender, EventArgs e)
        {
            //if (init)
            //{
            //    previewTile.Visible = false;
            //    if (!game.IsPlaying)
            //        glControl.Invalidate();
            //}
        }

        private void GlControl_MouseClick(object sender, MouseEventArgs e)
        {
            //if (init)
            //{
            //    if (e.Button == MouseButtons.Left)
            //        tiles.Add(new Drawable(previewTile.X, previewTile.Y, previewTile.Texture, previewTile.TextureX / previewTile.Texture.TileSize, previewTile.TextureY / previewTile.Texture.TileSize));
            //    else
            //    {
            //        for (int i = 0; i < tiles.Count; i++)
            //        {
            //            Drawable item = tiles[i];
            //            if (item.X == previewTile.X & item.Y == previewTile.Y)
            //            {
            //                tiles.Remove(item);
            //                i -= 1;
            //            }
            //        }
            //    }
            //    if (!game.IsPlaying)
            //        glControl.Invalidate();
            //}
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

            init = true;
        }
    }
}
