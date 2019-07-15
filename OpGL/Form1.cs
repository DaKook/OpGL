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
        uint va;
        uint texa;
        //uint prg;
        uint texPrg;
        int texSpritesWidth = 0;
        int texSpritesHeight = 0;
        bool init = false;
        bool playing = false;
        List<Texture> Textures = new List<Texture>();
        List<Drawable> tiles = new List<Drawable>();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!playing)
            {
                if (e.KeyCode == Keys.Space)
                {
                    if (!init)
                    {
                        LoadPrograms();
                        //InitColorQuad();
                        InitTexQuad();

                        Gl.Enable(EnableCap.Blend);
                        // opaque pixels being drawn over with a partially transparent pixels will result in a partially transparent pixel
                        // because the same function applied to each color value is also applied to the alpha value
                        // look at BlendFuncSeparate if that matters

                        //Gl.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.One, BlendingFactor.Zero);
                        Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                        Gl.Viewport(0, 0, glControl.Width, glControl.Height);
                        // need to tell GL which color to clear with
                        Gl.ClearColor(0f, 0f, 0f, 1f);

                        //tiles.Add(new Drawable(10f, 100f, Textures[1], 3f, 4f));
                        //tiles.Add(new Drawable(18f, 100f, Textures[1], 5f, 4f));
                        //tiles.Add(new Drawable(10f, 108f, Textures[1], 3f, 6f));
                        //tiles.Add(new Drawable(18f, 108f, Textures[1], 5f, 6f));
                        //tiles.Add(new Drawable(6f, 77f, Textures[0], 0f, 0f));
                        tiles.Add(new Drawable(8, 8, Textures[2], Textures[2].Animations[0]));

                        updatePreview();

                        init = true;
                    }
                    glControl.Invalidate();
                }
                else if (e.KeyCode == Keys.W)
                {
                    currentY -= 1;
                    updatePreview();
                }
                else if (e.KeyCode == Keys.A)
                {
                    currentX -= 1;
                    updatePreview();
                }
                else if (e.KeyCode == Keys.S)
                {
                    currentY += 1;
                    updatePreview();
                }
                else if (e.KeyCode == Keys.D)
                {
                    currentX += 1;
                    updatePreview();
                }
                else if (e.KeyCode == Keys.Q)
                {
                    currentTexture -= 1;
                    if (currentTexture < 0)
                        currentTexture = Textures.Count - 1;
                    updatePreview();
                }
                else if (e.KeyCode == Keys.E)
                {
                    currentTexture += 1;
                    if (currentTexture > Textures.Count - 1)
                        currentTexture = 0;
                    updatePreview();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    playing = true;
                    startFrameCount();
                }
            }
            else
            {
                if (e.KeyCode == Keys.Escape)
                {
                    playing = false;
                    Text = "";
                }
            }
        }

        private void updatePreview()
        {
            int px;
            int py;
            if (previewTile == null)
                px = py = 0;
            else
            {
                px = (int)previewTile.X;
                py = (int)previewTile.Y;
            }
            previewTile = new Drawable(px, py, Textures[currentTexture], currentX, currentY);
            glControl.Invalidate();
        }

        private void startFrameCount()
        {
            int sec = DateTime.Now.Second;
            int x = 1000;
            int fps = 0;
            System.Diagnostics.Stopwatch stp = new System.Diagnostics.Stopwatch();
            stp.Start();
            while (playing)
            {
                while (stp.ElapsedTicks < System.Diagnostics.Stopwatch.Frequency / 60)
                {
                    Application.DoEvents();
                }
                stp.Restart();
                fps += 1;
                for (int i = 0; i < tiles.Count; i++)
                {
                    tiles[i].Process();
                }
                glControl.Invalidate();
                if (sec != DateTime.Now.Second)
                {
                    Text = "Objects: " + x + ", FPS: " + fps;
                    sec = DateTime.Now.Second;
                    fps = 0;
                }
            }
        }

        private void LoadPrograms()
        {
            //prg = GLProgram.Load("shaders/v2dColor.txt", "shaders/f2dColor.txt");
            texPrg = GLProgram.Load("shaders/v2dTexTransform.txt", "shaders/f2dTex.txt");

            Gl.UseProgram(texPrg);
            int modelMatrixLoc = Gl.GetUniformLocation(texPrg, "model");
            Gl.UniformMatrix4f(modelMatrixLoc, 1, false, Matrix4x4f.Identity);
            int viewMatrixLoc = Gl.GetUniformLocation(texPrg, "view");
            Matrix4x4f camera = Matrix4x4f.Identity;
            camera.Translate(-1f, 1f, 0f);
            camera.Scale(4f / glControl.Width, -4f / glControl.Height, 1);
            Gl.UniformMatrix4f(viewMatrixLoc, 1, false, camera);
        }
        private void InitColorQuad()
        {
            va = Gl.CreateVertexArray();
            Gl.BindVertexArray(va);
            uint vb = Gl.CreateBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vb);
            float[] fls = new float[]
            {
                    -0.2f, -0.2f, 0, 0, 0, 1,
                    0.2f, -0.2f, 0, 0, 0, 1,
                    0.2f, 0.2f, 1, 1, 1, 1,
                    -0.2f, 0.2f, 1, 1, 1, 1
            };
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)fls.Length * sizeof(float), fls, BufferUsage.StaticDraw);

            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 6 * sizeof(float), (IntPtr)0);
            Gl.VertexAttribPointer(1, 4, VertexAttribType.Float, false, 6 * sizeof(float), (IntPtr)(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
        }
        private void InitTexQuad()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("textures/").ToList();
            foreach (string file in files)
            {
                if (file.EndsWith(".png"))
                {
                    string fl = file.Split('/').Last();
                    fl = fl.Substring(0, fl.Length - 4);

                    List<Animation> anims = new List<Animation>();
                    if (System.IO.File.Exists("textures/" + fl + "_data.txt"))
                    {
                        JObject jObject = JObject.Parse(System.IO.File.ReadAllText("textures/" + fl + "_data.txt"));
                        InitTex(fl, (int)jObject["GridSize"], ref Textures);

                        JArray arr = (JArray)jObject["Animations"];
                        if (arr != null)
                        {
                            foreach (var anim in arr)
                            {
                                JArray frms = (JArray)anim["Frames"];
                                List<Point> frames = new List<Point>(frms.Count);
                                int speed = (int)anim["Speed"];
                                // Animations are specified as X, Y tile coordinates.
                                // Or a single negative value indicating re-use previous
                                int i = 0;
                                while (i < frms.Count)
                                {
                                    Point f = new Point();
                                    int x = (int)frms[i];
                                    if (x >= 0)
                                    {
                                        i++;
                                        f = new Point(x, (int)frms[i]);
                                    }
                                    for (int j = 0; j < speed; j++)
                                        frames.Add(f);

                                    i++;
                                }
                                JArray hitbox = (JArray)anim["Hitbox"];
                                Rectangle r = hitbox.Count == 4 ? new Rectangle((int)hitbox[0], (int)hitbox[1], (int)hitbox[2], (int)hitbox[3]) : Rectangle.Empty;
                                anims.Add(new Animation(frames.ToArray(), r, Textures.Last()));
                            }
                        }

                        //string[] s = System.IO.File.ReadAllLines("textures/" + fl + "_data.txt");
                        //foreach (string set in s)
                        //{
                        //    if (set.StartsWith("GridSize:"))
                        //    {
                        //        int.TryParse(set.Substring(set.IndexOf(":") + 1), out gs);
                        //    }
                        //}
                    }
                    else // create with default grid size
                        InitTex(fl, 32, ref Textures);
                    Textures.Last().Animations = anims;
                }
            }
        }

        private void InitTex(string texture, int gridSize, ref List<Texture> textureIDs)
        {
            SKBitmap bmp = SKBitmap.Decode("textures/" + texture + ".png");
            texSpritesWidth = bmp.Width;
            texSpritesHeight = bmp.Height;

            texa = Gl.CreateVertexArray();
            Gl.BindVertexArray(texa);
            uint texb = Gl.CreateBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, texb);
            float[] fls = new float[]
            {
                0f,       0f,       0f,       0f,
                0f,       gridSize, 0f,       gridSize,
                gridSize, gridSize, gridSize, gridSize,
                gridSize, 0f,       gridSize, 0f
            };
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)fls.Length * sizeof(float), fls, BufferUsage.StaticDraw);

            //uint inst = Gl.CreateBuffer();
            //Gl.BindBuffer(BufferTarget.ArrayBuffer, inst);
            //Gl.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * (uint)tiles.Count * 16, fls, BufferUsage.StaticDraw);
            //Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
            //Gl.VertexAttribDivisor(0, 4);
            //Gl.VertexAttribDivisor(1, 4);

            textureIDs.Add(new Texture(Gl.CreateTexture(TextureTarget.Texture2d), bmp.Width, bmp.Height, gridSize, texture, texPrg, texa));
            Gl.BindTexture(TextureTarget.Texture2d, textureIDs.Last().ID);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmp.GetPixels());

            Gl.GenerateMipmap(TextureTarget.Texture2d);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, new float[] { 0f, 0, 0, 0 });
        }

        private void GlControl1_Render(object sender, GlControlEventArgs e)
        {
            if (init)
            {
                // clear the color buffer
                Gl.Clear(ClearBufferMask.ColorBufferBit);

                for (int i = 0; i < tiles.Count; i++)
                    tiles[i].Draw();
                previewTile.Draw();
            }
        }

        int currentTexture = 0;
        int currentX = 0;
        int currentY = 0;
        Drawable previewTile;

        private void GlControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (init)
            {
                int x = (int)((e.X - previewTile.Texture.TileSize / 2) / previewTile.Texture.TileSize);
                int y = (int)((e.Y - previewTile.Texture.TileSize / 2) / previewTile.Texture.TileSize);
                x /= 2;
                y /= 2;
                x *= (int)previewTile.Texture.TileSize;
                y *= (int)previewTile.Texture.TileSize;
                previewTile.X = x;
                previewTile.Y = y;
                previewTile.Visible = true;
                glControl.Invalidate();
            }
        }

        private void GlControl_MouseLeave(object sender, EventArgs e)
        {
            if (init)
            {
                previewTile.Visible = false;
                glControl.Invalidate();
            }
        }

        private void GlControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (init)
            {
                if (e.Button == MouseButtons.Left)
                    tiles.Add(new Drawable(previewTile.X, previewTile.Y, previewTile.Texture, previewTile.TextureX, previewTile.TextureY));
                else
                {
                    for (int i = 0; i < tiles.Count; i++)
                    {
                        Drawable item = tiles[i];
                        if (item.X == previewTile.X & item.Y == previewTile.Y)
                        {
                            tiles.Remove(item);
                            i -= 1;
                        }
                    }
                }
                glControl.Invalidate();
            }
        }
    }
}
