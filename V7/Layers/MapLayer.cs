using OpenTK;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace V7
{
    public class MapLayer : SpritesLayer
    {
        public SpriteCollection Sprites { get; private set; }
        public RectangleSprite Selection { get; private set; }
        public RectangleSprite BG { get; private set; }
        public int Width;
        public int Height;
        private static Random r = new Random();
        public Action<int, int> OnClick;
        public Action<int, int> SwapMap;
        public bool AllowEscape;
        public bool EnableSelect = true;
        private SpriteCollection extraSprites;

        private Point selected;
        private bool selecting;
        private PointF selectOrigin;
        private bool dragging;
        private IMapSprite mapDragging;
        private IMapSprite mapMoving;
        private Point mapOrigin;
        private SortedList<int, IMapSprite> MapSprites;

        public readonly PointF TopLeft;
        public readonly SizeF CellSize;

        public MapLayer(Game owner, Game.MapAnimations animation, float x, float y, float width, float height, int mapX = -1, int mapY = -1, int mapW = -1, int mapH = -1, bool white = false, bool showAll = false, Texture texture = null)
        {
            MapSprites = new SortedList<int, IMapSprite>();
            Sprites = new SpriteCollection();
            extraSprites = new SpriteCollection();
            Owner = owner;
            if (mapW == -1) mapW = Owner.WidthRooms;
            if (mapH == -1) mapH = Owner.HeightRooms;
            if (mapX == -1) mapX = Owner.OffsetXRooms;
            if (mapY == -1) mapY = Owner.OffsetYRooms;
            Width = mapW;
            Height = mapH;
            float w = width / mapW;
            float h = height / mapH;
            float ofX = 0;
            float ofY = 0;
            if (w / 40 > h / 30)
            {
                w = h * 4 / 3;
                ofX = (width - w * mapW) / 2;
            }
            else
            {
                h = w * 3 / 4;
                ofY = (height - h * mapH) / 2;
            }
            TopLeft = new PointF(ofX + x, ofY + y);
            CellSize = new SizeF(w, h);
            for (int y1 = mapY; y1 < mapH + mapY; y1++)
            {
                for (int x1 = mapX; x1 < mapW + mapX; x1++)
                {
                    if (!showAll && !owner.ExploredRooms.Contains(new Point(x1, y1)))
                        continue;
                    IMapSprite ms;
                    if (white && texture is object)
                    {
                        ms = new MapImageSprite(0, 0, texture, x1, y1);
                        //.Add(ms as Sprite);
                    }
                    else
                    {
                        if (!Owner.MapSprites.ContainsKey(x1 + y1 * 100)) continue;
                        ms = Owner.MapSprites[x1 + y1 * 100].Clone();
                        ms.IsWhite = white;
                    }
                    MapSprites.Add(x1 + y1 * 100, ms);
                    ms.FinishFading = null;
                    ms.X = ofX + x + x1 * w;
                    ms.Y = ofY + y + y1 * h;
                    ms.Layer = 25;
                    ms.SetSize(w, h);
                    Sprites.Add(ms as Sprite);
                    if (animation.HasFlag(Game.MapAnimations.Fade))
                    {
                        ms.FadeIn();
                    }
                    if (animation.HasFlag(Game.MapAnimations.Random))
                    {
                        ms.Delay(r.Next(30));
                    }
                    float ofx = 0, ofy = 0;
                    if (animation.HasFlag(Game.MapAnimations.Up))
                    {
                        ofy = Game.RESOLUTION_HEIGHT;
                    }
                    else if (animation.HasFlag(Game.MapAnimations.Down))
                    {
                        ofy = -Game.RESOLUTION_HEIGHT;
                    }
                    if (animation.HasFlag(Game.MapAnimations.Left))
                    {
                        ofx = Game.RESOLUTION_WIDTH;
                    }
                    else if (animation.HasFlag(Game.MapAnimations.Right))
                    {
                        ofx = -Game.RESOLUTION_WIDTH;
                    }
                    if (!(ofx == 0 && ofy == 0))
                    {
                        ms.EnterFrom(new PointF(ofx, ofy));
                    }
                }
            }
            BG = new RectangleSprite(0, 0, 0, 0);
            BG.Layer = 24;
            BG.Color = Color.Black;
            BG.X = x + ofX;
            BG.Y = y + ofY;
            BG.SetSize(width - ofX * 2, height - ofY * 2);
            if (!Sprites.Contains(BG))
            {
                Sprites.Add(BG);
            }
        }

        public void AddSprite(Sprite s)
        {
            extraSprites.Add(s);
            Sprites.Add(s);
        }

        public void RemoveSprite(Sprite s)
        {
            extraSprites.Remove(s);
            Sprites.Remove(s);
        }

        public override void Dispose()
        {
            // Do nothing?
        }

        public override void HandleClick(MouseButtonEventArgs e)
        {
            // Do nothing?
        }

        public override void HandleKey(PassedKeyEvent e, bool typing)
        {
            if (AllowEscape && e.Key == Keys.Escape)
            {
                Close(Owner.MapAnimation);
            }
        }

        public override void HandleWheel(int e)
        {
            // Do nothing
        }

        public override void Process()
        {
            if (Sprites.Count <= extraSprites.Count && closing)
            {
                FinishLayer();
                return;
            }
            for (int i = 0; i < Sprites.Count; i++)
            {
                Sprite s = Sprites[i];
                s.Process();
            }

            if (Selection is null)
            {
                Selection = new RectangleSprite(0, 0, 0, 0);
                Selection.Color = Color.FromArgb(100, 255, 255, 255);
                Selection.Layer = 26;
            }
            if (!Sprites.Contains(Selection) && !closing && EnableSelect)
            {
                Sprites.Add(Selection);
            }
            float w = BG.Width / Width;
            float h = BG.Height / Height;
            int x = (int)Math.Floor((Owner.MouseX - BG.X) / w);
            int y = (int)Math.Floor((Owner.MouseY - BG.Y) / h);
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                selected = new Point(x, y);
                Selection.X = BG.X + (x * w);
                Selection.Y = BG.Y + (y * h);
                Selection.SetSize(w, h);
                Selection.Visible = true;
            }
            else
                Selection.Visible = false;
            if (Owner.LeftMouse && !selecting && EnableSelect)
            {
                selecting = true;
                selectOrigin = new PointF(x, y);
            }
            else if (selecting)
            {
                if (SwapMap is object && !dragging && selectOrigin != new PointF(x, y) && MapSprites.ContainsKey((int)selectOrigin.X + (int)selectOrigin.Y * 100))
                {
                    dragging = true;
                    mapDragging = MapSprites[(int)selectOrigin.X + (int)selectOrigin.Y * 100];
                    mapDragging.SetTarget(Owner.MouseX - mapDragging.Width / 2, Owner.MouseY - mapDragging.Height / 2);
                    if (MapSprites.ContainsKey(x + y * 100))
                    {
                        mapMoving = MapSprites[x + y * 100];
                        mapOrigin = new Point(x, y);
                        mapMoving.SetTarget(BG.X + (int)selectOrigin.X * w, BG.Y + (int)selectOrigin.Y * h);
                    }
                }
                else if (dragging && Owner.LeftMouse)
                {
                    mapDragging.SetTarget(Owner.MouseX - mapDragging.Width / 2, Owner.MouseY - mapDragging.Height / 2);
                    if (MapSprites.ContainsKey(x + y * 100) && MapSprites[x + y * 100] != mapMoving)
                    {
                        if (mapMoving is object)
                            mapMoving.SetTarget(BG.X + mapOrigin.X * w, BG.Y + mapOrigin.Y * h);
                        mapMoving = MapSprites[x + y * 100];
                        if (mapMoving != mapDragging)
                        {
                            mapOrigin = new Point(x, y);
                            mapMoving.SetTarget(BG.X + (int)selectOrigin.X * w, BG.Y + (int)selectOrigin.Y * h);
                        }
                        else
                            mapMoving = null;
                    }
                }
                else if (!Owner.LeftMouse && dragging)
                {
                    selecting = false;
                    dragging = false;
                    if (mapMoving is object && mapDragging is object)
                    {
                        mapDragging.SetTarget(BG.X + mapOrigin.X * w, BG.Y + mapOrigin.Y * h);
                        int rs = x + y * 100;
                        int sw = (int)selectOrigin.X + (int)selectOrigin.Y * 100;
                        //JObject roomSwap = null;
                        //if (RoomDatas.ContainsKey(rs))
                        //    roomSwap = RoomDatas[rs];
                        //JObject swapWith = null;
                        //if (RoomDatas.ContainsKey(sw))
                        //    swapWith = RoomDatas[sw];
                        //RoomDatas.Remove(rs);
                        //RoomDatas.Remove(sw);
                        //if (roomSwap is object)
                        //{
                        //    roomSwap["X"] = (int)selectOrigin.X;
                        //    roomSwap["Y"] = (int)selectOrigin.Y;
                        //    RoomDatas.Add(sw, roomSwap);
                        //}
                        //if (swapWith is object)
                        //{
                        //    swapWith["X"] = x;
                        //    swapWith["Y"] = y;
                        //    RoomDatas.Add(rs, swapWith);
                        //}
                        SwapMap?.Invoke(sw, rs);
                        MapSprites.Remove(sw);
                        MapSprites.Remove(rs);
                        MapSprites.Add(rs, mapDragging);
                        MapSprites.Add(sw, mapMoving);
                        mapMoving = null;
                        mapDragging = null;
                    }
                    else if (mapDragging is object)
                    {
                        mapDragging.SetTarget(BG.X + (int)selectOrigin.X * w, BG.Y + (int)selectOrigin.Y * h);
                        mapDragging = null;
                        mapMoving = null;
                    }
                }
                else if (!Owner.LeftMouse && !dragging)
                {
                    selecting = false;
                    if (OnClick is null)
                    {
                        Owner.LoadRoom(x, y);
                        Close(Owner.MapAnimation);
                    }
                    else
                        OnClick(x, y);
                    //CurrentEditingFocus = FocusOptions.Level;
                    //clickMap = null;
                }
            }
        }

        private bool closing;
        public void Close(Game.MapAnimations animation)
        {
            closing = true;
            YieldInput = true;
            ExitLayer();
            foreach (IMapSprite m in MapSprites.Values)
            {
                if ((animation | Game.MapAnimations.Random) != Game.MapAnimations.Random)
                {
                    m.FinishFading = () => Sprites.Remove(m as Sprite);
                    if (animation.HasFlag(Game.MapAnimations.Random))
                        m.Delay(r.Next(30));
                    if (animation.HasFlag(Game.MapAnimations.Fade))
                        m.FadeOut();
                }
                else
                    Sprites.Remove(m as Sprite);
                float ofx = 0, ofy = 0;
                if (animation.HasFlag(Game.MapAnimations.Up))
                {
                    ofy = Game.RESOLUTION_HEIGHT;
                }
                else if (animation.HasFlag(Game.MapAnimations.Down))
                {
                    ofy = -Game.RESOLUTION_HEIGHT;
                }
                if (animation.HasFlag(Game.MapAnimations.Left))
                {
                    ofx = Game.RESOLUTION_WIDTH;
                }
                else if (animation.HasFlag(Game.MapAnimations.Right))
                {
                    ofx = -Game.RESOLUTION_WIDTH;
                }
                if (!(ofx == 0 && ofy == 0))
                {
                    m.X += ofx;
                    m.Y += ofy;
                    m.EnterFrom(new PointF(-ofx, -ofy));
                }
            }
            Sprites.Remove(BG);
            Sprites.Remove(Selection);
        }

        public override void Render(Matrix4 baseCamera, int viewMatrixLocation)
        {
            Owner.SetView(ref baseCamera);
            Sprites.Render(Owner.FrameCount);
        }
    }
}
