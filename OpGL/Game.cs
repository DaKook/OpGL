using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;

using OpenGL;
using Newtonsoft.Json.Linq;

namespace OpGL
{
    public class Game
    {
        const int RESOLUTION_WIDTH = 320;
        const int RESOLUTION_HEIGHT = 240;

        private GlControl glControl;
        private uint program;

        private Matrix4x4f camera, hudView;
        private List<Texture> textures;

        private List<Drawable> sprites;
        private List<Drawable> hudSprites;
        public bool IsPlaying { get; private set; } = false;

        public Game(GlControl control)
        {
            glControl = control;

            InitGlProgram();
            InitOpenGLSettings();

            textures = new List<Texture>();
            LoadTextures();

            sprites = new List<Drawable>();
            hudSprites = new List<Drawable>();
            // testing
            hudSprites.Add(new StringDrawable(8, 8, textures[0], "Welcome to VVVVVVV!" + Environment.NewLine + "You will enjoy...", Color.Red));

            glControl.Render += glControl_Render;
            glControl.Resize += glControl_Resize;
        }

        #region "Init"
        private void InitGlProgram()
        {
            program = GLProgram.Load("shaders/v2dTexTransform.txt", "shaders/f2dTex.txt");

            Gl.UseProgram(program);
            int modelMatrixLoc = Gl.GetUniformLocation(program, "model");
            Gl.UniformMatrix4f(modelMatrixLoc, 1, false, Matrix4x4f.Identity);

            // origin at top-left
            camera = Matrix4x4f.Translated(-1f, 1f, 0f); 
            camera.Scale(2f / RESOLUTION_WIDTH, -2f / RESOLUTION_HEIGHT, 1);
            hudView = camera;
            int viewMatrixLoc = Gl.GetUniformLocation(program, "view");
            Gl.UniformMatrix4f(viewMatrixLoc, 1, false, camera);
        }

        private void LoadTextures()
        {
            List<string> files = System.IO.Directory.EnumerateFiles("textures/").ToList();
            files.Sort();
            foreach (string file in files)
            {
                if (file.EndsWith(".png"))
                {
                    string fName = file.Split('/').Last();
                    fName = fName.Substring(0, fName.Length - 4);

                    if (System.IO.File.Exists("textures/" + fName + "_data.txt"))
                    {
                        JObject jObject = JObject.Parse(System.IO.File.ReadAllText("textures/" + fName + "_data.txt"));
                        Texture tex = CreateTexture(fName, (int)jObject["GridSize"]);
                        textures.Add(tex);

                        // Animations
                        JArray arr = (JArray)jObject["Animations"];
                        if (arr != null)
                        {
                            List<Animation> anims = new List<Animation>();
                            foreach (JObject anim in arr)
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
                                anims.Add(new Animation(frames.ToArray(), r, tex));
                            }
                            tex.Animations = anims;
                        }

                        // Tiles
                        JArray tls = (JArray)jObject["Tiles"];
                        if (arr != null)
                        {
                            Drawable.SolidState[,] states = new Drawable.SolidState[(int)(tex.Width / tex.TileSize), (int)(tex.Height / tex.TileSize)];
                            int i = 0;
                            int x = 0;
                            int y = 0;
                            while (i < tls.Count)
                            {
                                int count = (int)tls[i];
                                for (int j = 0; j < count; j++)
                                {
                                    states[x, y] = (Drawable.SolidState)(int)tls[i + 1];
                                    x += 1;
                                    if (x >= states.GetLength(0))
                                    {
                                        x = 0;
                                        y += 1;
                                    }
                                }
                                i += 2;
                            }
                            tex.TileSolidStates = states;
                        }
                    }
                    else // no _data file, create with default grid size
                    {
                        textures.Add(CreateTexture(fName, 32));
                        textures.Last().Animations = new List<Animation>();
                    }
                }
            }
        }
        private Texture CreateTexture(string texture, int gridSize)
        {
            SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.Decode("textures/" + texture + ".png");

            uint texa = Gl.CreateVertexArray();
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

            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)0);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);

            Texture ret = new Texture(Gl.CreateTexture(TextureTarget.Texture2d), bmp.Width, bmp.Height, gridSize, texture, program, texa);
            Gl.BindTexture(TextureTarget.Texture2d, ret.ID);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmp.GetPixels());

            Gl.GenerateMipmap(TextureTarget.Texture2d);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 0f });

            return ret;
        }

        private void InitOpenGLSettings()
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.OneMinusDstAlpha, BlendingFactor.One);

            glControl_Resize(null, null);

            Gl.ClearColor(0f, 0f, 0f, 1f);
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            float relX = (float)glControl.Width / RESOLUTION_WIDTH;
            float relY = (float)glControl.Height / RESOLUTION_HEIGHT;
            float scaleBy = Math.Min(relX, relY);
            int w = (int)(RESOLUTION_WIDTH * scaleBy);
            int h = (int)(RESOLUTION_HEIGHT * scaleBy);
            Gl.Viewport((glControl.Width - w) / 2, (glControl.Height - h) / 2, w, h);
        }
        #endregion

        private void GameLoop()
        {
            int sec = DateTime.Now.Second;
            int skippedFrames = 0;
            System.Diagnostics.Stopwatch stp = new System.Diagnostics.Stopwatch();
            long ticksPerFrame = System.Diagnostics.Stopwatch.Frequency / 60;
            long nextFrame = ticksPerFrame;
            stp.Start();

            while (IsPlaying)
            {
                while (stp.ElapsedTicks < nextFrame)
                {
                    int msToSleep = 15 - (int)stp.ElapsedMilliseconds;
                    if (msToSleep > 0)
                        Thread.Sleep(msToSleep);
                }
                long ticksElapsed = stp.ElapsedTicks - nextFrame;
                int frames = (int)(ticksPerFrame / ticksPerFrame);
                nextFrame += ticksPerFrame * frames;

                for (int i = 0; i < sprites.Count; i++)
                {
                    sprites[i].Process();
                }

                glControl.Invalidate();

                skippedFrames += frames - 1;
                if (sec != DateTime.Now.Second)
                {
                    //Text = "Objects: " + x + ", Skipped frames: " + skippedFrames;
                    sec = DateTime.Now.Second;
                    skippedFrames = 0;
                }
            }
        }
        public void StartGame()
        {
            if (!IsPlaying)
            {
                IsPlaying = true;
                Thread thread = new Thread(GameLoop);
                thread.Start();
            }
        }
        public void StopGame() { IsPlaying = false; }

        private void glControl_Render(object sender, GlControlEventArgs e)
        {
            // clear the color buffer
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            int viewMatrixLoc = Gl.GetUniformLocation(program, "view");
            Gl.UniformMatrix4f(viewMatrixLoc, 1, false, camera);
            for (int i = 0; i < sprites.Count; i++)
                sprites[i].Draw();
            Gl.UniformMatrix4f(viewMatrixLoc, 1, false, hudView);
            for (int i = 0; i < hudSprites.Count; i++)
                hudSprites[i].Draw();
        }

    }
}
