using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V7
{
    public class RoomGroup : Room
    {
        public SortedList<int, JObject> RoomDatas = new SortedList<int, JObject>();
        public bool ContainsRoom(int x, int y, Game game) => RoomDatas.ContainsKey(x * 100 + y);

        private bool isLoaded = false;

        public override int WidthRooms => w;
        public override int HeightRooms => h;
        private int w;
        private int h;
        public override bool IsGroup => true;

        public override float Width => w * ROOM_WIDTH;
        public override float Height => h * ROOM_HEIGHT;

        public RoomGroup Load(Game game)
        {
            if (!isLoaded)
            {
                isLoaded = true;
                Objects.Clear();
                int minX = -1, minY = -1, maxX = -1, maxY = -1;
                for (int i = 0; i < RoomDatas.Count; i++)
                {
                    JObject rd = RoomDatas.Values[i];
                    int x = (int)rd["X"];
                    int y = (int)rd["Y"];
                    if (x < minX || minX == -1) minX = x;
                    if (x > maxX || maxX == -1) maxX = x;
                    if (y < minY || minY == -1) minY = y;
                    if (y > maxY || maxY == -1) maxY = y;
                    Room r = LoadRoom(rd, game);
                    Objects.AddRange(r.Objects);
                }
                X = minX;
                Y = minY;
                w = maxX + 1 - minX;
                h = maxY + 1 - minY;
            }
            return this;
        }

        public void Unload()
        {
            isLoaded = false;
            Objects.Clear();
        }

        public RoomGroup(Script enter, Script leave) : base(new SpriteCollection(), enter, leave)
        {

        }
    }
}
