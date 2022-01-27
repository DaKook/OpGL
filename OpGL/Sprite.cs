using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace V7
{
    public class Sprite : IDisposable
    {
        public virtual void Dispose()
        {

        }

        protected virtual bool AlwaysCollide => false;
        private static Texture lastLoaded = null;
        protected bool flipX;
        protected bool flipY;
        protected IPlatform onPlatform;
        public virtual Pushability Pushability => Pushability.Pushable;
        public PushData FramePush;
        public string TemplateName;

        public string Name { get; set; } = "";

        public Color Color { get; set; } = Color.White;
        public AnimatedColor ColorModifier = null;
        public string Tag;

        public Animation Animation { get; set; } = Animation.EmptyAnimation;
        private int _animFrame;
        //private Point _old = new Point(0, 0);
        protected Point animationOffset = new Point(0, 0);

        public int Layer = 0;
        protected int animFrame
        {
            get => _animFrame;
            set
            {
                _animFrame = value;
                TexMatrix = Texture.BaseTexMatrix;
                Point p;
                if (Animation is object)
                    p = Animation.GetFrame(value);
                else
                    p = new Point(0, 0);
                TexMatrix = Matrix4.CreateTranslation(p.X + animationOffset.X, p.Y + animationOffset.Y, 0) * TexMatrix;
                TexMatrix = Matrix4.CreateScale(extent.X, extent.Y, 1) * TexMatrix;
            }
        }
        public double DX;
        public float X { get => (float)DX; set => DX = value; }
        public double DY;
        public float Y { get => (float)DY; set => DY = value; }
        private float _initX, _initY;
        public float InitialX
        {
            get => _initX;
            set
            {
                _initX = value;
                X = value;
            }
        }
        public float InitialY
        {
            get => _initY;
            set
            {
                _initY = value;
                Y = value;
            }
        }
        public List<PointF> Offsets = new List<PointF>();
        public bool MultiplePositions;
        public float Right { get => X + Width; set => X = value - Width; }
        public float Bottom { get => Y + Height; set => Y = value - Height; }
        public float CenterX { get => X + Width / 2; set => X = value - Width / 2; }
        public float CenterY { get => Y + Height / 2; set => Y = value - Height / 2; }
        public double PreviousX { get; set; }
        public double PreviousY { get; set; }
        public float PreviousWidth { get; set; }
        public float PreviousHeight { get; set; }
        public virtual float Width { get => (Animation is object ? Animation.Hitbox.Width : 0) * extent.X * Size; }
        public virtual float Height { get => (Animation is object ? Animation.Hitbox.Height : 0) * extent.Y * Size; }
        public bool Pushable = false;
        public bool IsOnPlatform => onPlatform is object;
        public Sprite Platform => onPlatform as Sprite;

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
        public virtual int TextureID { get => Texture.ID; }
        public SortedList<string, float> Tags = new SortedList<string, float>();

        public bool Visible { get; set; } = true;
        public ActivityZone ActivityZone;
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
        public virtual ProgramData Program => Texture?.Program;
        public virtual int VAO { get => Texture.baseVAO; set { } }
        public bool DidCollision = false;

        public bool IsWarpingV;
        public bool IsWarpingH;

        public Matrix4 LocMatrix;
        public Matrix4 TexMatrix;

        private PointF extent = new PointF(1, 1);
        public float Size = 1;

        public Sprite(float x, float y, Texture texture, int texX, int texY, Color? color = null)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;
            InitialX = x;
            InitialY = y;

            Texture = texture;
            TexMatrix = Matrix4.CreateScale(texture.TileSizeX / texture.Width, texture.TileSizeY / texture.Height, 1f);
            TexMatrix = Matrix4.CreateTranslation(texX, texY, 0f) * TexMatrix;

            Animation = new Animation(new Point[] { new Point(texX, texY) }, new Rectangle(0, 0, texture.TileSizeX, texture.TileSizeY), texture);
            if (Animation == null)
            {
                Animation = Animation.Static(0, 0, texture);
            }
            Color = color ?? Color.White;
        }
        protected Sprite()
        {

        }

        public virtual void ChangeTexture(Texture texture)
        {
            if (texture != null)
            {
                Texture = texture;
                TexMatrix = Matrix4.CreateScale(texture.TileSizeX / texture.Width, texture.TileSizeY / texture.Height, 1f);
            }
        }

        public void InitializePosition()
        {
            _initX = X;
            _initY = Y;
        }

        public void AttachToPlatform(IPlatform attachTo)
        {
            if (onPlatform is object)
                onPlatform.OnTop.Remove(this);
            onPlatform = attachTo;
            attachTo.OnTop.Add(this);
        }

        public Sprite(float x, float y, Texture texture, Animation animation)
        {
            X = x;
            Y = y;
            PreviousX = x;
            PreviousY = y;
            InitialX = x;
            InitialY = y;

            Texture = texture;
            if (texture != null)
                TexMatrix = Matrix4.CreateScale(texture.TileSizeX / texture.Width, texture.TileSizeY / texture.Height, 1f);

            Animation = animation;
            if (animation != null)
            {
                Point p = Animation.GetFrame(animFrame);
                TexMatrix = Matrix4.CreateTranslation(p.X, p.Y, 0f) * TexMatrix;
            }
        }

        public bool Within(double x, double y, float width, float height, double offsetX = 0, double offsetY = 0)
        {
            return Right + offsetX > x && X + offsetX < x + width
                && Bottom + offsetY > y && Y + offsetY < y + height;
        }

        public bool IsOverlapping(Sprite other)
        {
            bool over = Within(other.X, other.Y, other.Width, other.Height);
            if (!over && MultiplePositions)
            {
                foreach (PointF offset in Offsets)
                {
                    if (over = Right + offset.X > other.X && X + offset.X < other.Right
                    && Bottom + offset.Y > other.Y && Y + offset.Y < other.Bottom) break;
                }
            }
            if (!over && other.MultiplePositions)
            {
                foreach (PointF offsetO in other.Offsets)
                {
                    over = Within(other.X + offsetO.X, other.Y + offsetO.Y, other.Width, other.Height);
                    if (!over && MultiplePositions)
                    {
                        foreach (PointF offset in Offsets)
                        {
                            if (over = Right + offset.X > other.X + offsetO.X && X + offset.X < other.Right + offsetO.X
                            && Bottom + offset.Y > other.Y + offsetO.Y && Y + offset.Y < other.Bottom + offsetO.Y) break;
                        }
                        if (over) break;
                    }
                }
            }
            return over;
        }

        /// <summary>
        /// Performs OpenGL bindings and uniform gets/updates before drawing.
        /// </summary>
        public virtual void SafeDraw()
        {
            if (!Visible) return;
            GL.BindTexture(TextureTarget.Texture2D, Texture.ID);
            GL.BindVertexArray(VAO);

            int modelLoc = Texture.Program.ModelLocation;
            GL.UniformMatrix4(modelLoc, false, ref LocMatrix);
            int texLoc = Texture.Program.TexLocation;
            GL.UniformMatrix4(texLoc, false, ref TexMatrix);
            int colorLoc = Texture.Program.ColorLocation;
            GL.Uniform4(colorLoc, new Vector4((float)Color.R / 255, (float)Color.G / 255, (float)Color.B / 255, (float)Color.A / 255));

            UnsafeDraw();
        }
        // update model matrix
        public virtual void RenderPrep()
        {
            int x = 0;
            int y = 0;
            if (Animation != null)
            {
                x = (int)Math.Round(X - (Animation.Hitbox.X * Size));
                y = (int)Math.Round(Y - (Animation.Hitbox.Y * Size));
            }
            LocMatrix = Matrix4.CreateTranslation(x, y, 0);
            if (flipX)
            {
                LocMatrix = Matrix4.CreateScale(-1, 1, 1) * LocMatrix;
                LocMatrix = Matrix4.CreateTranslation(-Animation.Hitbox.X * 2 * Size - Width, 0, 0) * LocMatrix;
            }
            if (flipY)
            {
                LocMatrix = Matrix4.CreateScale(1, -1, 1) * LocMatrix;
                LocMatrix = Matrix4.CreateTranslation(0, -Animation.Hitbox.Y * 2 * Size - Height, 0) * LocMatrix;
            }
            LocMatrix = Matrix4.CreateScale(extent.X * Size, extent.Y * Size, 1) * LocMatrix;
        }
        // Just the render call; everything should be set up before calling this.
        public virtual void UnsafeDraw()
        {
            GL.DrawArrays(PrimitiveType.Polygon, 0, 4);
            if (MultiplePositions)
            {
                PointF lastOffset = new PointF(0, 0);
                foreach (PointF offset in Offsets)
                {
                    LocMatrix = Matrix4.CreateTranslation((offset.X - lastOffset.X) * (flipX ? -1 : 1), (offset.Y - lastOffset.Y) * (flipY ? -1 : 1), 0) * LocMatrix;
                    GL.UniformMatrix4(Texture.Program.ModelLocation, false, ref LocMatrix);
                    GL.DrawArrays(PrimitiveType.Polygon, 0, 4);
                    lastOffset = offset;
                }
            }
        }

        public void ResetAnimation()
        {
            if (Animation is object && Texture is object)
            animFrame = 0;
        }

        public void SetPreviousLoaction()
        {
            if (Animation is null)
            {
                Animation = Animation.Static(0, 0, Texture);
            }
            X = (float)Math.Round(X, 4);
            Y = (float)Math.Round(Y, 4);
            PreviousX = X;
            PreviousY = Y;
            PreviousWidth = Width;
            PreviousHeight = Height;
        }

        public virtual bool FlipX { get => flipX; set => flipX = value; }
        public virtual bool FlipY { get => flipY; set => flipY = value; }

        public virtual void Process()
        {
            DidCollision = false;
            FramePush = new PushData(Pushability);
            AdvanceFrame();
        }

        public void AdvanceFrame()
        {
            if (Animation is object && Texture is object)
            {
                if (animFrame + 1 >= Animation.FrameCount * Animation.BaseSpeed)
                    animFrame = Animation.LoopStart;
                else
                    animFrame += 1;
            }
        }

        public virtual void CollideY(double distance, Sprite collision)
        {
            DY -= distance;
            //if (distance > 0)
            //    Bottom = collision.Y;
            //else if (distance < 0)
            //    Y = collision.Bottom;
        }
        public virtual void CollideX(double distance, Sprite collision)
        {
            DX -= distance;
            //if (distance > 0)
            //    Right = collision.X;
            //else if (distance < 0)
            //    X = collision.Right;
        }

        public virtual CollisionData TestCollision(Sprite testFor)
        {
            if (testFor == this || Immovable || Static) return null;

            CollisionData ret = null;
            if ((Solid != SolidState.NonSolid) && testFor.Solid != SolidState.NonSolid || testFor.AlwaysCollide)
            {
                if (IsOverlapping(testFor))
                    ret = GetCollisionData(testFor);
            }
            return ret;
        }
        protected virtual CollisionData GetCollisionData(Sprite testFor)
        {
            bool isTile = testFor is Tile;
            int direction = -1;
            if (isTile)
            {
                direction = (int)(testFor as Tile).State;
                if (direction < 5)
                {
                    direction = -1;
                }
                else
                    direction -= 5;
            }
            for (int i = -1; i < Offsets.Count; i++)
            {
                float ofX = i > -1 ? Offsets[i].X : 0;
                float ofY = i > -1 ? Offsets[i].Y : 0;
                for (int j = -1; j < testFor.Offsets.Count; j++)
                {
                    float ofXO = j > -1 ? testFor.Offsets[j].X : 0;
                    float ofYO = j > -1 ? testFor.Offsets[j].Y : 0;
                    if (!testFor.Within(DX + ofX, DY + ofY, Width, Height, ofXO, ofYO)) continue;
                    // check for vertical collision first
                    // top
                    if (Math.Round(PreviousY + PreviousHeight + ofY, 4) <= Math.Round(testFor.PreviousY, 4) + ofYO && direction < 1)
                        return new CollisionData(true, DY + Height + ofY - (testFor.DY + ofYO), testFor);
                    // bottom
                    else if (Math.Round(PreviousY + ofY, 4) >= Math.Round(testFor.PreviousY + testFor.Height + ofYO, 4) && (direction == -1 || direction == 1))
                        return new CollisionData(true, DY + ofY - (testFor.DY + testFor.Height + ofYO), testFor);
                    // right
                    else if (Math.Round(PreviousX + PreviousWidth + ofX, 4) <= Math.Round(testFor.PreviousX + ofXO, 4) && (direction == -1 || direction == 2))
                        return new CollisionData(false, DX + Width + ofX - (testFor.DX + ofXO), testFor);
                    // left
                    else if (Math.Round(PreviousX + ofX, 4) >= Math.Round(testFor.PreviousX + testFor.Width + ofXO, 4) && (direction == -1 || direction == 3))
                        return new CollisionData(false, DX + ofX - (testFor.DX + testFor.Width + ofXO), testFor);
                    else if (testFor.AlwaysCollide)
                        return new CollisionData(true, 0, testFor);
                    if (!testFor.MultiplePositions)
                        break;
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
            double d = ret.Distance;
            Pushability pushability = Pushability;
            if (ret.CollidedWith.Solid != SolidState.Ground) d = 0;
            for (int i = 1; i < collisions.Count; i++)
            {
                CollisionData dt = collisions[i];
                if (!dt.Vertical)
                {
                    if (ret.Vertical || Math.Abs(dt.Distance) > Math.Abs(d) && dt.CollidedWith.Solid == SolidState.Ground)
                    {
                        if (pushability <= dt.CollidedWith.FramePush.GetOppositePushability(dt))
                        {
                            pushability = dt.CollidedWith.FramePush.GetOppositePushability(dt);
                            ret = dt;
                            d = ret.Distance;
                        }
                    }
                }
                else if (ret.Vertical)
                {
                    if (Math.Abs(dt.Distance) > Math.Abs(d) && dt.CollidedWith.Solid == SolidState.Ground)
                    {
                        ret = dt;
                        d = ret.Distance;
                    }
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

        public virtual bool CollideWith(CollisionData data)
        {
            if (Static || Immovable) return false;
            if (data.CollidedWith is WarpLine)
                data.CollidedWith.Collide(new CollisionData(data.Vertical, -data.Distance, this));
            if (data.CollidedWith.Solid == SolidState.Entity && (data.CollidedWith.Static || data.CollidedWith.Immovable)) return true;
            if (data.Vertical)
            {
                CollideY(data.Distance, data.CollidedWith);
            }
            else
            {
                CollideX(data.Distance, data.CollidedWith);
            }
            return true;
        }

        public virtual void Move(double x, double y)
        {
            this.DX += x;
            this.DY += y;
        }

        public virtual void HandleCrewmanCollision(Crewman crewman)
        {
            //Do nothing
        }

        public void ExtendTexture(float width, float height)
        {
            extent = new PointF(width, height);
            animFrame = animFrame;
        }

        public virtual SortedList<string, SpriteProperty> Properties
        {
            get
            {
                SortedList<string, SpriteProperty> ret = new SortedList<string, SpriteProperty>();
                ret.Add("Type", new SpriteProperty("Type", () => "Sprite", (t, g) => { }, "", SpriteProperty.Types.String, "The type of the sprite.", false));
                ret.Add("X", new SpriteProperty("X", () => X, (t, g) => InitialX = (float)t, 0f, SpriteProperty.Types.Float, "The X position of the sprite."));
                ret.Add("Y", new SpriteProperty("Y", () => Y, (t, g) => InitialY = (float)t, 0f, SpriteProperty.Types.Float, "The Y position of the sprite."));
                ret.Add("Texture", new SpriteProperty("Texture", () => Texture.Name, (t, g) => ChangeTexture(g.TextureFromName((string)t)), "", SpriteProperty.Types.Texture, "The Texture used by the sprite.", false));
                ret.Add("Animation", new SpriteProperty("Animation", () => Animation?.Name, (t, g) => Animation = Texture.AnimationFromName((string)t), "", SpriteProperty.Types.Animation, "The Animation desplayed as the sprite."));
                ret.Add("Layer", new SpriteProperty("Layer", () => Layer, (t, g) => Layer = (int)t, 0, SpriteProperty.Types.Int, "The layer this sprite is drawn on. Lower layers are behind higher ones.", false));
                ret.Add("Gravity", new SpriteProperty("Gravity", () => Gravity, (t, g) => Gravity = (float)t, 0f, SpriteProperty.Types.Float, "The gravity of the sprite.", false));
                ret.Add("Color", new SpriteProperty("Color", () => Color.ToArgb(), (t, g) => Color = Color.FromArgb((int)t), Color.White.ToArgb(), SpriteProperty.Types.Color, "The color of this sprite."));
                ret.Add("FlipX", new SpriteProperty("FlipX", () => FlipX, (t, g) => FlipX = (bool)t, false, SpriteProperty.Types.Bool, "Whether this sprite is flipped on the X axis."));
                ret.Add("FlipY", new SpriteProperty("FlipY", () => FlipY, (t, g) => FlipY = (bool)t, false, SpriteProperty.Types.Bool, "Whether this sprite is flipped on the Y axis."));
                ret.Add("Static", new SpriteProperty("Static", () => Static, (t, g) => Static = (bool)t, false, SpriteProperty.Types.Bool, "Static sprites cannot be animated or moved.", false));
                ret.Add("Solid", new SpriteProperty("Solid", () => (int)Solid, (t, g) => Solid = (SolidState)(int)t, (int)SolidState.Entity, SpriteProperty.Types.Int, "The solidity of the sprite. 0 = Solid, 1 = Entity, 2 = NonSolid", false));
                ret.Add("Name", new SpriteProperty("Name", () => Name, (t, g) => Name = (string)t, "", SpriteProperty.Types.String, "The name of the sprite.", false));
                ret.Add("Size", new SpriteProperty("Size", () => Size, (t, g) => Size = (float)t, 1, SpriteProperty.Types.Float, "The size of the sprite.", false));
                return ret;
            }
        }

        public virtual void SetProperty(string name, JToken value, Game game)
        {
            SortedList<string, SpriteProperty> properties = Properties;
            if (properties.ContainsKey(name))
            {
                properties[name].SetValue(value, game);
            }
        }
        public void SetProperty(JProperty property, Game game)
        {
            SetProperty(property.Name, property.Value, game);
        }
        public JToken GetProperty(string name)
        {
            SortedList<string, SpriteProperty> properties = Properties;
            if (properties.ContainsKey(name))
                return properties[name].GetValue();
            else
                return null;
        }
        public void SyncAnimation(int frame)
        {
            animFrame = frame % Animation.FrameCount;
        }

        public virtual JObject Save(Game game, bool isUniversal = false)
        {
            JObject ret = new JObject();
            if (game.UserAccessSprites.ContainsValue(this) && !isUniversal)
            {
                ret.Add("Sprite", Name);
                ret.Add("X", X);
                ret.Add("Y", Y);
                return ret;
            }
            bool isTemplate = TemplateName != null;
            Sprite tmp = null;
            if (isTemplate)
            {
                tmp = LoadSprite(game.TemplateFromName(TemplateName), game);
                ret.Add("Base", TemplateName);
                if (tmp.Texture != Texture)
                    ret.Add("Texture", Texture.Name);
            }
            else
                ret.Add("Texture", Texture.Name);
            SortedList<string, SpriteProperty> properties = Properties;
            foreach (SpriteProperty property in properties.Values)
            {
                if (property.Name == "Texture") continue;
                if (isTemplate)
                {
                    if (property.GetValue().Equals(tmp.GetProperty(property.Name)))
                        continue;
                }
                else if (property.IsDefault) continue;
                ret.Add(property.Name, property.GetValue());
            }
            return ret;
        }

        public static Sprite LoadSprite(JToken loadFrom, Game game)
        {
            if (!loadFrom.HasValues) return null;
            string spriteName = (string)loadFrom["Sprite"];
            string templateName = (string)loadFrom["Base"];
            string type = (string)loadFrom["Type"];
            //Type t = typeof(Sprite);
            Sprite s;
            if (spriteName is object)
            {
                float x = (float)(loadFrom["X"] ?? 0f);
                float y = (float)(loadFrom["Y"] ?? 0f);
                s = game.SpriteFromName(spriteName);
                if (s != game.ActivePlayer || game.CurrentState != Game.GameStates.Playing)
                {
                    s.ResetAnimation();
                    s.InitialX = x;
                    s.InitialY = y;
                }
            }
            else if (templateName is object)
            {
                s = LoadSprite(game.TemplateFromName(templateName), game);
                foreach (JProperty property in loadFrom)
                {
                    s.SetProperty(property, game);
                    s.TemplateName = templateName;
                }
            }
            else
            {
                float x = (float)(loadFrom["X"] ?? 0f);
                float y = (float)(loadFrom["Y"] ?? 0f);
                string textureName = (string)loadFrom["Texture"] ?? "";
                string name = (string)loadFrom["Name"];

                Texture texture;
                if (textureName == lastLoaded?.Name)
                    texture = lastLoaded;
                else
                    texture = game.TextureFromName(textureName ?? "");
                lastLoaded = texture;
                int? layer = loadFrom["Layer"] != null ? (int)loadFrom["Layer"] : (int?)null;
                Color color = Color.FromArgb((int)(loadFrom["Color"] ?? -1));
                if (type == "Tile")
                {
                    int tileX = (int)(loadFrom["TileX"] ?? 0);
                    int tileY = (int)(loadFrom["TileY"] ?? 0);
                    string tag = (string)loadFrom["Tag"] ?? "";
                    s = new Tile((int)x, (int)y, texture, tileX, tileY) { Tag = tag };
                }
                else if (type == "Enemy")
                {
                    string animationName = (string)loadFrom["Animation"] ?? "";
                    float xSpeed = (float)(loadFrom["XSpeed"] ?? 0);
                    float ySpeed = (float)(loadFrom["YSpeed"] ?? 0);
                    int boundX = (int)(loadFrom["BoundsX"] ?? 0);
                    int boundY = (int)(loadFrom["BoundsY"] ?? 0);
                    int boundW = (int)(loadFrom["BoundsWidth"] ?? 0);
                    int boundH = (int)(loadFrom["BoundsHeight"] ?? 0);
                    bool flipX = (bool)(loadFrom["FlipX"] ?? false);
                    bool flipY = (bool)(loadFrom["FlipY"] ?? false);
                    s = new Enemy(x, y, texture, texture.AnimationFromName(animationName), xSpeed, ySpeed, color);
                    s.FlipX = flipX;
                    s.FlipY = flipY;
                    (s as Enemy).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
                }
                else if (type == "Crewman")
                {
                    string standName = (string)loadFrom["Standing"] ?? "Standing";
                    string walkName = (string)loadFrom["Walking"] ?? "Walking";
                    string fallName = (string)loadFrom["Falling"] ?? "Falling";
                    string jumpName = (string)loadFrom["Jumping"] ?? "Jumping";
                    string dieName = (string)loadFrom["Dying"] ?? "Dying";
                    int textBoxColor = (int)(loadFrom["TextBox"] ?? 0);
                    bool sad = (bool)(loadFrom["Sad"] ?? false);
                    float gravity = (float)(loadFrom["Gravity"] ?? 0.6875f);
                    bool flipX = (bool)(loadFrom["FlipX"] ?? false);
                    string squeakName = (string)loadFrom["Squeak"] ?? "crew1";
                    bool canFlip = (bool)(loadFrom["Flip"] ?? true);
                    float jump = (float)(loadFrom["Jump"] ?? 1.6875f);
                    float speed = (float)(loadFrom["Speed"] ?? 3f);
                    float acceleration = (float)(loadFrom["Acceleration"] ?? 0.475f);
                    s = new Crewman(x, y, texture, game, name ?? "", texture.AnimationFromName(standName), texture.AnimationFromName(walkName), texture.AnimationFromName(fallName), texture.AnimationFromName(jumpName), texture.AnimationFromName(dieName), Color.FromArgb(textBoxColor));
                    (s as Crewman).Sad = sad;
                    (s as Crewman).Squeak = game.GetSound(squeakName);
                    (s as Crewman).CanFlip = canFlip;
                    (s as Crewman).Jump = jump;
                    (s as Crewman).MaxSpeed = speed;
                    (s as Crewman).Acceleration = acceleration;
                    s.Gravity = gravity;
                    s.FlipX = flipX;
                }
                else if (type == "Checkpoint")
                {
                    string deactivatedName = (string)loadFrom["Animation"] ?? "Deactivated";
                    string activatedName = (string)loadFrom["ActivatedAnimation"] ?? "Activated";
                    bool flipX = (bool)(loadFrom["FlipX"] ?? false);
                    bool flipY = (bool)(loadFrom["FlipY"] ?? false);
                    s = new Checkpoint(x, y, texture, texture.AnimationFromName(deactivatedName), texture.AnimationFromName(activatedName), flipX, flipY);
                }
                else if (type == "Platform")
                {
                    string animationName = (string)loadFrom["Animation"] ?? "";
                    string disappearName = (string)loadFrom["DisappearAnimation"] ?? "";
                    float xSpeed = (float)(loadFrom["XSpeed"] ?? 0f);
                    float ySpeed = (float)(loadFrom["YSpeed"] ?? 0f);
                    float conveyor = (float)(loadFrom["Conveyor"] ?? 0f);
                    bool disappear = (bool)(loadFrom["Disappear"] ?? false);
                    int boundX = (int)(loadFrom["BoundsX"] ?? 0);
                    int boundY = (int)(loadFrom["BoundsY"] ?? 0);
                    int boundW = (int)(loadFrom["BoundsWidth"] ?? 0);
                    int boundH = (int)(loadFrom["BoundsHeight"] ?? 0);
                    int length = (int)(loadFrom["Length"] ?? 4);
                    s = new Platform(x, y, texture, texture.AnimationFromName(animationName), xSpeed, ySpeed, conveyor, disappear, texture.AnimationFromName(disappearName));
                    (s as Platform).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
                    (s as Platform).Length = length;
                }
                else if (type == "Terminal")
                {
                    string deactivatedName = (string)loadFrom["Animation"] ?? "";
                    string activatedName = (string)loadFrom["ActivatedAnimation"] ?? "";
                    string script = (string)loadFrom["Script"] ?? "";
                    bool repeat = (bool)(loadFrom["Repeat"] ?? false);
                    bool flipX = (bool)(loadFrom["FlipX"] ?? false);
                    bool flipY = (bool)(loadFrom["FlipY"] ?? false);
                    s = new Terminal(x, y, texture, texture.AnimationFromName(deactivatedName), texture.AnimationFromName(activatedName), game.ScriptFromName(script), repeat, game);
                    s.FlipX = flipX;
                    s.FlipY = flipY;
                }
                else if (type == "GravityLine")
                {
                    int length = (int)(loadFrom["Length"] ?? 1);
                    bool horizontal = (bool)(loadFrom["Horizontal"] ?? true);
                    string animationName = (string)loadFrom["Animation"] ?? "";
                    float xSpeed = (float)(loadFrom["XSpeed"] ?? 0f);
                    float ySpeed = (float)(loadFrom["YSpeed"] ?? 0f);
                    int boundX = (int)(loadFrom["BoundsX"] ?? 0);
                    int boundY = (int)(loadFrom["BoundsY"] ?? 0);
                    int boundW = (int)(loadFrom["BoundsWidth"] ?? 0);
                    int boundH = (int)(loadFrom["BoundsHeight"] ?? 0);
                    s = new GravityLine(x, y, texture, texture.AnimationFromName(animationName), horizontal, length);
                    (s as GravityLine).XVel = xSpeed;
                    (s as GravityLine).YVel = ySpeed;
                    (s as GravityLine).Bounds = new Rectangle(boundX, boundY, boundW, boundH);
                }
                else if (type == "WarpLine")
                {
                    string animationName = (string)loadFrom["Animation"] ?? "";
                    int length = (int)(loadFrom["Length"] ?? 1);
                    bool horizontal = (bool)(loadFrom["Horizontal"] ?? true);
                    float offX = (float)(loadFrom["OffsetX"] ?? 0f);
                    float offY = (float)(loadFrom["OffsetY"] ?? 0f);
                    int direction = (int)(loadFrom["Direction"] ?? 0);
                    s = new WarpLine(x, y, texture, texture.AnimationFromName(animationName), length, horizontal, offX, offY, direction);
                }
                else if (type == "WarpToken")
                {
                    Animation animation = texture.AnimationFromName((string)loadFrom["Animation"] ?? "");
                    float outX = (float)(loadFrom["OutX"] ?? 0f);
                    float outY = (float)(loadFrom["OutY"] ?? 0f);
                    int outRoomX = (int)(loadFrom["OutRoomX"] ?? 0);
                    int outRoomY = (int)(loadFrom["OutRoomY"] ?? 0);
                    int settings = (int)(loadFrom["Flip"] ?? 3);
                    s = new WarpToken(x, y, texture, animation, outX, outY, outRoomX, outRoomY, game, (WarpToken.FlipSettings)settings);
                }
                else if (type == "ScriptBox")
                {
                    int width = (int)(loadFrom["Width"] ?? 1);
                    int height = (int)(loadFrom["Height"] ?? 1);
                    string script = (string)loadFrom["Script"] ?? "";
                    s = new ScriptBox(x, y, texture, width, height, game.ScriptFromName(script), game);
                }
                else if (type == "Sprite")
                {
                    string animationName = (string)loadFrom["Animation"] ?? "";
                    bool flipX = (bool)(loadFrom["FlipX"] ?? false);
                    bool flipY = (bool)(loadFrom["FlipY"] ?? false);
                    s = new Sprite(x, y, texture, texture.AnimationFromName(animationName));
                    s.FlipX = flipX;
                    s.FlipY = flipY;
                }
                else if (type == "Push")
                {
                    string animationName = (string)loadFrom["Animation"] ?? "";
                    float gravity = (float)(loadFrom["Gravity"] ?? 0.6875f);
                    s = new PushSprite(x, y, texture, texture.AnimationFromName(animationName));
                    s.Gravity = gravity;
                }
                else if (type == "Text")
                {
                    string text = (string)loadFrom["Text"] ?? "";
                    s = new StringDrawable(x, y, texture, text);
                }
                else if (type == "Trinket")
                {
                    string animationName = (string)loadFrom["Animation"] ?? "";
                    string scriptName = (string)loadFrom["Script"] ?? "";
                    int id = (int)(loadFrom["ID"] ?? 0);
                    s = new Trinket(x, y, texture, texture.AnimationFromName(animationName), game.ScriptFromName(scriptName), game, id);
                }

                else s = null;
                if (s != null && layer != null)
                    s.Layer = layer.Value;
                if (s != null)
                    s.Color = color;
                if (name is object)
                    s.Name = name;
                s.Size = (float)(loadFrom["Size"] ?? 1);
                if (s is IPlatform)
                {
                    IPlatform ip = s as IPlatform;
                    JArray ot = (JArray)loadFrom["Attached"];
                    if (ot is object)
                    {
                        foreach (JToken ots in ot)
                        {
                            Sprite sp = LoadSprite(ots, game);
                            ip.OnTop.Add(sp);
                            sp.onPlatform = ip;
                        }
                    }
                }
            }

            return s;
        }
        public void Load(JToken loadFrom, Game game)
        {
            SortedList<string, SpriteProperty> props = Properties;
            foreach (JProperty property in loadFrom)
            {
                if (props.TryGetValue(property.Name, out SpriteProperty p))
                {
                    p.SetValue(property.Value, game);
                    props.Remove(p.Name);
                }
            }
            foreach (SpriteProperty property in props.Values)
            {
                property.SetValue(property.DefaultValue, game);
            }
            ResetSprite();
        }
        protected virtual void ResetSprite()
        {
            ResetAnimation();
            IsWarpingH = IsWarpingV = false;
            MultiplePositions = false;
            Offsets.Clear();
        }
    }

}
