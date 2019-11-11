using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenGL;

namespace OpGL
{
    public class Sprite
    {
        protected bool flipX;
        protected bool flipY;
        protected Platform onPlatform;

        public string Name { get; set; } = "";

        public Color Color { get; set; } = Color.White;

        public Animation Animation { get; set; } = Animation.EmptyAnimation;
        private int _animFrame;
        private Point _old = new Point(0, 0);
        protected Point animationOffset = new Point(0, 0);

        public int Layer = 0;
        private int animFrame
        {
            get => _animFrame;
            set
            {
                Point n = Animation.GetFrame(_animFrame = value);
                n.X += animationOffset.X;
                n.Y += animationOffset.Y;
                if (n != _old)
                {
                    TexMatrix.Translate((n.X - _old.X), (n.Y - _old.Y), 0f);
                    _old = n;
                }
            }
        }
        public float X { get; set; }
        public float Y { get; set; }
        public List<PointF> Offsets = new List<PointF>();
        public bool MultiplePositions;
        public float Right { get => X + Width; set => X = value - Width; }
        public float Bottom { get => Y + Height; set => Y = value - Height; }
        public float CenterX { get => X + Width / 2; set => X = value - Width / 2; }
        public float CenterY { get => Y + Height / 2; set => Y = value - Height / 2; }
        public float PreviousX { get; set; }
        public float PreviousY { get; set; }
        public virtual float Width { get => Animation.Hitbox.Width; }
        public virtual float Height { get => Animation.Hitbox.Height; }

        public virtual bool IsCrewman { get => false; }

        public int TextureX
        {
            get
            {
                return Animation.GetFrame(animFrame).X;
            }
        }
        public int TextureY
        {
            get
            {
                return Animation.GetFrame(animFrame).Y;
            }
        }
        public virtual uint TextureID { get => Texture.ID; }

        public bool Visible { get; set; } = true;
        public enum SolidState {
            /// <summary>
            /// Solid for anything except NonSolid.
            /// </summary>
            Ground = 0,
            /// <summary>
            /// Goes through other entities, but is stopped by Ground.
            /// </summary>
            Entity = 1,
            /// <summary>
            /// Goes through everything. Typically will be static.
            /// </summary>
            NonSolid = 2 }
        /// <summary>
        /// Gets or sets the solid state of the Drawable.
        /// </summary>
        public SolidState Solid { get; set; } = SolidState.Entity;
        /// <summary>
        /// Gets or sets the magnitude of gravity to be applied to the Drawable. Negative values will cause it to fall up.
        /// </summary>
        public float Gravity { get; set; }
        /// <summary>
        /// When set to true, the Drawable will not have its own collision detection. Other Drawables will still test for collision with Static Drawables.
        /// </summary>
        public bool Static { get; set; }

        public bool Immovable { get; set; }
        /// <summary>
        /// Determines whether the Drawable will process even while out of the screen. Too many objects that always process could slow down the game.
        /// </summary>
        //public bool AlwaysProcess { get; set; } = false;
        public bool KillCrewmen { get; set; } = false;
        public Texture Texture { get; protected set; }
        public virtual uint VAO { get => Texture.baseVAO; set { } }

        public bool IsWarpingV;
        public bool IsWarpingH;

        public Matrix4x4f LocMatrix;
        public Matrix4x4f TexMatrix;

        public Sprite(float x, float y, Texture texture, int texX, int texY, Color? color = null)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;

            Texture = texture;
            TexMatrix = Matrix4x4f.Scaled(texture.TileSizeX / texture.Width, texture.TileSizeY / texture.Height, 1f);
            TexMatrix.Translate(texX, texY, 0f);

            Animation = new Animation(new Point[] { new Point(texX, texY) }, new Rectangle(0, 0, texture.TileSizeX, texture.TileSizeY), texture);
            _old = Animation.GetFrame(0);
            Color = color ?? Color.White;
        }

        public Sprite(float x, float y, Texture texture, Animation animation)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;

            Texture = texture;
            if (texture != null)
                TexMatrix = Matrix4x4f.Scaled(texture.TileSizeX / texture.Width, texture.TileSizeY / texture.Height, 1f);

            Animation = animation;
            if (animation != null)
            {
                _old = Animation.GetFrame(0);
                Point p = Animation.GetFrame(animFrame);
                TexMatrix.Translate(p.X, p.Y, 0f);
            }
        }

        public bool Within(float x, float y, float width, float height, float offsetX = 0, float offsetY = 0)
        {
            return Right + offsetX > x && X + offsetX < x + width
                && Bottom + offsetY > y && Y + offsetY < y + height;
        }
        public bool IsOverlapping(Sprite other)
        {
            bool ret = Within(other.X, other.Y, other.Width, other.Height);
            if (!ret && MultiplePositions)
            {
                foreach (PointF offset in Offsets)
                {
                    if (ret = Right + offset.X > other.X && X + offset.X < other.Right
                    && Bottom + offset.Y > other.Y && Y + offset.Y < other.Bottom) break;
                }
            }
            if (!ret && other.MultiplePositions)
            {
                foreach (PointF offsetO in other.Offsets)
                {
                    ret = Within(other.X + offsetO.X, other.Y + offsetO.Y, other.Width, other.Height);
                    if (!ret && MultiplePositions)
                    {
                        foreach (PointF offset in Offsets)
                        {
                            if (ret = Right + offset.X > other.X + offsetO.X && X + offset.X < other.Right + offsetO.X
                            && Bottom + offset.Y > other.Y + offsetO.Y && Y + offset.Y < other.Bottom + offsetO.Y) break;
                        }
                        if (ret) break;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Performs OpenGL bindings and uniform gets/updates before drawing.
        /// </summary>
        public virtual void SafeDraw()
        {
            if (!Visible) return;
            Gl.BindTexture(TextureTarget.Texture2d, Texture.ID);
            Gl.BindVertexArray(VAO);

            int modelLoc = Texture.Program.ModelLocation;
            Gl.UniformMatrix4f(modelLoc, 1, false, LocMatrix);
            int texLoc = Texture.Program.TexLocation;
            Gl.UniformMatrix4f(texLoc, 1, false, TexMatrix);
            int colorLoc = Texture.Program.ColorLocation;
            Gl.Uniform4f(colorLoc, 1, new Vertex4f((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));

            UnsafeDraw();
        }
        // update model matrix
        public virtual void RenderPrep()
        {
            LocMatrix = Matrix4x4f.Translated((int)X - Animation.Hitbox.X, (int)Y - Animation.Hitbox.Y, 0);
            if (flipX)
            {
                LocMatrix.Scale(-1, 1, 1);
                LocMatrix.Translate(-Animation.Hitbox.X * 2 - Width, 0, 0);
            }
            if (flipY)
            {
                LocMatrix.Scale(1, -1, 1);
                LocMatrix.Translate(0, -Animation.Hitbox.Y * 2 - Height, 0);
            }
        }
        // Just the render call; everything should be set up before calling this.
        public virtual void UnsafeDraw()
        {
            Gl.DrawArrays(PrimitiveType.Polygon, 0, 4);
            if (MultiplePositions)
            {
                PointF lastOffset = new PointF(0, 0);
                foreach (PointF offset in Offsets)
                {
                    LocMatrix.Translate((offset.X - lastOffset.X) * (flipX ? -1 : 1), (offset.Y - lastOffset.Y) * (flipY ? -1 : 1), 0);
                    Gl.UniformMatrix4f(Texture.Program.ModelLocation, 1, false, LocMatrix);
                    Gl.DrawArrays(PrimitiveType.Polygon, 0, 4);
                    lastOffset = offset;
                }
            }
        }

        public void ResetAnimation()
        {
            animFrame = 0;
        }

        public virtual bool FlipX { get => flipX; set => flipX = value; }
        public virtual bool FlipY { get => flipY; set => flipY = value; }

        public virtual void Process()
        {
            //Advance animation frame and change TextureX and TextureY accordingly
            if (animFrame + 1 >= Animation.FrameCount)
                animFrame = Animation.LoopStart;
            else
                animFrame += 1;

            PreviousX = X;
            PreviousY = Y;
        }

        public virtual void CollideY(float distance, Sprite collision)
        {
            Y -= distance;
        }
        public virtual void CollideX(float distance, Sprite collision)
        {
            X -= distance;
        }

        public virtual CollisionData TestCollision(Sprite testFor)
        {
            if (testFor == this || Immovable || Static) return null;

            CollisionData ret = null;
            if ((Solid == SolidState.Entity || Solid == SolidState.Ground) && testFor.Solid == SolidState.Ground || testFor is WarpLine)
            {
                if (IsOverlapping(testFor))
                    ret = GetCollisionData(testFor);
            }
            return ret;
        }
        protected CollisionData GetCollisionData(Sprite testFor)
        {
            for (int i = -1; i < Offsets.Count; i++)
            {
                float ofX = i > -1 ? Offsets[i].X : 0;
                float ofY = i > -1 ? Offsets[i].Y : 0;
                for (int j = -1; j < testFor.Offsets.Count; j++)
                {
                    float ofXO = j > -1 ? testFor.Offsets[j].X : 0;
                    float ofYO = j > -1 ? testFor.Offsets[j].Y : 0;
                    if (!testFor.Within(X + ofX, Y + ofY, Width, Height, ofXO, ofYO)) continue;
                    // check for vertical collision first
                    // top
                    if (PreviousY + Height + ofY <= testFor.PreviousY + ofYO)
                        return new CollisionData(true, Bottom + ofY - (testFor.Y + ofYO), testFor);
                    // bottom
                    else if (PreviousY + ofY >= testFor.PreviousY + testFor.Height + ofYO)
                        return new CollisionData(true, Y + ofY - (testFor.Bottom + ofYO), testFor);
                    // right
                    else if (PreviousX + Width + ofX <= testFor.PreviousX + ofXO)
                        return new CollisionData(false, Right + ofX - (testFor.X + ofXO), testFor);
                    // left
                    else if (PreviousX + ofX >= testFor.PreviousX + testFor.Width + ofXO)
                        return new CollisionData(false, X + ofX - (testFor.Right + ofXO), testFor);
                    else if (this is Crewman && testFor is ScriptBox)
                        return new CollisionData(true, 0, testFor);
                    if (!testFor.MultiplePositions) break;
                }
                if (!MultiplePositions) break;
            }

            return null;
        }

        /// <summary>
        /// Get the collision that should happen fist.
        /// That is the highest-distance horizontal collision; if none, the highest-distance vertical collision.
        /// </summary>
        public virtual CollisionData GetFirstCollision(List<CollisionData> collisions)
        {
            if (collisions.Count == 0)
                return null;

            CollisionData ret = collisions[0];
            for (int i = 1; i < collisions.Count; i++)
            {
                CollisionData dt = collisions[i];
                if (!dt.Vertical)
                {
                    if (ret.Vertical || Math.Abs(dt.Distance) > Math.Abs(ret.Distance))
                        ret = dt;
                }
                else if (ret.Vertical)
                {
                    if (Math.Abs(dt.Distance) > Math.Abs(ret.Distance))
                        ret = dt;
                }
            }
            return ret;
        }

        public virtual void Collide(CollisionData cd)
        {
            if (cd.Vertical)
            {
                CollideY(cd.Distance, cd.CollidedWith);
            }
            else
            {
                CollideX(cd.Distance, cd.CollidedWith);
            }
        }

        public virtual void HandleCrewmanCollision(Crewman crewman)
        {
            //Do nothing
        }

        public virtual JObject Save()
        {
            JObject ret = new JObject();
            ret.Add("Type", "Sprite");
            ret.Add("X", X);
            ret.Add("Y", Y);
            ret.Add("Texture", Texture.Name);
            return ret;
        }

        public static Sprite LoadSprite(JToken loadFrom, Game game)
        {
            string type = (string)loadFrom["Type"];
            //Type t = typeof(Sprite);
            Sprite s;
            float x = (float)loadFrom["X"];
            float y = (float)loadFrom["Y"];
            string textureName = (string)loadFrom["Texture"];
            Texture texture = game.TextureFromName(textureName ?? "");
            int? layer = loadFrom["Layer"] != null ? (int)loadFrom["Layer"] : (int?)null;
            if (type == "Tile")
            {
                int tileX = (int)loadFrom["TileX"];
                int tileY = (int)loadFrom["TileY"];
                string tag = (string)loadFrom["Tag"];
                s = new Tile((int)x, (int)y, texture, tileX, tileY) { Tag = tag };
            }
            else if (type == "Enemy")
            {
                string animationName = (string)loadFrom["Animation"];
                float xSpeed = (float)loadFrom["XSpeed"];
                float ySpeed = (float)loadFrom["YSpeed"];
                string name = (string)loadFrom["Name"];
                int color = (int)loadFrom["Color"];
                int boundX = (int)loadFrom["BoundsX"];
                int boundY = (int)loadFrom["BoundsY"];
                int boundW = (int)loadFrom["BoundsWidth"];
                int boundH = (int)loadFrom["BoundsHeight"];
                s = new Enemy(x, y, texture, texture.AnimationFromName(animationName), xSpeed, ySpeed, Color.FromArgb(color));
                s.Name = name;
                (s as Enemy).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
            }
            else if (type == "Crewman")
            {
                string standName = (string)loadFrom["Standing"];
                string walkName = (string)loadFrom["Walking"];
                string fallName = (string)loadFrom["Falling"];
                string jumpName = (string)loadFrom["Jumping"];
                string dieName = (string)loadFrom["Dying"];
                string name = (string)loadFrom["Name"];
                int textBoxColor = (int)loadFrom["TextBox"];
                bool sad = (bool)loadFrom["Sad"];
                float gravity = (float)loadFrom["Gravity"];
                bool flipX = (bool)loadFrom["FlipX"];
                string squeakName = (string)loadFrom["Squeak"];
                s = new Crewman(x, y, texture, name, texture.AnimationFromName(standName), texture.AnimationFromName(walkName), texture.AnimationFromName(fallName), texture.AnimationFromName(jumpName), texture.AnimationFromName(dieName), Color.FromArgb(textBoxColor));
                (s as Crewman).Sad = sad;
                (s as Crewman).Squeak = game.GetSound(squeakName);
                s.Gravity = gravity;
                s.FlipX = flipX;
            }
            else if (type == "Checkpoint")
            {
                string deactivatedName = (string)loadFrom["Deactivated"];
                string activatedName = (string)loadFrom["Activated"];
                bool flipX = (bool)loadFrom["FlipX"];
                bool flipY = (bool)loadFrom["FlipY"];
                s = new Checkpoint(x, y, texture, texture.AnimationFromName(deactivatedName), texture.AnimationFromName(activatedName), flipX, flipY);
            }
            else if (type == "Platform")
            {
                string animationName = (string)loadFrom["Animation"];
                string disappearName = (string)loadFrom["DisappearAnimation"];
                float xSpeed = (float)loadFrom["XSpeed"];
                float ySpeed = (float)loadFrom["YSpeed"];
                float conveyor = (float)loadFrom["Conveyor"];
                string name = (string)loadFrom["Name"];
                bool disappear = (bool)loadFrom["Disappear"];
                int color = (int)loadFrom["Color"];
                int boundX = (int)loadFrom["BoundsX"];
                int boundY = (int)loadFrom["BoundsY"];
                int boundW = (int)loadFrom["BoundsWidth"];
                int boundH = (int)loadFrom["BoundsHeight"];
                s = new Platform(x, y, texture, texture.AnimationFromName(animationName), xSpeed, ySpeed, conveyor, disappear, texture.AnimationFromName(disappearName));
                s.Name = name;
                s.Color = Color.FromArgb(color);
                (s as Platform).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
            }
            else if (type == "Terminal")
            {
                string deactivatedName = (string)loadFrom["Deactivated"];
                string activatedName = (string)loadFrom["Activated"];
                string script = (string)loadFrom["Script"];
                bool repeat = (bool)loadFrom["Repeat"];
                bool flipX = (bool)loadFrom["FlipX"];
                bool flipY = (bool)loadFrom["FlipY"];
                s = new Terminal(x, y, texture, texture.AnimationFromName(deactivatedName), texture.AnimationFromName(activatedName), game.ScriptFromName(script), repeat);
                s.FlipX = flipX;
                s.FlipY = flipY;
            }
            else if (type == "GravityLine")
            {
                int length = (int)loadFrom["Length"];
                bool horizontal = (bool)loadFrom["Horizontal"];
                string animationName = (string)loadFrom["Animation"];
                float xSpeed = (float)loadFrom["XSpeed"];
                float ySpeed = (float)loadFrom["YSpeed"];
                int boundX = (int)loadFrom["BoundsX"];
                int boundY = (int)loadFrom["BoundsY"];
                int boundW = (int)loadFrom["BoundsWidth"];
                int boundH = (int)loadFrom["BoundsHeight"];
                s = new GravityLine(x, y, texture, texture.AnimationFromName(animationName), horizontal, length);
                (s as GravityLine).XSpeed = xSpeed;
                (s as GravityLine).YSpeed = ySpeed;
                (s as GravityLine).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
            }
            else if (type == "WarpLine")
            {
                int length = (int)loadFrom["Length"];
                bool horizontal = (bool)loadFrom["Horizontal"];
                float offX = (float)loadFrom["OffsetX"];
                float offY = (float)loadFrom["OffsetY"];
                s = new WarpLine(x, y, length, horizontal, offX, offY);
            }
            else if (type == "WarpToken")
            {
                Animation animation = texture.AnimationFromName((string)loadFrom["Animation"]);
                float outX = (float)loadFrom["OutX"];
                float outY = (float)loadFrom["OutY"];
                int settings = (int)loadFrom["Flip"];
                s = new WarpToken(x, y, texture, animation, outX, outY, game, (WarpToken.FlipSettings)settings);
            }
            else if (type == "ScriptBox")
            {
                int width = (int)loadFrom["Width"];
                int height = (int)loadFrom["Height"];
                string script = (string)loadFrom["Script"];
                s = new ScriptBox(x, y, texture, width, height, game.ScriptFromName(script), game);
            }

            else s = null;
            if (s != null && layer != null)
                s.Layer = layer.Value;

            return s;
        }
    }

}
